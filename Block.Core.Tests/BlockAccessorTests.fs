
module BlockAccessorTests
    open System
    open Expecto
    open Block.Core.Contracts
    open Block.Core

    module TestApi = 
        // alternately, I could create a class that internally manages the partial application of the host file, but that isn't very functional
        let MockHostFile () = HostsFileWebBlockAccessor.Stream (new System.IO.MemoryStream())
        let BlockSite = HostsFileWebBlockAccessor.blockSite 
        let UnblockSite = HostsFileWebBlockAccessor.unblockSite 
        let ListBlockedSites =  HostsFileWebBlockAccessor.listBlockedSites 
        let GetRawHostFile fileLocator = 
            match fileLocator with 
            | HostsFileWebBlockAccessor.Stream s -> (s.Position <- int64 0; (new System.IO.StreamReader(s)).ReadToEnd())
            | _ -> raise (NotImplementedException())

    // expecto mentions https://github.com/SwensenSoftware/unquote, but I don't quite understand the value prop yet
    [<Tests>]
    let ``My test`` =
        testList "Block accessor" [
            test "List Blocked Sites When None Blocked" {
                let hostFile = TestApi.MockHostFile ();
                let blockedSites = TestApi.ListBlockedSites hostFile ()    
                Expect.isEmpty blockedSites "Blocked sites should be empty"
            }
            test "Block empty" {
                let hostFile = TestApi.MockHostFile ();
                let site = { Url = "" }
                TestApi.BlockSite hostFile site
                let blockedSites = TestApi.ListBlockedSites hostFile ()   
                Expect.sequenceEqual (Seq.empty) blockedSites "Site not blocked??"
            }
            test "Unblock When Not Blocked" {
                raise (NotImplementedException())
            }
            test "Unblock Single" {
                raise (NotImplementedException())
            }
            test "Unblock Doesn't remove other blocks" {
                raise (NotImplementedException())
            }
            test "Block Single" {
                let hostFile = TestApi.MockHostFile ();
                let site = { Url = "www.meow.com" }
                TestApi.BlockSite hostFile site
                let blockedSites = TestApi.ListBlockedSites hostFile ()
                Expect.sequenceEqual blockedSites (seq { site }) "Site not blocked??"
            }
            test "Sequential Blocks" {
                let hostFile = TestApi.MockHostFile ();
                let site1 = { Url = "www.meow.com" }
                TestApi.BlockSite hostFile site1
                let site2 = { Url = "www.pat.com" }
                TestApi.BlockSite hostFile site2
                let blockedSites = TestApi.ListBlockedSites hostFile ()

                Expect.sequenceEqual blockedSites (seq { site1; site2 }) "Site not blocked??"
            }
            test "Block Multiple" {
                raise (NotImplementedException())
            }
        ]

    [<Tests>]
    let HostFileSpecific = testList "Hostfile-Specific tests" [
        test "Doesn't delete records from other sources" {
            raise (NotImplementedException())
        }
    ]
        
        


    

// how do I achieve test suite re-use like I do in c#? do I still make a test api?
