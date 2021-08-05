module ExpectoExtensions
open Expecto
open FsCheck

[<AutoOpen>]
module TestLabels =
    let slowLabel = testLabel "Slow"

module Expect = 
    let equal' actual expected = Expect.equal actual expected "Should be equal"
    let wantOk' result = Expect.wantOk result "Expected Ok got Error"
    let isEmpty' list = Expect.isEmpty list "Expected the list to be empty"



//TODO: split out the testApi stuff into it's own module
type ITestEnv<'api, 'env> = 
    abstract Setup : unit -> ('api * 'env)
    abstract Cleanup : 'env -> unit


let withEnv setup cleanup f () =
    let (api, env) = setup ()
    let result = f api
    cleanup env
    result




let withEnvI (testEnv:ITestEnv<'a, 'b>) f = 
    let (api, env) = testEnv.Setup ()
    let result = f api
    testEnv.Cleanup env
    result

let withEnvAndArgs (testEnv:ITestEnv<'a, 'b>) f argTuple= 
    withEnvI testEnv (fun api -> f api argTuple)

let testWithEnv testEnv name test = 
    testCase name (fun () -> withEnvI testEnv test)
    
    
let testEnvProperty testEnv name (ftest: 'api -> 'argtuple -> 'a) =
    //Mostly intuitive property test with centralized environment application
    // Takes advantage of the fact that
    // 1. destructured tuples look almost identical to additional function parameters,
    //    but are one parameter so I can control function execution time relative to setup/cleanup
    // 2. FsCheck knows how to automatically build tuples from sub-types
    testProperty name (withEnvAndArgs testEnv ftest)

let testEnvPropertyWithConfig testEnv config name ftest =
    testPropertyWithConfig config name (withEnvAndArgs testEnv ftest)



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

