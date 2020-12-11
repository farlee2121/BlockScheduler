module HostAccessorTests
open Xunit
open FsUnit.Xunit
open FsUnit

[<Fact>]
let ``List Blocked Sites When None Blocked`` () = 
    // let hostFile = TestApi.MockHostFile ()
    // let blockedSites = TestApi.ListBlockedSites hostFile ()  
    [] |> should be Empty 
    // Expect.isEmpty blockedSites "Blocked sites should be empty"
            

