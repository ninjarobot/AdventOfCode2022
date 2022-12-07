#r "nuget: Farmer"

open Farmer
open Farmer.Builders

let day6data = System.IO.File.ReadAllText "data/Day6.data.txt"

let startOfMessageLen = 14

// Too many vnets makes a huge deployment, so breaking into 150 vnet deployment chunks
let chunkSize = 150

seq {
    for charNum in 0..day6data.Length-startOfMessageLen do
        yield day6data[charNum..charNum+(startOfMessageLen-1)]
}
|> Seq.chunkBySize chunkSize
|> Seq.iteri(
    fun chunkIdx chunk ->
    let vNets=
        chunk
        |> Seq.mapi(
            fun vnetIdx chars ->
            vnet {
                // Pad with zeros so they sort properly in the browser
                // Also increment 1 for the index and the length of the SOM marker
                name $"%04i{(chunkIdx * chunkSize) + (vnetIdx + 1 + startOfMessageLen)}.advent"
                add_address_spaces ["10.0.0.0/16"]
                add_subnets (
                    chars |> Seq.mapi(fun idx c ->
                        subnet {
                            name $"{c}"
                            prefix $"10.0.{idx}.0/24"
                        }
                    ) |> List.ofSeq
                )
            } :> IBuilder
        ) |> List.ofSeq

    arm {
        location Location.EastUS
        add_resources vNets
    }
    |> fun arm ->
        try
            arm |> Farmer.Deploy.execute "farmer-aoc-day6-pt2" [] |> ignore
        with // print the error but move on because it's likely these will all fail
        | ex -> eprintfn "%O" ex
)

// After all the chunks are processed there should be a vnet named for the number of the start of message index.
