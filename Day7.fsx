#r "nuget: Farmer"
#r "nuget: FSharp.Text.Docker"

open System.IO
open FSharp.Text.Docker.Builders
open Farmer
open Farmer.Builders

let day7data =
    File.ReadAllText "data/Day7.data.txt"
    |> System.Text.Encoding.UTF8.GetBytes
    |> System.Convert.ToBase64String
let script =
    File.ReadAllText "Day7.Script.fsx"
    |> System.Text.Encoding.UTF8.GetBytes
    |> System.Convert.ToBase64String

// Define a multistage dockerfile to do native compilation and produce a small final image
let dockerfileB64 =
    let dockerSpec =
        dockerfile {
            from_stage "mcr.microsoft.com/dotnet/sdk:7.0.100" "build"
            // Add dependencies for AOT compilation
            run "apt-get update && apt-get install -y --no-install-recommends clang zlib1g-dev"
            // Make a new console application
            run "mkdir day7 && cd day7 && dotnet new console --language F#"
            // Add the as Program.fs
            run $"echo {script} | base64 -d > day7/Program.fs"
            // Add a line that will keep the container running so we can view the console and see results.
            run "echo '(new System.Threading.ManualResetEvent(false)).WaitOne()' >> day7/Program.fs"
            // Build native binary
            run "cd day7 && dotnet publish -r linux-x64 -c Release -o out -p:PublishAot=true -p:StripSymbols=true"
            // Add new stage for the runtime container
            from "mcr.microsoft.com/dotnet/runtime-deps:7.0"
            workdir "/app"
            // Copy the input data to the final image
            run $"mkdir data && echo {day7data} | base64 -d > data/Day7.data.txt"
            // Copy the build output
            copy_from "build" "/day7/out" "."
            // Set the entrypoint to run the application
            cmd "/app/day7"
        }
    dockerSpec.Build()
    |> System.Text.Encoding.UTF8.GetBytes
    |> System.Convert.ToBase64String

arm {
    location Location.EastUS
    add_resources [
        containerRegistry {
            name "adventofcode2022"
            sku ContainerRegistry.Basic
            enable_admin_user
        }
        deploymentScript {
            name "day7-docker-image"
            env_vars [ "ACR_NAME", "adventofcode2022" ]
            depends_on (Arm.ContainerRegistry.registries.resourceId "adventofcode2022")
            script_content (
                [
                    "set -eux"
                    $"echo {dockerfileB64} | base64 -d > Dockerfile"
                    $"az acr build --registry $ACR_NAME --image day7:1.0.0 ."
                ] |> String.concat " ; "
            )
        }
        containerGroup {
            name "day7"
            depends_on (Arm.DeploymentScript.deploymentScripts.resourceId "day7-docker-image")
            add_instances [
                containerInstance {
                    name "solution"
                    image "adventofcode2022.azurecr.io/day7:1.0.0"
                }
            ]
            reference_registry_credentials [
                Arm.ContainerRegistry.registries.resourceId "adventofcode2022"
            ]
        }
    ]
} |> Farmer.Deploy.execute "farmer-aoc" []
