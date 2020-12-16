namespace Block.Core

open System.Text.RegularExpressions
open Contracts
open System.IO
open StreamExtensions
open SeqExtensions

// how to make this more functional?
// probably just make this a host file accessor, not specific to blocking
// I can wrap it as a blocker later
// create URL string wrapper using union, not record
// decide types of lines: in parsed, out of scope?, unparsed
// methods, get section? I think my writer should force a section, but let the caller choose the section name 
// separate it out into a different lib

(* # This can contain markdown *)

module HostsFileWebBlockAccessor =

    let contains (substr:string) (record:string) = record.Contains(substr)

    let sectionStartMarker = "#blocker.start"
    let sectionEndMarker = "#blocker.end"

    let isSectionStart = contains sectionStartMarker
    let isSectionEnd = contains sectionEndMarker
    
    type RecordParseResult = Result<BlockedSite, string>

    
    let parseUrlFromRecord (hostLine: string) = 
        let urlRegex = new Regex("(\s)+([^\s]+)");
        let urlMatch = urlRegex.Match(hostLine)
        if(urlMatch.Success) then RecordParseResult.Ok {Url = urlMatch.Value.Trim()}
        else RecordParseResult.Error "Invalid hosts record: no domain/url present"

    let getHostsBlockerSection hostLines = 
        let recordsBeforeBlockerSection = hostLines |> Seq.takeWhile ((not) << isSectionStart) |> Set.ofSeq
        let recordsBeforeSectionEnd = hostLines |> Seq.takeWhile ((not) << isSectionEnd) |> Set.ofSeq

        let blockerSection =
            (Set.union recordsBeforeBlockerSection (Set.ofArray [|sectionStartMarker; sectionEndMarker|]))
            |> Set.difference recordsBeforeSectionEnd 
        let parsedSites =
            blockerSection
            |> List.ofSeq
            |> List.map (
               parseUrlFromRecord
               >> (function | Ok line -> Some line | Error _ -> None )
            )
            |> List.filter (Option.isSome) 
            |> List.map Option.get   
        parsedSites



    let blockRecordToHostLine (blockRecord: BlockedSite) =
        sprintf "127.0.0.1 %s" blockRecord.Url


    let EnsureBlockerSection (lines: seq<string>) = 
        let hasBlockerSection = Seq.contains sectionStartMarker lines
        // hmm. should think about malformed sections
        if(hasBlockerSection) then lines else Seq.append lines [sectionStartMarker; sectionEndMarker]

    let saveHostFileLines hostFileStream (hostRecords: BlockedSite seq) =
        let rawLines = Stream.ReadAllLines hostFileStream |> EnsureBlockerSection
        let recordsBeforeBlockerSection = rawLines |> Seq.takeUntilInclusive (isSectionStart)
        let recordsAfterSectionEnd = rawLines |> Seq.rev |>  Seq.takeUntilInclusive (isSectionEnd)
        let blockerSection = 
            hostRecords 
            |> Seq.filter (fun url -> (not <| System.String.IsNullOrWhiteSpace(url.Url))) // is there a good way to generalize this to valid host record?
            |> Seq.map blockRecordToHostLine
        let allLines = [|recordsBeforeBlockerSection ; blockerSection; recordsAfterSectionEnd|] |> Seq.reduce Seq.append
        Stream.WriteAllLines hostFileStream allLines 
        |> ignore


    let matchSite (domain:BlockedSite) (record:BlockedSite) = contains domain.Url record.Url


    let addSiteBlock (hostRecords:BlockedSite seq) (domain:BlockedSite) =
        let doesRecordExist = hostRecords |> Seq.tryFind (matchSite domain)
        match doesRecordExist with
        | Some record -> hostRecords
        | None -> hostRecords |> Seq.append [domain]

    let addSiteBlock' site records = addSiteBlock records site

    let removeSiteBlock (hostRecords: BlockedSite seq) domain =
        let doesRecordExist = hostRecords |> Seq.tryFind (matchSite domain)
        match doesRecordExist with
        | Some record -> hostRecords |> Seq.filter (not << (matchSite domain))
        | None -> hostRecords

    let removeSiteBlock' (site:BlockedSite) (hostRecords:seq<BlockedSite>) = removeSiteBlock hostRecords site

    let resolveFile fileLocator= 
        match fileLocator with 
        | Stream s -> s.Position <- (int64 0); s
        | Path s -> (File.OpenRead(s) :> System.IO.Stream)
        | Uri u -> File.OpenRead(u.AbsolutePath) :> System.IO.Stream

    let blockSite (hostFilePath:FileLocator) (site:BlockedSite) =
        resolveFile hostFilePath
        |> Stream.ReadAllLines
        |> getHostsBlockerSection
        |> addSiteBlock' site
        |> saveHostFileLines (resolveFile hostFilePath)

        

    let unblockSite (hostFilePath:FileLocator) (site:BlockedSite) =
        resolveFile hostFilePath
        |> Stream.ReadAllLines
        |> getHostsBlockerSection
        |> removeSiteBlock' site
        |> saveHostFileLines (resolveFile hostFilePath)

    let listBlockedSites (hostFilePath:FileLocator) () =
        resolveFile hostFilePath
        |> Stream.ReadAllLines
        |> getHostsBlockerSection |> Seq.ofList

    
    

