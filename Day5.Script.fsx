open System
open System.IO

/// Splits the data into the crates and the instructions
let cratesAndInstructions data =
    let rec getCrateData (crates:string list) (lines:string list) =
        match lines with
        | [] -> (List.rev crates), []
        | head::tail ->
            match head with
            | "" -> (List.rev crates), tail
            | crate -> getCrateData (crate::crates) tail
    data |> List.ofArray |> getCrateData []

/// Builds a lookup to get the index for each stack number
let stackIndices (crates:string list) =
    let headers = crates |> List.last
    headers.ToCharArray()
    |> Seq.mapi (
        fun idx c ->
            let stackNum = Char.GetNumericValue c |> int
            if stackNum >= 0 then
                Some (stackNum, idx)
            else None
        )
    |> Seq.choose id
    |> dict

/// Gets a stack from crate strings by the stack's index.
let getStackByIndex (stackIndex:int) (crates:string list) =
    seq {
        for crate in crates do
            let crateChar = crate.[stackIndex]
            if Char.IsLetter crateChar then
                yield crate.[stackIndex]
    } |> String.Concat

/// Returns the stacks by stack number and then a string for the crates
let crateStacks (crates:string list) =
    let stackIndexMap = stackIndices crates
    stackIndexMap |> Seq.map (
        fun kvp ->
            kvp.Key, crates |> getStackByIndex stackIndexMap.[kvp.Key]
    )
    |> dict

/// Writes the crates back out to a list of strings
let writeCrates (stackIndexMap:System.Collections.Generic.IDictionary<int, string>) =
    // reverse the strings so we write the bottom one last
    let stackIndexMap = stackIndexMap |> Seq.map (fun kvp -> kvp.Key, kvp.Value |> Seq.rev |> String.Concat) |> dict
    // Get the largest stack so we can write that one first.
    let largestStack = stackIndexMap.Values |> Seq.maxBy (fun s -> s.Length)
    seq {
        yield stackIndexMap.Keys |> Seq.map(fun stackNum -> $" %i{stackNum} ") |> String.concat " "
        for stackLevel in [0..largestStack.Length] do
            yield
                seq {
                    for stackNum in stackIndexMap.Keys do
                        if stackIndexMap.[stackNum].Length > stackLevel then
                            $"[{stackIndexMap.[stackNum].[stackLevel]}]"
                        else
                            "   "
                } |> String.concat " "
    } |> Seq.rev

type Instruction = {
    HowMany : int
    MoveFrom : int
    MoveTo : int
} with
    /// Applies the instructions from a line to the crate stacks.
    static member ApplyTo(crates:string list) (instruction:Instruction) =
        let stacks = crates |> crateStacks |> System.Collections.Generic.Dictionary
        let sourceStack = stacks.[instruction.MoveFrom]
        let moved = sourceStack |> Seq.take instruction.HowMany |> Seq.rev |> String.Concat
        let targetStack = stacks.[instruction.MoveTo]
        stacks.[instruction.MoveFrom] <- sourceStack.Substring(instruction.HowMany)
        stacks.[instruction.MoveTo] <- System.Text.StringBuilder(moved).Append(targetStack).ToString()
        stacks |> writeCrates |> List.ofSeq

/// Parses a line of instructions.
let parseInstruction (instruction:string) =
    match instruction.Split null with
    | [| "move"; howMany; "from"; moveFrom; "to"; moveTo |] ->
        {
            HowMany = Int32.Parse howMany
            MoveFrom = Int32.Parse moveFrom
            MoveTo = Int32.Parse moveTo
        }
    | _ -> invalidArg "instruction" $"Instruction had unexpected format '{instruction}'"

/// Updates the crates that are passed by applying the instruction to them.
let updateCrates (crates:string list) (instruction) =
    instruction |> parseInstruction |> Instruction.ApplyTo crates

/// Loads the Day 5 data, splits out the crates and instructions, prints the crates, 
/// process the instructions, and writes back to the same file.
let day5data = File.ReadAllLines "data/Day5.data.txt"
let crates, instructions = cratesAndInstructions day5data
crates |> List.iter (printfn "%s")
match instructions with
| [] -> crates |> List.iter (printfn "%s"); Environment.Exit 0
| currentInstruction::restOfInstructions ->
    let updatedCrates = updateCrates crates currentInstruction
    let newData = updatedCrates @ "" :: restOfInstructions
    File.WriteAllLines ("data/Day5.data.txt", newData)
