module Tests

open System
//open Xunit
open Expecto


// expecto mentions https://github.com/SwensenSoftware/unquote, but I don't quite understand the value prop yet
[<Tests>]
let ``My test`` () =
    testList "I am name" [
        test "somuch name" {
            Expect.isTrue true "message"
        }
    ]
