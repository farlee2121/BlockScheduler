namespace BlockRules.RuleManagement
open System

// module Result = 
//     let traverse results = 

type Command<'data> = {
    TimeStamp: DateTime;
    Data: 'data
}

type UnvalidatedBlockTarget = Site of string
type ValidatedBlockTarget = Site of string

type UnvalidatedTimeTrigger = {Start: int * int; End: int * int}
type UnvalidatedBlockTrigger = | TimeTrigger of UnvalidatedTimeTrigger

type Time = { Hour: int; Minute: int } 
module Time = 
    let create hour minute = 
        if (hour < 0 || 23 < hour) then Error "Hour must be between 0 and 23"
        elif (minute < 0 || 60 < minute) then  Error "meow"
        else Ok { Hour = hour; Minute = minute}
    let (<==) left right =
        left.Hour <= right.Hour && left.Minute <= right.Minute

type TimeTrigger =  { Start: Time; End: Time }
    
// module TimeTrigger = 
//     let create {Start = (startHour, startMinute); End = (endHour, endMinute)} =
//         let startResult = Time.create startHour startMinute
//         let endResult = Time.create endHour endMinute

//         match startResult, endResult with
//         | Ok s, Ok e ->
//         // Result.map (fun start End -> {Start = start; End = End})
//         // startResult |> Result.bind endResult 

type ValidatedBlockTrigger = TimeTrigger of TimeTrigger
module ValidatedBlockTrigger = 
    let isTimeTrigger trigger = 
        match trigger with 
            | TimeTrigger t -> true

type RuleName = RuleName of string
module RuleName = 
    let private nameRegex = (System.Text.RegularExpressions.Regex ("[\w]+[\w\-\s]+", System.Text.RegularExpressions.RegexOptions.None))
    let create (name: string) =
        nameRegex.IsMatch(name) && name.Length <= 100

type UnvalidatedBlockRule = {
    Name: string
    Targets: UnvalidatedBlockTarget list;
    BlockTriggers: UnvalidatedBlockTrigger list
}

type RuleId = RuleId of Guid

type ValidatedBlockRule = {
    Id: RuleId;
    Name: RuleName;
    Targets: ValidatedBlockTarget list;
    BlockTriggers: ValidatedBlockTrigger list
}




type RuleListItemModel = {
    RuleId: RuleId
    // the block target
    // StateIndicator: 
}

type CreateBlockRuleCommand = Command<UnvalidatedBlockRule>
type RuleCreated = | RuleCreated of ValidatedBlockRule
type ErrorReason = string

// do I need to have all the details in here?
type UpdateRuleCommand = Command<RuleId * UnvalidatedBlockRule>
type PendingRuleUpdate = {Current: ValidatedBlockRule; Pending: ValidatedBlockRule}
 
type RuleUpdated = | RuleUpdated of ValidatedBlockRule
type RuleUpdateQueued = | UpdatePending of RuleId * PendingRuleUpdate // this needs more info?
type UpdateEvents = 
    | Updated of RuleUpdated 
    | Pending of RuleUpdateQueued
    | NoUpdate
type ValidateRuleError = 
    | InvalidName of ErrorReason
    | InvalidTarget of (UnvalidatedBlockTarget * ErrorReason)
    | InvalidTrigger of (UnvalidatedBlockTrigger * ErrorReason) 
    | UnknownRule of ErrorReason

type DeleteBlockRuleCommand = Command<RuleId>
type RuleDeleted = | RuleDeleted of RuleId 
type RuleDeleteQueued = | DeletePending of RuleId
type DeleteEvents = | Deleted of RuleDeleted | Pending of RuleDeleteQueued
type RuleDeletedError = | InvalidRule | Unknown of ErrorReason

type BlockRuleActivated = BlockRuleActivated of RuleId
type BlockRuleDeactivated = BlockRuleDeactivated of RuleId
// what about pending actions?
type RuleActivationEvent = | Activated of BlockRuleActivated | Deactivated of BlockRuleDeactivated | DeleteApplied of RuleDeleted | UpdateApplied of RuleUpdated

type StatefulBlockRule = 
    | Active of ValidatedBlockRule
    | Inactive of ValidatedBlockRule
    | PendingUpdate of PendingRuleUpdate
    | PendingDelete of ValidatedBlockRule

type TriggerContext = {Time: DateTime} // could include modes, location, etc


// remember that workflows take commands and return events
type CreateBlockRule = CreateBlockRuleCommand -> Result<RuleCreated, ValidateRuleError list>
type UpdateRuleSchedule = UpdateRuleCommand -> Result<UpdateEvents, ValidateRuleError list>
type DeleteBlockRule = DeleteBlockRuleCommand -> Result<DeleteEvents, RuleDeletedError>
type UpdateRuleActivations = TriggerContext -> RuleActivationEvent list

// read models
type ListBlockRules = unit -> RuleListItemModel list
// type ViewRuleDetails = RuleId ->  option // hmm, I would want to display different state here based on activation... I probably need a different model here


// internal