#r "nuget: Farmer"
#r "nuget: KubernetesClient"

open System
open System.IO
open k8s
open k8s.Models
open Farmer
open Farmer.Builders

let ns =
    V1Namespace (
        apiVersion = "v1",
        kind = "Namespace",
        metadata = V1ObjectMeta (name = "day4")
    )

// Load the script to a k8s secret that will be mounted on all containers.
let day4ScriptSecret =
    V1Secret(
        apiVersion = "v1", kind = "Secret", metadata = V1ObjectMeta(
            name="day4script",
            namespaceProperty = "day4"
        ),
        ``type`` = "Opaque",
        stringData = dict [
                "Day4.sh", (System.IO.File.ReadAllText "Day4.sh")
            ]
    )

// Make a container for each line of data, setting the line in the ASSIGNMENTS environment variable.
let day4data = File.ReadAllLines "data/Day4.data.txt"
let containers =
    day4data |> Seq.mapi (fun idx line ->
        V1Container (
            image = "bash:5.2.12",
            imagePullPolicy = "IfNotPresent",
            name = $"check-overlap-{idx}",
            command = ResizeArray [ "bash" ],
            args = ResizeArray [ "/app/Day4.sh" ],
            env = ResizeArray [ V1EnvVar ("ASSIGNMENTS", line) ],
            volumeMounts = ResizeArray [V1VolumeMount(name="appscripts", mountPath="/app")]
        )
    )
    
// Then build the deployment.
let deployment =
    V1Deployment (
        apiVersion = "apps/v1",
        kind = "Deployment",
        metadata = V1ObjectMeta(
            name = "day4-part1-deployment",
            namespaceProperty = "day4",
            labels = dict ["app", "day4-part1"]
        ),
        spec = V1DeploymentSpec(
            selector = V1LabelSelector(
                matchLabels = dict [ "app", "day4-part1"]),
            template = V1PodTemplateSpec(
                metadata=V1ObjectMeta(labels = dict [ "app", "day4-part1" ]),
                spec=V1PodSpec(
                    containers = 
                        ResizeArray containers,
                    volumes = ResizeArray [
                        V1Volume(name = "appscripts", secret = V1SecretVolumeSource(secretName="day4script"))
                    ]
                )
            ),
            replicas = 1)
        )

/// The AKS deployment gets verbose, but it's very compressible. This will keep it within limits.
let gzB64 (input:string) =
    use istream = new MemoryStream(input |> System.Text.Encoding.UTF8.GetBytes)
    use ostream = new MemoryStream()
    using (new Compression.GZipStream(ostream, Compression.CompressionLevel.SmallestSize)) (fun gzstream ->
        istream.WriteTo(gzstream)
    )
    ostream.ToArray() |> Convert.ToBase64String

let b64deployment =
    [
        box ns
        box day4ScriptSecret
        box deployment
    ]
    |> KubernetesYaml.SerializeAll
    |> gzB64

// Build the deployment with a minimal AKS cluster and deployment script resource that will 
// 1. Decode the gzipped deployment from base64 to binary
// 2. Gunzip it to a deployment YAML
// 3. Install kubectl
// 4. Login to the newly deployed AKS cluster.
// 5. Apply the deployment
arm {
    location Location.EastUS
    add_resources [
        aks {
            name "advent-of-code-2022"
            service_principal_use_msi
        }
        deploymentScript {
            name "day4-k8s-deploy"
            depends_on (Farmer.Arm.ContainerService.managedClusters.resourceId "advent-of-code-2022")
            script_content $"echo '{b64deployment}' | base64 -d | gzip -d > deployment.yaml && az aks install-cli && az aks get-credentials -g $AZURE_RESOURCE_GROUP -n advent-of-code-2022 && kubectl apply -f deployment.yaml"
            env_vars [
                "AZURE_RESOURCE_GROUP", "[resourceGroup().name]"
            ]
        }
    ]
}
|> Farmer.Deploy.execute "farmer-aoc" []
