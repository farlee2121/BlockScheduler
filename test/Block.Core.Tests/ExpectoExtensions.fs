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
type ITestEnv<'api, 'env>  = 
    abstract Setup : unit -> ('api * 'env)
    abstract Cleanup : 'env -> unit

type TestEnv<'api, 'env> = 
    { 
        setup : unit -> ('api * 'env)
        cleanup : 'env -> unit
    }
    interface ITestEnv<'api, 'env> with
        member this.Setup () = this.setup ()
        member this.Cleanup (env) = this.cleanup env



let withEnv (testEnv:ITestEnv<'api, 'env>) f = 
    let (api, env) = testEnv.Setup ()
    let result = f api
    testEnv.Cleanup env
    result

let withEnvAndArgs (testEnv:ITestEnv<'api, 'env>) f argTuple= 
    withEnv testEnv (fun api -> f api argTuple)

let testWithEnv name test testEnv = 
    testCase name (fun () -> withEnv testEnv test)
    
    
let testPropertyWithEnv name (ftest: 'api -> 'argtuple -> 'a) testEnv =
    //Mostly intuitive property test with centralized environment application
    // Takes advantage of the fact that
    // 1. destructured tuples look almost identical to additional function parameters,
    //    but are one parameter so I can control function execution time relative to setup/cleanup
    // 2. FsCheck knows how to automatically build tuples from sub-types
    testProperty name (withEnvAndArgs testEnv ftest)

let testPropertyWithConfigWithEnv config name ftest testEnv =
    testPropertyWithConfig config name (withEnvAndArgs testEnv ftest)

//IDEA: some of these names are pretty long. I should consider aliasing them

type EnvironmentTestFactory<'api, 'env> = ITestEnv<'api, 'env> -> Test

let testListWithEnv listName (testList : EnvironmentTestFactory<'api, 'env> list) (env : ITestEnv<'api, 'env>) : Test = 
    let applyEnv f = f env
    Expecto.Tests.testList listName (List.map applyEnv testList) 


