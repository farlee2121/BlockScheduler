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

