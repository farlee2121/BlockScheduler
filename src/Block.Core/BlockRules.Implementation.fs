module BlockRules.Core.Implementation
open BlockRules.RuleManagement
open System;


module List =
    let return' item = [item] 



type ValidateRuleName = string -> RuleName
type ValidateBlockTarget = UnvalidatedBlockTarget -> ValidatedBlockTarget
type ValidateBlockTrigger = UnvalidatedBlockTrigger -> ValidatedBlockTrigger
type ValidateBlockRule = ValidateRuleName -> ValidateBlockTarget -> ValidateBlockTrigger -> ValidatedBlockRule
type InitializeRuleState = ValidatedBlockRule -> StatefulBlockRule


type SaveBlockRule = StatefulBlockRule -> unit
type GetBlockRule = RuleId -> StatefulBlockRule


// activity management

// let createBlockRule validateBlockRule initializeState createBlockRuleCommand =
//     // this requires active and inactive states to include all of the rule data... 
//     validateBlockRule createBlockRuleCommand.Data
//     |> initializeState
//     |>
//     ()

type ActivateRule = StatefulBlockRule -> RuleActivationEvent
type DeactivateRule = StatefulBlockRule -> RuleActivationEvent

type ActivityTransitions = | Activated | Deactivated | Unchanged

type IsTimeTriggerActivated = DateTime -> TimeTrigger -> bool
type GetTransitionState =  (StatefulBlockRule -> bool) -> StatefulBlockRule -> ActivityTransitions

let isTimeTriggerActive (time:DateTime) timeTrigger = 
    time.Hour = timeTrigger.Hour && time.Minute = 

let isTriggerActive triggerContext trigger = 
    match trigger with
    | Time t -> isTimeTriggerActive triggerContext.Time t

let isAnyTriggerActive context triggers =
    triggers 
    |> List.map (isTriggerActive context)
    |> List.reduce (&&)

let (|Activelike|Inactivelike|) rule =
    match rule with
    | Active _ | PendingUpdate _ | PendingDelete _ -> Activelike
    | Inactive _ -> Inactivelike

let getRuleTriggers rule = 
    match rule with 
    | Active r -> r.BlockTriggers
    | Inactive r -> r.BlockTriggers
    | PendingDelete r -> r.BlockTriggers
    | PendingUpdate r -> r.Current.BlockTriggers

let isRuleTriggered triggerContext rule = 
    isAnyTriggerActive triggerContext (getRuleTriggers rule)

let getTransitionState : GetTransitionState = fun isTriggered rule ->
    match rule, isTriggered rule with 
    | Activelike, true 
    | Inactivelike, false -> Unchanged
    | Activelike , false -> Deactivated
    | Inactivelike , true -> Activated 
    
let createEvents getTransitionState rule =
    let createDeactivation ruleId = (RuleActivationEvent.Deactivated (BlockRuleDeactivated ruleId))
    match getTransitionState rule with
    | Activated ->
            match rule with
            | Inactive r -> RuleActivationEvent.Activated (BlockRuleActivated r.Id) |> List.return'
            | Active _ | PendingUpdate _ | PendingDelete _ -> []
    | Deactivated-> 
        match rule with 
        | Active r -> [createDeactivation r.Id]
        | Inactive r -> []
        | PendingDelete r -> [createDeactivation r.Id;
                                (DeleteApplied (RuleDeleted r.Id))]
        | PendingUpdate r -> [createDeactivation r.Current.Id;
                                (UpdateApplied (RuleUpdated r.Pending))]
    | Unchanged _ -> []

let UpdateRuleActivity ruleList getTransitionState ruleContext : RuleActivationEvent list = 
    
    let isRuleTriggered' = (isRuleTriggered ruleContext)
    let getTransitionState' = getTransitionState isRuleTriggered'

    ruleList
    |> List.map (createEvents getTransitionState')
    |> List.collect id


let CreateRule validateRule createCommand : Result<RuleCreated, ValidateRuleError list>=
    // and Id needs to be assigned in here somewhere
    let ruleId = RuleId (Guid.NewGuid ())
    let validated = validateRule ruleId createCommand.Data
    validated |> Result.bind (RuleCreated >> Ok) 

let UpdateRule validateRule getCurrentState updateCommand = 
    // hold it, we need to get current state
    let (ruleId, unvalidatedRule) = updateCommand.Data
    let validated = validateRule ruleId unvalidatedRule

    let currentRule = getCurrentState ruleId 
    let toUpdateEvent updated current =
        match current with
        | Active r -> (r.Id, {Current = r; Pending = updated}) |> RuleUpdateQueued.UpdatePending |> UpdateEvents.Pending 
        | PendingUpdate pending -> (pending.Current.Id,  {pending with Pending = updated }) |> RuleUpdateQueued.UpdatePending |> UpdateEvents.Pending 
        | Inactive _ -> updated |> RuleUpdated.RuleUpdated |> UpdateEvents.Updated 
        | PendingDelete _ -> NoUpdate 
      
    match currentRule with
    | Some rule -> validated |> Result.bind ((toUpdateEvent rule) >> Ok) 
    | None -> Error ValidateRuleError.UnknownRule

let DeleteRule getCurrentState deleteCommand =
    let ruleId = deleteCommand.Data
    let toDeleteEvent current =
        match current with
        | Inactive r -> r.Id |> RuleDeleted.RuleDeleted |> DeleteEvents.Deleted  
        | PendingUpdate pending -> pending.Current.Id |> RuleDeleteQueued.DeletePending |> DeleteEvents.Pending
        | Active r ->  r.Id |> RuleDeleteQueued.DeletePending |> DeleteEvents.Pending 
        | PendingDelete r -> r.Id |> RuleDeleteQueued.DeletePending |> DeleteEvents.Pending  

    let currentRule = getCurrentState ruleId 
    match currentRule with
    | Some rule -> (toDeleteEvent rule) |> Ok
    | None -> Error ValidateRuleError.UnknownRule