module ExpectoExtensions
open Expecto

[<AutoOpen>]
module TestLabels =
    let slowLabel = testLabel "Slow"

module Expect = 
    let equal' actual expected = Expect.equal actual expected "Should be equal"
    let wantOk' result = Expect.wantOk result "Expected Ok got Error"
    let isEmpty' list = Expect.isEmpty list "Expected the list to be empty"

let withEnv setup cleanup f () =
    let (api, env) = setup ()
    let result = f api
    cleanup env
    result

let testWithEnv setup cleanup name test = 
    testCase name (withEnv setup cleanup test)


// let testPropertyWithEnvAndConfig setup cleanup config name prop =
//     // IMPORTANT: the closest i've gotten, but only running setup once and not running the cleanup (because it doesn't find a match in the dictionary)
//     let envdict = (new System.Collections.Generic.Dictionary<obj,'b>())
//     let prop' = lazy(
//         let (api, env) = setup ()
//         envdict.Add(api :> obj, env)
//         prop api
//         )
//     let receiveArgsWithClean defaultReceiveArgs' = (fun config name testNum (args:obj list) -> 
//                             printfn "running cleanup"

//                             args |> List.map (function 
//                             | arg when envdict.ContainsKey(arg) -> 
//                                 printfn "running cleanup %O" envdict.[arg]
//                                 cleanup envdict.[arg]
//                             | _ -> () ) |> ignore
//                             defaultReceiveArgs' config name testNum args
//                         ) 
//     let configWithCleanup = { config with receivedArgs = receiveArgsWithClean config.receivedArgs }

//     testPropertyWithConfig configWithCleanup name prop'

