open System
open System.IO

module AssumedDecoding =
    let scoreRound (round:string) =
        match round.Split(null, 2) with
        | [| "A"; "X" |] -> 3, 1
        | [| "A"; "Y" |] -> 6, 2
        | [| "A"; "Z" |] -> 0, 3
        | [| "B"; "X" |] -> 0, 1
        | [| "B"; "Y" |] -> 3, 2
        | [| "B"; "Z" |] -> 6, 3
        | [| "C"; "X" |] -> 6, 1
        | [| "C"; "Y" |] -> 0, 2
        | [| "C"; "Z" |] -> 3, 3
        | _ -> raise (FormatException($"Invalid round: '{round}'"))
    let totalRound (round:string) =
        let score, shape = scoreRound round
        score + shape

module ActualDecoding =
    let scoreRound (round:string) =
        match round.Split(null, 2) with
        | [| "A"; "X" |] -> 0, 3
        | [| "A"; "Y" |] -> 3, 1
        | [| "A"; "Z" |] -> 6, 2
        | [| "B"; "X" |] -> 0, 1
        | [| "B"; "Y" |] -> 3, 2
        | [| "B"; "Z" |] -> 6, 3
        | [| "C"; "X" |] -> 0, 2
        | [| "C"; "Y" |] -> 3, 3
        | [| "C"; "Z" |] -> 6, 1
        | _ -> raise (FormatException($"Invalid round: '{round}'"))
    let totalRound (round:string) =
        let score, shape = scoreRound round
        score + shape

let day2data = File.ReadAllLines "data/Day2.data.txt"
day2data |> Seq.map(AssumedDecoding.totalRound) |> Seq.sum |> printfn "%i"

day2data |> Seq.map(ActualDecoding.totalRound) |> Seq.sum |> printfn "%i"