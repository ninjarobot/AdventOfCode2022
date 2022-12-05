#r "nuget: Farmer"

open System.IO
open Farmer
open Farmer.Builders

let adventAppInsights = 
    appInsights {
        name "advent-of-code-2022"
    }

type AppInsightsConfig with
    member this.UpdatedConnectionString =
        ArmExpression.create $"reference(resourceId('Microsoft.Insights/components', '{this.Name.Value}'), '2020-02-02').ConnectionString"


let day3: ContainerGroupConfig = containerGroup {
    name "day3"
    add_volumes [
        volume_mount.secret_string "app" "Day3.Script.fsx" (File.ReadAllText "Day3.Script.fsx")
        volume_mount.secret_string "data" "Day3.data.txt" (File.ReadAllText "data/Day3.data.txt")
    ]
    restart_policy ContainerGroup.RestartPolicy.NeverRestart
    add_instances [
        containerInstance {
            name "checkPackages"
            image "mcr.microsoft.com/dotnet/sdk:7.0.100"
            memory 0.5<Gb>
            cpu_cores 0.5
            command_line ("dotnet fsi /app/Day3.Script.fsx".Split null |> List.ofArray)
            add_volume_mount "app" "/app"
            add_volume_mount "data" "/app/data"
            env_vars [
                EnvVar.createSecureExpression "APP_INSIGHTS_CONN_STRING" adventAppInsights.UpdatedConnectionString
            ]
        }
    ]
    depends_on (Farmer.ResourceId.create (Farmer.Arm.Insights.components, ResourceName "advent-of-code-2022"))
}

arm {
    location Location.EastUS
    add_resources [
        adventAppInsights
        day3
    ]
}
|> Farmer.Deploy.execute "farmer-aoc" []
