#r "nuget: FSharp.Text.Docker"
#r "nuget: Farmer"

open FSharp.Text.Docker
open FSharp.Text.Docker.Builders
open Farmer
open Farmer.Builders

// Get the data and the script to embed in the Dockerfile.

let day5data =
    System.IO.File.ReadAllText "data/Day5.data.txt"
    |> System.Text.Encoding.UTF8.GetBytes
    |> System.Convert.ToBase64String
let day5script =
    System.IO.File.ReadAllText "Day5.Script.fsx"
    |> System.Text.Encoding.UTF8.GetBytes
    |> System.Convert.ToBase64String

// Determine how many intermediate images are needed to process the instructions.
let numInstructions =
    System.IO.File.ReadAllLines "data/Day5.data.txt"
    |> Array.skipWhile(fun line -> line.Length > 0)
    |> fun arr -> arr.Length    

// The first image needs the data and script, then it will run it and update the data.
let initialData = 
    dockerfile {
        from_stage "mcr.microsoft.com/dotnet/sdk:7.0.100" "img0"
        // Add the script to the initial image
        run $"echo {day5script} | base64 -d > Day5.Script.fsx"
        // Copy the initial data in place
        run $"mkdir data && echo {day5data} | base64 -d > data/Day5.data.txt"
        // Run a script to modify the crates based on a line and then save the updated data.
        run "dotnet fsi Day5.Script.fsx"
    }

// Then add an intermediate image for each instruction by copying from the prior image
// so as each image is built, it is processing a line of instructions and saving the updated
// data.
let next (i:int) =
    dockerfile {
        // Make a new stage
        from_stage "mcr.microsoft.com/dotnet/sdk:7.0.100" $"img{i}"
        // Copy the data and script from the preview stage to this new state
        copy_from $"img{i-1}" "data/Day5.data.txt" "data/Day5.data.txt"
        copy_from $"img{i-1}" "Day5.Script.fsx" "Day5.Script.fsx"
        // Run a script to modify the crates based on a line and then save the updated data.
        run "dotnet fsi Day5.Script.fsx"
        // ... keep doing this until we run out of instructions
    }

let rest =
    [1..numInstructions]
    |> Seq.map next
    |> Seq.map (fun stage -> stage.Instructions)
    |> List.ofSeq

// Now get the howl dockerfile as base64, because we have to embed that in the ARM template.
let dockerfile =
    initialData.Instructions :: rest
    |> List.concat
    |> Dockerfile.buildDockerfile
    |> System.Text.Encoding.UTF8.GetBytes
    |> System.Convert.ToBase64String

// Definte the ARM deployment to create a container registry and run a deployment script that will
// write the Dockerfile to the script container and run an ACR build task to build it.
// When the image is fully built, the last layer contains the answer to the puzzle.
arm {
    location Location.EastUS
    add_resources [
        containerRegistry {
            name "adventofcode2022"
            sku ContainerRegistry.Basic
            enable_admin_user
        }
        deploymentScript {
            name "day5-docker-image"
            env_vars [ "ACR_NAME", "adventofcode2022" ]
            depends_on (Arm.ContainerRegistry.registries.resourceId "adventofcode2022")
            script_content (
                [
                    "set -eux"
                    $"echo {dockerfile} | base64 -d > Dockerfile"
                    $"az acr build --registry $ACR_NAME --image day5:1.0.0 ."
                ] |> String.concat " ; "
            )
        }
    ]
} |> Farmer.Deploy.execute "farmer-aoc" []
