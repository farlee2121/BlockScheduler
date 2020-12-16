module BlockAccessorTests
   open System
   open Expecto
   open Block.Core.Contracts
   open Block.Core

   module TestApi = 
       // alternately, I could create a class that internally manages the partial application of the host file, but that isn't very functional
       let MockHostFile () = Contracts.Stream (new System.IO.MemoryStream())
       let HostFileFromLines (lines:seq<string>) =
           let stream = (new System.IO.MemoryStream())
           StreamExtensions.Stream.WriteAllLines stream lines
           |> Contracts.Stream 
       let HostFileFromString (contents:string) = 
           let stream = (new System.IO.MemoryStream())
           StreamExtensions.Stream.WriteAllText stream contents
           |> Contracts.Stream 
       let BlockSite = HostsFileWebBlockAccessor.blockSite 
       let UnblockSite = HostsFileWebBlockAccessor.unblockSite 
       let ListBlockedSites =  HostsFileWebBlockAccessor.listBlockedSites 
       let GetRawHostFile fileLocator = 
           match fileLocator with 
           | Contracts.Stream s -> (s.Position <- int64 0; (new System.IO.StreamReader(s)).ReadToEnd())
           | _ -> raise (NotImplementedException())

   // expecto mentions https://github.com/SwensenSoftware/unquote, but I don't quite understand the value prop yet
   [<Tests>]
   let ``My test`` =
       testList "Block accessor" [
           test "List Blocked Sites When None Blocked" {
               let hostFile = TestApi.MockHostFile ()
               let blockedSites = TestApi.ListBlockedSites hostFile ()    
               Expect.isEmpty blockedSites "Blocked sites should be empty"
           }
           test "Block empty" {
               let hostFile = TestApi.MockHostFile ()
               let site = { Url = "" }
               TestApi.BlockSite hostFile site
               let blockedSites = TestApi.ListBlockedSites hostFile ()   
               Expect.sequenceEqual (Seq.empty) blockedSites "Site not blocked??"
           }
           test "Unblock When Not Blocked" {
               let hostFile = TestApi.MockHostFile ()
               let site = { Url = "www.iamsite.com"}
               TestApi.UnblockSite hostFile site
               Expect.isEmpty (TestApi.ListBlockedSites hostFile ()) "Expected empty"
           }
           test "Unblock Single" {
               let hostFile = TestApi.MockHostFile ()
               let site = { Url = "www.iamsite.com"}
               TestApi.BlockSite hostFile site
               TestApi.UnblockSite hostFile site
               Expect.isEmpty (TestApi.ListBlockedSites hostFile ()) "Expected empty"
           }
           test "Unblock doesn't remove other blocks" {
               let hostFile = TestApi.MockHostFile ()
               let sitesToBlock = [{ Url = "www.iamsite.com"}; { Url = "www.nya.com"}; { Url = "george.com"}]
               sitesToBlock |> List.iter (TestApi.BlockSite hostFile)
               TestApi.UnblockSite hostFile (List.head sitesToBlock)
               Expect.sequenceEqual (Seq.sort (TestApi.ListBlockedSites hostFile ())) (Seq.sort (List.tail sitesToBlock)) ""
           }
           test "Block Single" {
               let hostFile = TestApi.MockHostFile ();
               let site = { Url = "www.meow.com" }
               TestApi.BlockSite hostFile site
               let blockedSites = TestApi.ListBlockedSites hostFile ()
               Expect.sequenceEqual blockedSites (seq { site }) "Site not blocked??"
           }
           test "Re-block same site" {
               let hostFile = TestApi.MockHostFile ();
               let site1 = { Url = "www.meow.com" }
               TestApi.BlockSite hostFile site1
               TestApi.BlockSite hostFile site1
               let blockedSites = TestApi.ListBlockedSites hostFile ()

               Expect.sequenceEqual blockedSites (seq { site1; }) "Site not blocked??"
           }
           test "Block Multiple" {
               let hostFile = TestApi.MockHostFile ();
               let site1 = { Url = "www.meow.com" }
               TestApi.BlockSite hostFile site1
               let site2 = { Url = "www.pat.com" }
               TestApi.BlockSite hostFile site2
               let blockedSites = TestApi.ListBlockedSites hostFile ()

               Expect.sequenceEqual blockedSites (seq { site1; site2 }) "Site not blocked??"
           }
       ]
    
   module String =
       let join (separator:string) (strings:seq<string>)= System.String.Join(separator, strings)
       let joinLines = join "\r\n";

   [<Tests>]
   let HostFileSpecific = testList "Hostfile-Specific tests" [
       test "Doesn't delete records from other sources" {

           let originalLines = ["This is a line";"another line"]
           let originalContent = String.joinLines originalLines
           let hostFile = TestApi.HostFileFromLines originalLines

           let site = { Url = "www.meow.com" }
           TestApi.BlockSite hostFile site
           let modifiedContent = TestApi.GetRawHostFile hostFile

           Expect.isTrue (modifiedContent.StartsWith(originalContent, System.StringComparison.Ordinal)) "Doesn't start with original content"
       }
   ]
        
