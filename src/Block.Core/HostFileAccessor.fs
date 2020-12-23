module HostsFileAccessor
open System.IO
// open FSharpx.Text
// open System.Text.RegularExpressions
open FSharp.Text.RegexProvider
open System

[<AutoOpen>]
module Model =
// https://github.com/fsprojects/FSharpPlus/ contains various extensions (like for parsing) as well as some approximated type classes (monoids, TryParse,...)
    type DomainUrl = | Domain of string
    module DomainUrl = 
        //TODO: validate the string. consider using F#+ tryparse typeclass. Else could create a regex active pattern or use the regex type provider
        let create str = (Domain str)
        let ofUri (uri:Uri) = (Domain uri.DnsSafeHost)

    type IP = | IP of string
    module IPAddress =
        let create str = (IP str) // I'm pretty sure I saw a try parse for this in F#+

    type Meta = | Meta of string
    module Meta = 
        let create meta = 
            match meta with 
            | str when String.IsNullOrWhiteSpace(str) -> None
            | nonEmpty -> Some (Meta nonEmpty)

    type HostRecord = 
        | Registration of IP * DomainUrl * Meta option // I think this needs to return to a record and manage unknown a different way, I expect everything in the section to be valid, maybe just ignore invalids and only write back successes
        | Other of string 

    module HostRecord = 
        let create ip domain meta =
            let ip = IPAddress.create ip
            let domain = DomainUrl.create domain
            let meta = Meta.create meta
            Registration (ip, domain, meta)
            
        let fold fRegistration fOther record =
            match record with
            | Registration (ip, domain, meta) -> fRegistration ip domain meta
            | Other str -> fOther str

    type AddRecordError = 
        | Other of string // uhh, I'm sure there will be reasons 

    type RemoveRecordError = 
        | Other of string


    type GetRecords<'a> = unit -> Result<HostRecord list, 'a>
    type WriteRecords<'a> = HostRecord list -> 'a


let defaultHostPath = "c:/windows/system32/drivers/etc/hosts"
// let getDefaultHostStream () = System.IO.File.Open(defaultHostPath, )
// what do I pass to this? A text writer? A file location? a Reader monad?


type HostRecordRegex = Regex< @"^(?<ip>(\d+\.){3}\d+)\s+(?<domain>[a-zA-Z0-9\-\.]+\.[a-xA-Z]+\\?)\s*#*(?<meta>.*)$"> 
// ok, let's just do the thing and refactor as needed
let getRecords readLines () = 
        
    let parseLine line = 
        let maybeParsed = if line |> isNull  then None else HostRecordRegex().TryTypedMatch(line)
        match maybeParsed with 
        | Some record ->
            let ip = IPAddress.create record.ip.Value
            let domain = DomainUrl.create record.domain.Value
            let meta = Meta.create record.meta.Value
            Registration (ip, domain, meta)
        | None -> (HostRecord.Other line)
        //TODO: need to deal with the possibility of the sub-types not passing validation

    //TODO: account for the case where 
    match readLines () with
    | Ok lines -> lines |> List.map parseLine |> Ok
    | Error err -> Error err

let writeAll writeLines records = 
    let metaToString = function | Some (Meta str) -> str | None -> ""
    let registrationToString (IP ip) (Domain domain) meta = sprintf "%s %s #%s" ip domain (metaToString meta) //$"{ip} {domain} # {meta}" 
    let toFileString = HostRecord.fold registrationToString id
    let writeAllLines path lines = File.WriteAllLines(path, Array.ofList lines)
        
    // records |> List.map toFileString |> writeAllLines defaultHostPath
    records |> List.map toFileString |> writeLines 


module SectionWriter = 
    type SectionId = | SectionId of string

    let getStartMarker (SectionId sectionId) = sprintf "# start %s" sectionId
    let getEndMarker (SectionId sectionId) = sprintf "# end %s" sectionId

    let splitOnSection sectionId lines = 
        let startIndex = lines |> List.tryFindIndex ((=) (getStartMarker sectionId))
        let endIndex = lines |> List.tryFindIndex ((=) (getEndMarker sectionId))
        
        let reverseTail list = 
            list |> List.take (List.length list - 1)
        
        let split = 
            match (startIndex, endIndex) with
            | (Some starti, Some endi) -> 
                let (beforeSection, sectionAndAfter) = lines |> List.splitAt (starti)  
                let (sectionWithMarkers, afterSection) = sectionAndAfter |> List.splitAt (endi + 1)
                let sectionWithoutMarkers = sectionWithMarkers |> (reverseTail >> List.tail)
                (beforeSection, sectionWithoutMarkers, afterSection)
            | (None, _) | (_, None) -> (lines,[],[])
        split

    let readSection stream sectionId () = 
        let (_, section, _) = StreamExtensions.Stream.ReadAllLines stream  |> splitOnSection sectionId
        section
    // probably use partition to get up until section start, and up until section end, can use the pairwise grouping trick if I need to identify the line after section end
    // I'd say I should always ensure the section first... I think?

    let writeSection stream sectionId lines = 
        let (before, section, after) = StreamExtensions.Stream.ReadAllLines stream  |> splitOnSection sectionId
        let updatedLines = List.concat [before; [getStartMarker sectionId]; lines; [getEndMarker sectionId]; after]
        StreamExtensions.Stream.WriteAllLines stream updatedLines

