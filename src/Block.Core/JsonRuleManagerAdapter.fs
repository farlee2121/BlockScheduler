module RuleManagerAdapters
open FSharp.Data
open System.Text.Json.Serialization
open System.Text.Json
open StreamExtensions
open BlockRuleManager
open System

// type RuleJsonProvider = JsonProvider<"""{"ruleId": "guid" }""", true>


let private serializerOptions = JsonSerializerOptions()
serializerOptions.Converters.Add(JsonFSharpConverter())

let private serialize x = JsonSerializer.Serialize(x, serializerOptions)
let private deserialize<'a> text = JsonSerializer.Deserialize<'a>(json = text, options = serializerOptions)



let private writeAll stream records = 
    serialize records
    |> Stream.WriteAllText stream 

let private readAll stream = 
    Stream.ReadAllText stream
    |> deserialize<BlockRule list>

let createRule stream blockable schedule =
    let ruleId = RuleId (Guid.NewGuid ())
    readAll stream 
    |> List.append [(ruleId, blockable, schedule)]
    |> writeAll stream
    |> ignore

    Ok ruleId
    

let listRules stream () = 
    readAll stream

let deleteRule stream ruleId = 
    readAll stream 
    |> List.filter (fun (id, _, _) -> ruleId <> id)
    |> writeAll stream 
    |> ignore
    Ok RuleId

let updateRule stream rule =
    let (ruleId, _, _) = rule
    let replaceMap updated existing = 
        match existing with
        | (existingId, _, _) when ruleId = existingId -> updated
        | _ -> existing

    readAll stream 
    |> List.map replaceMap
    |> writeAll stream
    |> ignore
    Ok ruleId






