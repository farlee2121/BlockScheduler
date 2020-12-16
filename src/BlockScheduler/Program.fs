// Learn more about F# at http://fsharp.org

open System
open Block.Core.HostsFileWebBlockAccessor
open HostsFileAccessor
open BlockApi
open System.IO


// what is my configuration going to look like? a json file? 
// even if file edited, shouldn't apply change until next scheduled block release

[<EntryPoint>]
let main argv =
    let readFile () = File.ReadAllLines defaultHostPath |> List.ofArray |> Ok
    let getRecordsComposed = getRecords readFile

    let records = getRecordsComposed ()
    printfn "%A" records

    printfn "Hello World from F#!"
    //unblockDomain
    //HostsFileWebBlockAccessor.
    0 // return an integer exit code
