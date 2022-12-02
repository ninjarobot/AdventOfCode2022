open System
open System.IO

let day1Data = File.ReadAllLines "data/Day1.data.txt"
let elves =
    seq {
        let mutable acc = ResizeArray()
        for food in day1Data do
            match food with
            | "" -> 
                let elf = List.ofSeq acc
                acc <- ResizeArray()
                yield elf
            | calories ->
                acc.Add(Int32.Parse calories)
        yield List.ofSeq acc
    }
elves |> Seq.map (List.sum) |> Seq.max |> Console.WriteLine
elves |> Seq.map (List.sum) |> Seq.sortDescending |> Seq.take 3 |> Seq.sum |> Console.WriteLine
