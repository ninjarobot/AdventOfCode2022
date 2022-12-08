open System
open System.Collections.Generic
open System.IO

let day7data = File.ReadAllLines "data/Day7.data.txt"

/// Gets the of the parent directories and updates the dictionary.
let rec getBranchSize (directorySizes:Dictionary<_,_>) (branch:string, node:string, nodeSize:int) =
    match directorySizes.TryGetValue branch with
    | true, size ->
        directorySizes.[branch] <- size + nodeSize
    | false, _ -> directorySizes.Add(branch, nodeSize)
    let separator = branch.LastIndexOf '/'
    if separator > 0 && branch.Length > 1 then
        let newBranch = branch.Substring(0, separator)
        let newNode = branch.Substring(separator + 1)
        getBranchSize directorySizes (newBranch, newNode, nodeSize)

// Formats a stack of path segments into a string path
let format(entries:Stack<string>) =
    entries.ToArray() |> Array.rev |> String.concat "/"

let directorySizes = Dictionary<_,_>()
seq {
    let entries = Stack<string>()
    for line in day7data do
        match line.Split null with
        | [|"$"; "cd"; "/"|] ->
            entries.Push "/"
        | [|"$"; "cd"; ".." |] ->
            entries.Pop() |> ignore // not sure if we need to do anything here
        | [|"$"; "cd"; subdirectory |] ->
            entries.Push subdirectory
        | [|"$"; "ls"|] ->
            ()
        | [| "dir"; directoryName |] ->
            ()
        | [| size; fileName |] ->
            let dirPath = format entries
            yield dirPath, fileName, Int32.Parse size
        | _ -> failwithf "Unsupported format %s" line
}
|> Seq.iter (getBranchSize directorySizes)

// Directories where size is at most 100,000
directorySizes.Values
|> Seq.where (fun size -> size <= 100_000)
|> Seq.sum
|> fun sum -> Console.WriteLine ("Solution - part 1: {0}", sum)

let totalDiskSpace = 70_000_000
let updateSize = 30_000_000
let used = directorySizes.["/"]
let free = totalDiskSpace - used
let spaceNeeded = updateSize - free

// Smallest directory that can be removed to free enough space
directorySizes.Values
|> Seq.sort
|> Seq.find(fun v -> v >= spaceNeeded)
|> fun size -> Console.WriteLine ("Solution - part 2: {0}", size)
