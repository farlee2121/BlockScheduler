﻿namespace Block.Core

open System.Text.RegularExpressions
open Contracts
open System.IO
open StreamExtensions
open SeqExtensions

module HostsFileWebBlockAccessor =
// model of host file is a bunch of lines split on spaces
// starting with # means commented out
// ip part, domain part, is commented, other content (i.e. comments)
// could certainly build a model of whole file, but it would be simplier if I reserve a portion of the file #blocker.start #blocker.end

    let contains substr (record:string) = record.Contains(substr)

    let sectionStartMarker = "#blocker.start"
    let sectionEndMarker = "#blocker.end"

    let isSectionStart = contains sectionStartMarker
    let isSectionEnd = contains sectionEndMarker
    
    type RecordParseResult = Result<BlockedSite, string>

    module Result = 
        let isOk = function | Ok -> true | Error -> false
        let tryGet = function | Ok res -> res | Error -> failwith "Could not get value. Result was an error"



    let parseUrlFromRecord (hostLine: string) = 
    // what I really want here is does it look like "something.something"
        let urlRegex = new Regex("(\s)+([^\s]+)");
        let urlMatch = urlRegex.Match(hostLine)
        if(urlMatch.Success) then RecordParseResult.Ok {Url = urlMatch.Value.Trim()}
        else RecordParseResult.Error "Invalid hosts record: no domain/url present"

    let getHostsBlockerSection hostLines = 
        // take while after #start and before # end 
        // could do this with state, could do this with set difference. We'll do set diff for now
        let recordsBeforeBlockerSection = hostLines |> Seq.takeWhile ((not) << isSectionStart) |> Set.ofSeq
        let recordsBeforeSectionEnd = hostLines |> Seq.takeWhile ((not) << isSectionEnd) |> Set.ofSeq

        let blockerSection =
            (Set.union recordsBeforeBlockerSection (Set.ofArray [|sectionStartMarker; sectionEndMarker|]))
            |> Set.difference recordsBeforeSectionEnd 
        let parsedSites =
            blockerSection
            |> Seq.map parseUrlFromRecord
            |> Seq.filter Result.isOk
            |> Seq.map Result.tryGet
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



    // only these two should really be public
    // hmm, it is most convenient to write these against a collection of blocked addresses, but I need it to save to the file
    // I should compose, but what do I do with the names? What do I want to be my config parameters that configure? Just the file? A host record source
    // I will likely only use this method of block/unblock with the host file, so I would say I want to just partially apply the file path
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
        |> getHostsBlockerSection

    
    

