module BlockRuleManager
open System

// need schedule, domain,... pretty much it
// Don't want a domain to unblock until the end of it's current block, or at latest by the end of day. Removed right away if not an active block

type Hour = | Hour of int
module Hour =
    let create zeroBasedHour =
        match zeroBasedHour with
        | n when 0 <= n && n <= 23 -> Some (Hour zeroBasedHour)
        | _ -> None  

type Minute = | Minute of int
module Minute =
    let create minute = 
        match minute with
        | n when 0 <= n && n <= 60 -> Some (Minute minute)
        | _ -> None  

type TimeOfDay = | TimeOfDay of Hour * Minute
                    member this.ToDateTime () = 
                        match this with
                        | TimeOfDay (Hour h, Minute m) -> DateTime(year = 0, month = 0, day = 0, hour = h, minute = m, second = 0)

type TimeRange = { Start: TimeOfDay; End: TimeOfDay }

type Day = | Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
// how do I want to handle special cases like Weekdays, Weedends, Work hours, Permanent/All the time
// OH! I could let them define their own special time ranges

type Schedule = Day * TimeRange list // hmm needs to be non-empty

type Blockable = 
    | Domain of string

type RuleId = | RuleId of Guid

type BlockRule =  RuleId * Blockable * Schedule

type CreateRule = Blockable -> Schedule -> Result<RuleId, string list>

type UpdateRule = BlockRule -> Result<RuleId, string list>

type PauseRule = RuleId -> Result<RuleId, string list>

type DeleteRule = RuleId -> Result<RuleId, string list>

type ListRules = unit -> BlockRule list



type Block = RuleId -> unit // this should take a writer monad somewhere 
type Unblock = RuleId -> unit
//type GetActiveRuleIds = DateTime -> RuleId list // this will require comments/meta
type GetScheduledRules = DateTime -> RuleId list
// type WriteActive
// Do I want to Block/unblock from the top-level or just write the active blocks and let a lower level figure out the diff?
