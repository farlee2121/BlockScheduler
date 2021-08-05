module HostAccessorTests
open HostsFileAccessor
open Expecto
open FsUnit.Xunit

open FsCheck
open TypeGenerators
open System.IO
open ExpectoExtensions

type HostAccessorApi<'a, 'err> = { GetRecords: (unit -> Result<HostRecord list, 'err>); WriteAll: (HostRecord list -> 'a)}


let BuildHostAccessorTests testEnv () =
    let config = HostRecordGen.registerAll FsCheckConfig.defaultConfig

    let test' = testWithEnv testEnv
    let testProperty' name = testEnvPropertyWithConfig testEnv config name
    let withEnv' = withEnvI testEnv
    
        
    testList "HostAccessorTests" [
        test' "List Records Empty When None Written" <| fun testApi ->
            let records = testApi.GetRecords ()
            Expect.isEmpty (Expect.wantOk' records) ""

        testProperty' "Single record: read equals write" 
            <| fun testApi (record:HostRecord) ->
                    testApi.WriteAll [record] |> ignore
                    let actual = (Expect.wantOk' (testApi.GetRecords ()))
                    let isSuccess = actual = [record]
                    // NOTE: potential alternative is to compose an Arb for the composite type and use Prop.forAll 
                    isSuccess
                    
            

        testEnvPropertyWithConfig testEnv { config with endSize = 5 } "Multi-record: read equals write" 
                (fun testApi (records:HostRecord list) ->                
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


    let testEnv ={
        setup = (fun () -> 
            let guid = System.Guid.NewGuid()
            printfn "setup %O" guid
            ((testApiProvider ()), guid))
        cleanup = (fun env -> printfn "cleanup %O" env )
    }

    //let testEnv = {
    //    new ITestEnv with 
    //        member this.Setup () : (HostAccessorApi<unit, 'a> * System.Guid) =
    //            let guid = System.Guid.NewGuid()
    //            printfn "setup %O" guid
    //            ((testApiProvider ()), guid)

    //        member this.Cleanup env : System.Guid =
    //            printfn "cleanup %O" env 
    //}
     
    testList "Host Accessor Spec" [
        BuildHostAccessorTests testEnv ()
        BuildExampleBasedRegexTests ()
    ]


[<Tests>]
let ``HostAccessor with SectionWriter`` = 

    let testEnv = {
        setup = fun () ->
            let sectionId = SectionWriter.SectionId "Block Test"
            let stream = (new MemoryStream());
            let reader () = SectionWriter.readSection stream sectionId () |> Ok
            let writer lines = SectionWriter.writeSection stream sectionId lines
            ({GetRecords = (getRecords reader); WriteAll = (writeAll writer)}, stream)

        cleanup = fun env -> env.Dispose() 
    }

    testList "HostAccessor with SectionWriter" [
        BuildHostAccessorTests testEnv ()
    ]


type SectionWriterTestApi = { 
    ReadSection: SectionWriter.SectionId -> string list;
    WriteSection: SectionWriter.SectionId -> string list -> unit;
    // ReadFullDoc: unit -> string list
}

let buildSectionWriterTests setup cleanup = 
    let test' = testWithEnv setup cleanup

    // blank document
    // document with no section, but other content
    // document with partial section
    // document with existing section and other content
    
    // QUESTION: do I add methods for adding content outside the section? is it worth abstracting the underlying section division?
    //          I could also create a type for |OutsideSection | InSection as a sort of builder
    //          The public api doesn't know about non-section content though, so maybe here I just make sure sections stay separate and leave 

    // Plan: blackbox test that sections stay separate, whitebox test against a stream to make sure it doesn't erase other file content  
    testList "meow" [

    ]
       
