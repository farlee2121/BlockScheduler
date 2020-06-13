namespace Block.Core

open System.Text.RegularExpressions

module HostsFileWebBlockAccessor =
// model of host file is a bunch of lines split on spaces
// starting with # means commented out
// ip part, domain part, is commented, other content (i.e. comments)
// could certainly build a model of whole file, but it would be simplier if I reserve a portion of the file #blocker.start #blocker.end

    type BlockedWebAddress = { Url:string; }

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

    let getHostFileLines filePath =
        System.IO.File.ReadAllLines(filePath)

    let blockRecordToHostLine (blockRecord: BlockedWebAddress) =
        sprintf "127.0.0.1 %s" blockRecord.Url

    let saveHostFileLines filePath (hostRecords: BlockedWebAddress seq) =
        let rawLines = getHostFileLines filePath 
        let recordsBeforeBlockerSection = rawLines |> Seq.takeWhile ((not) << isSectionStart)
        let recordsAfterSectionEnd = rawLines |> Seq.rev |>  Seq.takeWhile ((not) << isSectionEnd)
        let blockerSection = hostRecords |> Seq.map blockRecordToHostLine
        let allLines = [|recordsBeforeBlockerSection; blockerSection; recordsAfterSectionEnd|] |> Seq.reduce Seq.append
        System.IO.File.WriteAllLines(filePath, allLines)


    let matchesUrl (domain:string) (record:BlockedWebAddress) = contains domain record.Url



    // only these two should really be public
    // hmm, it is most convenient to write these against a collection of blocked addresses, but I need it to save to the file
    // I should compose, but what do I do with the names? What do I want to be my config parameters that configure? Just the file? A host record source
    // I will likely only use this method of block/unblock with the host file, so I would say I want to just partially apply the file path
    let blockDomain (hostRecords:BlockedWebAddress seq) domain =
        let doesRecordExist = hostRecords |> Seq.tryFind (matchesUrl domain)
        match doesRecordExist with
        | Some record -> hostRecords
        | None -> hostRecords |> Seq.append [{ Url = domain }]

    let unblockDomain (hostRecords: BlockedWebAddress seq) domain =
        let doesRecordExist = hostRecords |> Seq.tryFind (matchesUrl domain)
        match doesRecordExist with
        | Some record -> hostRecords |> Seq.filter (not << (matchesUrl domain))
        | None -> hostRecords


