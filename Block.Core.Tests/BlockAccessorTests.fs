
module BlockAccessorTests
    open System
    open Expecto
    open Block.Core.Contracts
    open Block.Core

    module TestApi = 
        let private stream = HostsFileWebBlockAccessor.Stream (new System.IO.MemoryStream())
        let BlockSite = HostsFileWebBlockAccessor.blockSite stream
        let UnblockSite = HostsFileWebBlockAccessor.unblockSite stream
        let ListBlockedSites =  HostsFileWebBlockAccessor.listBlockedSites stream

    // expecto mentions https://github.com/SwensenSoftware/unquote, but I don't quite understand the value prop yet
    [<Tests>]
    let ``My test`` =
        testList "Block accessor" [
            test "List Blocked Sites When None Blocked" {
                let blockedSites = TestApi.ListBlockedSites ()    
                Expect.isEmpty blockedSites "Blocked sites should be empty"
            }
            test "Block empty" {
                let site = { Url = "" }
                TestApi.BlockSite site
                let blockedSites = TestApi.ListBlockedSites ()   
                Expect.equal (seq { site }) blockedSites "Site not blocked??"
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
                let site = { Url = "www.meow.com" }
                TestApi.BlockSite site
                let blockedSites = TestApi.ListBlockedSites ()
                Expect.equal blockedSites (seq { site }) "Site not blocked??"
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
