module TypeGenerators
open FsCheck
open Fare
open HostsFileAccessor
open Expecto

let matching pattern =
    Gen.sized (fun size ->
        let xeger = Xeger pattern
        let count = if size < 1 then 1 else size
        [ for i in 1..count -> xeger.Generate() ]
        |> Gen.elements
        |> Gen.resize count)

//type IPGen = 
    

//type DomainGen =
    

//type MetaGen =
module Arb =
    let registerWithExpecto type' config = { config with arbitrary = type' :: config.arbitrary }
        
type HostRecordGen =
    static member hostRecordArb () = 
        Gen.oneof [ 
            Arb.Default.Derive<IP * DomainUrl * Meta option> () |> Arb.toGen |> Gen.map (Registration);
            matching @"^[^\n\r]+$" |> Gen.map (HostRecord.Other)
        ]
        |> Arb.fromGen

    static member metaOptionArb () = 
        let regexGenerator = matching @"^[^#\n\r]+[^\n\r\s]+[^\n\r]*$" |> Gen.map (Meta)
        regexGenerator |> Arb.fromGen
        
    static member domainArb () =
        //IMPORTANT: requires ^ and $ or it might create nulls (maybe because of multi-line?)
        matching @"^[a-zA-Z0-9\-\.]+\.[a-xA-Z]+\\?$" |> Gen.map (Domain)
        |> Arb.fromGen
        

    static member ipArb () : Arbitrary<IP> =
        matching @"^(\d+\.){3}\d+$" |> Gen.map (IP)
        |> Arb.fromGen
        
    static member registerAll = Arb.registerWithExpecto typeof<HostRecordGen>


//NOTE: The other method is a sneaky extension of Gen. It is important that the type itself has a definition in the module
//      trying to extend the initial type causes issues with finding the extension method... maybe I need to put the extension in an auto open module?
//SOURCE: https://github.com/haf/expecto#property-based-tests
//Could also look into a convention-based (typeclass?) registration like in https://github.com/fscheck/FsCheck/issues/334
