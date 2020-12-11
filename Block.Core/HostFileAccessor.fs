module HostsFileAccessor
open System
open System.IO
// open FSharpx.Text
// open System.Text.RegularExpressions
open FSharp.Text.RegexProvider

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
    let create = (Meta)

type HostRecord = 
    | Parsed of IP * DomainUrl * Meta option // I think this needs to return to a record and manage unknown a different way, I expect everything in the section to be valid, maybe just ignore invalids and only write back successes
    | Unknown of string 
    // may want to differentiate section records from other file bits. In part because I want to leave everything else untouched and 

type FileLine = 
    | SafeSection of HostRecord 
    | UnsafeSection of string

type AddRecordError = 
    | Other of string // uhh, I'm sure there will be reasons 

type RemoveRecordError = 
    | Other of string

type SectionId = | SectionId of string

type AddRecord = SectionId -> DomainUrl -> IP -> Result<unit, AddRecordError> 

type RemoveRecord = SectionId -> DomainUrl -> IP -> Result<unit, RemoveRecordError>

type GetRecords = SectionId -> HostRecord list

let defaultHostPath = "c:/windows/system32/drivers/etc/hosts"
// let getDefaultHostStream () = System.IO.File.Open(defaultHostPath, )
// what do I pass to this? A text writer? A file location? a Reader monad?


type HostRecordRegex = Regex< @"(?<ip>)\s+(?<domain>)#*(?<meta>.*)">
// ok, let's just do the thing and refactor as needed
let getRecords sectionId = 
    let rawLines = File.ReadAllLines(defaultHostPath)

    let parseLine line = 
        let maybeParsed = HostRecordRegex().TryTypedMatch(line)
        match maybeParsed with 
        | Some record ->
            let ip = IPAddress.create record.ip.Value
            let domain = DomainUrl.create record.domain.Value
            let meta = 
                match record.meta.Value with
                | "" -> None
                | nonEmpty -> Some (Meta.create nonEmpty)
            Parsed (ip, domain, meta)
        | None -> (Unknown line)

        //TODO: need to deal with the possibility of the sub-types not passing validation
    rawLines |> List.ofArray |> List.map parseLine 

let writeRecords records = 
    