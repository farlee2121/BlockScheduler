module HostsFileAccessorModel
open System


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
