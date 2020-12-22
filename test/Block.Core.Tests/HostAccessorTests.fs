module HostAccessorTests
open HostsFileAccessor
open Expecto
open FsUnit.Xunit

open FsCheck
open TypeGenerators
open System.IO
open TestLabels


//TODO: How do I achieve a test api? I think I have to do a reader monad... I can't inject into a module or namespace. I could define everything in an object, but that's yucky
// well, even using a reader monad I have to somehow let it be specified external to the test suite and applied to all tests. An expecto test list would be better here 

module Expect = 
    let equal' actual expected = Expect.equal actual expected "Should be equal"
    let wantOk' result = Expect.wantOk result "Expected Ok got Error"

type HostAccessorApi<'a, 'err> = { GetRecords: (unit -> Result<HostRecord list, 'err>); WriteAll: (HostRecord list -> 'a)}

let testWithEnv setup cleanup name test = 
    let testWrap () = 
        let (api, env) = setup ()
        test api
        cleanup env 
    testCase name testWrap

let testPropertyWithEnvAndConfig setup cleanup config name prop =
    // IMPORTANT: the closest i've gotten, but only running setup once and not running the cleanup (because it doesn't find a match in the dictionary)
    let envdict = (new System.Collections.Generic.Dictionary<obj,'b>())
    let prop' = lazy(
        let (api, env) = setup ()
        envdict.Add(api :> obj, env)
        prop api
        )
    let receiveArgsWithClean defaultReceiveArgs' = (fun config name testNum (args:obj list) -> 
                            printfn "running cleanup"

                            args |> List.map (function 
                            | arg when envdict.ContainsKey(arg) -> 
                                printfn "running cleanup %O" envdict.[arg]
                                cleanup envdict.[arg]
                            | _ -> () ) |> ignore
                            defaultReceiveArgs' config name testNum args
                        ) 
    let configWithCleanup = { config with receivedArgs = receiveArgsWithClean config.receivedArgs }

    testPropertyWithConfig configWithCleanup name prop'


let BuildHostAccessorTests setup cleanup () =
    let config = HostRecordGen.registerAll FsCheckConfig.defaultConfig

    let test' = testWithEnv setup cleanup
    let testProperty' name = testPropertyWithEnvAndConfig setup cleanup config name
    let testPropertyWithConfig' _config name = testPropertyWithEnvAndConfig setup cleanup _config name 
        
    testList "HostAccessorTests" [
        test' "List Records Empty When None Written" <| fun testApi ->
            let records = testApi.GetRecords ()
            Expect.isEmpty (Expect.wantOk' records) ""

        testProperty'  "Single record: read equals write" 
            <| (fun (testApi) (record:HostRecord) -> 
                    testApi.WriteAll [record] |> ignore
                    let actual = (Expect.wantOk' (testApi.GetRecords ()))
                    let isSuccess = actual = [record]
                    // NOTE: potential alternative is to compose an Arb for the composite type and use Prop.forAll 
                    isSuccess
                )

        testPropertyWithConfig' { config with endSize = 5 } "Multi-record: read equals write" 
            <| (fun testApi (records:HostRecord list) ->                    
                    testApi.WriteAll records |> ignore
                    let expected = records
                    let actual = (Expect.wantOk' (testApi.GetRecords ()))
                    let isSuccess = actual = expected 
                    isSuccess
                ) 
        ]

let BuildExampleBasedRegexTests () =
    // a fallback to make sure the property test hits important cases
    let expectOther str =
        let reader () = Ok [str]
        let expected = (HostRecord.Other str) 
        let actual = (Expect.wantOk' (getRecords reader ()))
        Expect.equal' actual [expected]

    let expectRegistration raw (ip, domain, meta) = 
        let reader () = Ok [raw]
        let expected = Registration ((IP ip), (Domain domain), (meta |> Option.map (Meta)))
        let actual = (Expect.wantOk' (getRecords reader ()))
        Expect.equal' actual [expected]

    testList "Regex tests" [
        test "Basic domain, no meta" { 
            expectRegistration "127.0.0.1 domain.com" ("127.0.0.1", "domain.com", None)
        }
        test "Basic domain, has meta" {
            expectRegistration "127.0.0.1 domain.com #i'm meta" ("127.0.0.1", "domain.com", Some "i'm meta")
        }
        test "Sub-domain" {
            expectRegistration "127.0.0.1 sub.domain.com" ("127.0.0.1", "sub.domain.com", None)
        }
        test "Multiple pounds to start meta" {
            expectRegistration "127.0.0.1 sub.domain.com ####meta" ("127.0.0.1", "sub.domain.com", Some "meta")
        }
        testList "Non-record cases" [
            test "Commented" { expectOther "#127.0.0.1 domain.com"}
            test "empty" { expectOther ""}
            test "whitespace" { expectOther "       "}
            test "comment line" { expectOther "# comment with spaces"}            
        ]
        testList "Non-record cases" [
            test "invalid ip, not enough segments" { expectOther "1270.0.1 domain.com"}
            test "invalid domain, includes protocol" { expectOther "127.0.0.1 https://domain.com"}       
            test "missing ip" { expectOther "https://domain.com"}       
            test "missing ip, has meta" { expectOther "https://domain.com # i'm meta"}    
            test "missing domain" { expectOther "127.0.0.1"}    
        ]
    ]

[<Tests>]
let ``HostAccessor In-Memory Array`` = 
    
    let testApiProvider () =
        let mutable records = [];
        let reader () = Ok records
        let writer lines = records <- lines
        {GetRecords = (getRecords reader); WriteAll = (writeAll writer)}

    let setup () = 
        let guid = System.Guid.NewGuid()
        printfn "setup %O" guid
        (testApiProvider (), guid)
    let cleanup env = 
        printfn "cleanup %O" env 
    testList "Host Accessor Spec" [
        BuildHostAccessorTests setup cleanup ()
        BuildExampleBasedRegexTests ()
    ]


[<Tests>]
let ``Section Writer Tests`` = 

    let setup () =
        let sectionId = SectionWriter.SectionId "Block Test"
        let stream = (new MemoryStream());
        let reader () = SectionWriter.readSection stream sectionId () |> Ok
        let writer lines = SectionWriter.writeSection stream sectionId lines
        ({GetRecords = (getRecords reader); WriteAll = (writeAll writer)}, stream)

    let cleanup (stream:Stream) = 
        stream.Dispose()

    testList "HostAccessor with SectionWriter" [
        BuildHostAccessorTests setup cleanup ()
    ]


let buildSectionWriterTests () = 
    ()
       
// need to test the section reader/writer to make sure it doesn't mess up surroundings

// next build a UI? or do I make the scheduler?


// check if I can run tests in isolation
// figure out test cases

// could make testApi disposable to pass on the dispose to stream and prevent any leaks