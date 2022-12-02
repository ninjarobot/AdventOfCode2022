#r "nuget: Farmer"

open System
open Farmer
open Farmer.Builders
open Farmer.Arm.RoleAssignment

let day1Data = IO.File.ReadAllText "data/Day1.data.txt"
let elfScript = IO.File.ReadAllText "Day1.sh"

let containerIdentity = createUserAssignedIdentity "day1"

let contributorRbac =
    {
        Name = ResourceName "ad3ee29f-24a7-4a38-b5ce-6dc5f74c44df"
        RoleDefinitionId = Roles.Contributor
        PrincipalId = containerIdentity.PrincipalId
        PrincipalType = PrincipalType.ServicePrincipal
        Scope = AssignmentScope.ResourceGroup
        Dependencies = [ containerIdentity.ResourceId ] |> Set.ofList
    }

let elfContainerGroup =
    containerGroup {
        name $"elf"
        add_identity containerIdentity
        add_volumes [
            volume_mount.secret_string "data" "Day1.data.txt" day1Data
            volume_mount.secret_string "app" "elf.sh" elfScript
            volume_mount.secret_parameter "template" "template.json.b64" "template"
        ]
        restart_policy ContainerGroup.RestartPolicy.NeverRestart
        add_instances [
            containerInstance {
                name "processData"
                image "mcr.microsoft.com/azure-cli:2.42.0"
                add_volume_mount "data" "/data"
                add_volume_mount "app" "/app"
                add_volume_mount "template" "/template"
                memory 0.2<Gb>
                cpu_cores 0.2
                command_line [ "bash"; "/app/elf.sh" ]
                env_vars [
                    EnvVar.createSecure "LINE" "line"
                    EnvVar.createSecure "MAX_SO_FAR" "maxSoFar"
                    EnvVar.createSecure "RESOURCE_GROUP" "resourceGroupName"
                ]
            }
        ]
    }


let innerDeployment =
    arm {
        location Location.EastUS
        add_resources [
            containerIdentity
            elfContainerGroup
        ]
    }

innerDeployment.Template |> Writer.toJson |> Console.WriteLine

let innerDeploymentBase64 =
    innerDeployment.Template
    |> Writer.toJson
    |> System.Text.Encoding.UTF8.GetBytes
    |> Convert.ToBase64String

let dataStorage =
    storageAccount {
        name "advofcode2022"
        add_blob_container "day1"
        grant_access containerIdentity Roles.StorageBlobDataContributor
    }

let outerDeployment =
    arm {
        location Location.EastUS
        add_resource contributorRbac
        add_resources [
            containerIdentity
            dataStorage
            resourceGroup {
                name "[resourceGroup().name]"
                location Location.EastUS
                add_resources [
                    containerIdentity
                    elfContainerGroup
                ]
                add_parameter_values [
                    "template", innerDeploymentBase64
                    "line", "0"
                    "maxSoFar", "0"
                    "resourceGroupName", "[resourceGroup().name]"
                ]
            }
        ]
    }
outerDeployment |> Writer.quickWrite "day1"
