module HostAccessorTests
open HostsFileAccessor
open Expecto
open FsUnit.Xunit

open FsCheck
open TypeGenerators


//TODO: How do I achieve a test api? I think I have to do a reader monad... I can't inject into a module or namespace. I could define everything in an object, but that's yucky
// well, even using a reader monad I have to somehow let it be specified external to the test suite and applied to all tests. An expecto test list would be better here 


module Expect = 
    let equal' actual expected = Expect.equal actual expected "Should be equal"
    let wantOk' result = Expect.wantOk result "Expected Ok got Error"

let BuildHostAccessorTests getRecords writeAll () =
    let config = FsCheckConfig.defaultConfig 
                    |> (Gen.registerWithExpecto typeof<DomainGen>
                    >> Gen.registerWithExpecto typeof<MetaGen>
                    >> Gen.registerWithExpecto typeof<IPGen>)

    let testProperty' name property = testPropertyWithConfig config name property
        
    testList "HostAccessorTests" [
        test "List Records Empty When None Written" {
            let records = getRecords ()
            Expect.isEmpty (Expect.wantOk' records) ""
        }

        testProperty' "Single record: read equals write" 
            <| (fun (record:HostRecord) -> 
                    writeAll [record] |> ignore
                    let actual = (Expect.wantOk' (getRecords ()))
                    let isSuccess = actual = [record]
                    // NOTE: potential alternative is to compose an Arb for the composite type and use Prop.forAll 
                    isSuccess
                )

        testProperty' "Multi-record: read equals write" 
            <| (fun (records:HostRecord list) -> 
                    writeAll records |> ignore
                    let expected = records
                    let actual = (Expect.wantOk' (getRecords ()))
                    let isSuccess = actual = expected
                    // NOTE: potential alternative is to compose an Arb for the composite type and use Prop.forAll 
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
    let mutable records = [];
    let reader () = Ok records
    let writer lines = records <- lines

    testList "Host Accessor Spec" [
        BuildHostAccessorTests (getRecords reader) (writeAll writer) ()
        BuildExampleBasedRegexTests ()
    ]


// [<Tests>]
// let ``Section Writer Tests`` = 
//     let mutable records = [];
//     let reader () = Ok records
//     let writer lines = records <- lines

//     testList "Host Accessor Spec" [
//         BuildHostAccessorTests (getRecords reader) (writeAll writer) ()
//         BuildExampleBasedRegexTests ()
//     ]
       
// need to test the section reader/writer to make sure it doesn't mess up surroundings

// next build a UI? or do I make the scheduler?
