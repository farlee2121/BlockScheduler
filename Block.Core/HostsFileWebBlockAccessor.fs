namespace Block.Core

open System.Text.RegularExpressions
open Contracts
open System.IO

module HostsFileWebBlockAccessor =
// model of host file is a bunch of lines split on spaces
// starting with # means commented out
// ip part, domain part, is commented, other content (i.e. comments)
// could certainly build a model of whole file, but it would be simplier if I reserve a portion of the file #blocker.start #blocker.end

    let contains substr (record:string) = record.Contains(substr)

    let sectionStartMarker = "#blocker.start"
    let sectionEndMarker = "#blocker.start"

    let isSectionStart = contains sectionStartMarker
    let isSectionEnd = contains sectionEndMarker


    let parseUrlFromRecord (hostLine: string) = 
        let urlRegex = new Regex("(http:|https:)([^\s]+)");
        let urlMatch = urlRegex.Match(hostLine)
        if(urlMatch.Success) then urlMatch.Value
        else failwith "Invalid hosts record: no domain/url present"

    let getHostsBlockerSection hostLines = 
        // take while after #start and before # end 
        // could do this with state, could do this with set difference. We'll do set diff for now
        let recordsBeforeBlockerSection = hostLines |> Seq.takeWhile ((not) << isSectionStart) |> Set.ofSeq
        let recordsBeforeSectionEnd = hostLines |> Seq.takeWhile ((not) << isSectionEnd) |> Set.ofSeq

        Set.difference recordsBeforeSectionEnd recordsBeforeBlockerSection
        |> Seq.map (fun stringRecord -> {Url = (parseUrlFromRecord stringRecord)})



    let blockRecordToHostLine (blockRecord: BlockedSite) =
        sprintf "127.0.0.1 %s" blockRecord.Url


    let rec private ReadAllLines (streamReader:StreamReader) = 
        match streamReader.ReadLine() with
        | null -> []
        | value -> value :: ReadAllLines streamReader

    let rec WriteAllLines (stream:Stream) (lines:seq<string>) =
        // conctrasting the interative approach to the recursive read approach
        let writer = (new StreamWriter(stream))
        lines |> Seq.iter (fun line -> writer.WriteLine(line))
        writer.Flush();
        
    let private GetLinesFromStream (stream:Stream) = 
        ReadAllLines (new StreamReader(stream))

    let saveHostFileLines hostFileStream (hostRecords: BlockedSite seq) =
        let rawLines = GetLinesFromStream hostFileStream 
        let recordsBeforeBlockerSection = rawLines |> Seq.takeWhile ((not) << isSectionStart)
        let recordsAfterSectionEnd = rawLines |> Seq.rev |>  Seq.takeWhile ((not) << isSectionEnd)
        let blockerSection = hostRecords |> Seq.map blockRecordToHostLine
        let allLines = [|recordsBeforeBlockerSection; blockerSection; recordsAfterSectionEnd|] |> Seq.reduce Seq.append
        WriteAllLines hostFileStream allLines


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

    type FileLocator = Stream of Stream | Path of string | Uri of System.Uri 

    let resolveFile fileLocator= 
        match fileLocator with 
        | Stream s -> s.Position <- (int64 0); s
        | Path s -> (File.OpenRead(s) :> System.IO.Stream)
        | Uri u -> File.OpenRead(u.AbsolutePath) :> System.IO.Stream

    let blockSite (hostFilePath:FileLocator) (site:BlockedSite) =
        resolveFile hostFilePath
        |> GetLinesFromStream
        |> getHostsBlockerSection
        |> addSiteBlock' site
        |> saveHostFileLines (resolveFile hostFilePath)

        

    let unblockSite (hostFilePath:FileLocator) (site:BlockedSite) =
        resolveFile hostFilePath
        |> GetLinesFromStream
        |> getHostsBlockerSection
        |> removeSiteBlock' site
        |> saveHostFileLines (resolveFile hostFilePath)

    let listBlockedSites (hostFilePath:FileLocator) () =
        resolveFile hostFilePath
        |> GetLinesFromStream
        |> getHostsBlockerSection

    
    

