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

type IPGen =
    static member IP() : Arbitrary<IP> =
        let generator = matching @"^(\d+\.){3}\d+$" |> Gen.map (IP)
        let arb = generator |> Arb.fromGen
        arb

type DomainGen =
    static member Domain() : Arbitrary<DomainUrl> =
        //IMPORTANT: requires ^ and $ or it might create nulls (maybe because of multi-line?)
        
        let generator = matching @"^[a-zA-Z0-9\-\.]+\.[a-xA-Z]+\\?$" |> Gen.map (Domain)
        let arb = generator |> Arb.fromGen
        arb

type MetaGen =
    static member Meta() : Arbitrary<Meta option> =
        let regexGenerator = matching @"^[^#\n\r]+[^\n\r\s]+[^\n\r]*$" |> Gen.map (Meta >> Some)
        let generator = Gen.oneof [regexGenerator; Gen.constant None]
        let arb = generator |> Arb.fromGen
        arb


//NOTE: The other method is a sneaky extension of Gen. It is important that the type itself has a definition in the module
//      trying to extend the initial type causes issues with finding the extension method... maybe I need to put the extension in an auto open module?
//SOURCE: https://github.com/haf/expecto#property-based-tests
//Could also look into a convention-based (typeclass?) registration like in https://github.com/fscheck/FsCheck/issues/334


module Gen =
    let registerWithExpecto type' config = { config with arbitrary = type' :: config.arbitrary }