## Event Storm

reminder
1. events: past tense
2. sources:
   1. commands: imperative language
   2. external/time-based: not sure. no clear guidance yet. I'd guess past-tense referencing the external or time event that cause it
3. look for sub-domains/contexts
4. workflows
5. model
The chain of events seems very short...


- Site determined distracting/negative
  - Source: User
  - Command: CreateSiteBlock
  - Event: block hours set 
- block hours updated
  - source: user
  - Command: UpdateBlockSchedule
  - REQ: if block running, finish the current block period before applying updates
- ScheduledBlockStarted
  - Source: time / saved schedule
  - Time: `BlockStarted` triggered by registered schedule
  - note: minimum block increment is 1 minute. Expect blocks to be enforced to the minute.
- ScheduledBlockEnded
  - source: time / saved schedule
  - `BlockEnded`
- BlockRulesViewed
  - command: ListBlockRules
- BlockPaused 
  - source: user
  - command: `PauseBlock`
- BlockDeleted
  - source: user
  - REQ: if block running, don't remove until normal period finished or end of day

sub-domains
- it's so small i think there is only one domain context

Workflows
- Q: what is the guidance for separating workflows?
  - a workflow is a transform of business data
- creating a block
- updating a schedule
- pause a block
- delete a block
- monitor schedules?
  - yes. it might not change the persisted rule, but it does transform the state. It controls "active" and "inactive" rule state

Q: how should read-models be named and classified?

all the rule management workflows are likely to be available together

there could be other block triggers, like manually determined context, location, etc. I'm not likely to implement those though


## Domain re-evaluation
I implemented the model under the assumption that I want rules to be associated to a particular blocked resource.

This doesn't exactly reflect the domain though. I commonly want to block a list of things in a common time frame.

This muddies the identity of block rules. There are a few options
- require the use to name block rules, allow them to associate any number of blocked resources
- keep rules per resource, but allow users to define reusable time periods

Overall, I think the simpler solution is to give rules a name, and allow multiple resources. I think this also is more flexible and allows the user to shape the experience more how they want

There are also some potential extra commands

ActivateNow (ruleId, end time)
- introduces a new rule state: temporary triggers that are deleted on completion

## Model

```fs

type Command<'data> = {
    TimeStamp: Datetime;
    Data: 'data
}

type UnvalidatedBlockTarget = Site of string
type ValidatedBlockTarget = Site of string

type UnvalidatedBlockTrigger = Time of (Datetime, DateTime)
type ValidatedBlockTrigger = Time of (Datetime, DateTime)

type RuleName = RuleName of string
module RuleName = 
    let create name = "[\w]+[\w\-\s]+" && length <= 100 ///System.Text.RegularExpressions.

type UnvalidatedBlockRule = {
    Name: string
    Site: UnvalidatedBlockableResource;
    BlockTriggers: UnvalidatedBlockTrigger list
}

type ValidatedBlockRule = {
    Name: RuleName
    Site: ValidatedBlockTarget list;
    BlockTriggers: ValidatedBlockTrigger list
}

type RuleId = RuleId of Guid



type RuleListItemModel = {
    RuleId: RuleId
    // the block target
    StateIndicator: 
}

type CreateBlockRuleCommand = Command<UnvalidatedBlockRule>
type RuleCreated = | RuleCreated of RuleId
type ErrorReason = string

type UpdateRuleCommand = Command<(RuleId, UnvalidatedBlockWindow list)>
type RuleUpdated = | RuleUpdated of RuleId
type ValidateRuleError = 
    | InvalidName of ErrorReason
    | InvalidTarget of (UnvalidatedBlockTarget, ErrorReason) 
    | InvalidTrigger of (UnvalidatedBlockTrigger, ErrorReason) 
    | UnknownRule of ErrorReason

type DeleteBlockRuleCommand = Command<RuleId>
type RuleDeleted = | RuleDeleted of RuleId
type RuleDeletedError = | InvalidRule | Unknown of ErrorReason

type BlockRuleActivated = BlockRuleActivated of RuleId
type BlockRuleDeactivated = BlockRuleDeactivated of RuleId
type RuleActivationEvent = | Activated of BlockRuleActivated | Deactivated of BlockRuleDeactivated

// remember that workflows take commands and return events
type CreateBlockRule = CreateBlockRuleCommand -> Result<RuleCreated, CreateRuleError list>
type UpdateRuleSchedule = UpdateRuleCommand -> Result<RuleUpdated, UpdateRuleError list>
type DeleteBlockRule = DeleteBlockRuleCommand -> Async<RuleDeleted, RuleDeletedError>
type UpdateRuleActivations = DateTime -> RuleActivationEvent list

// read models
type ListBlockRules = unit -> RuleListItemModel list
type ViewRuleDetails = RuleId ->  option // hmm, I would want to display different state here based on activation... I probably need a different model here

type Get


type RuleManagement = {
    Create: CreateBlockRule;
    Update: UpdateRuleSchedule;
    Delete: DeleteBlockRule;
    List: ListBlockRules;
    Details: ViewRuleDetails;
}

// internal

type StatefulBlockRule = 
    | Active of RuleId
    | Inactive of RuleId
    | PendingUpdate of ValidatedBlockRule
    | PendingDelete of RuleId // need to be careful 
    | Deleted of RuleId



type ValidateRuleName = string -> RuleName
type ValidateBlockTarget = UnvalidatedBlockTarget -> ValidatedBlockTarget
type ValidateBlockTrigger = UnvalidatedBlockTrigger -> ValidatedBlockTrigger
type ValidateBlockRule = ValidateRuleName -> ValidateBlockTarget -> ValidateBlockTrigger -> ValidatedBlockRule

type initializeState = ValidatedBlockRule -> StatefulBlockRule

type SaveBlockRule = StatefulBlockRule -> unit
type GetBlockRule = RuleId -> StatefulBlockRule


// activity management
type GetRulesActiveAtTime = DateTime -> RuleId list // I think this needs refined
type SetActiveRules = RuleId list -> unit // this should internally resolve against existing active rules
type GetActiveRules = unit -> RuleId list

// need to figure out the read models as well
// I feel like what I really want in the read cases is a lazy and queryable collection of rules
// I can put it off like a side-effect, and have a dependency that magically materializes a list of rules active at a time. 
// that might be best. I can't filter any list of rules to ones currently active, but that also isn't my requirement...

// since there is no class representing the manager. Thin layers like the read models can just be directly implemented as adapters. The abstraction is just the function signature and alias

```
TODO: 
- [x] refactor for named rules with multiple targets, finish defining rule state models
  - the states are a bit muddled, validated, activated, inactive, and unvalidated are not really alternative states
  - Unvalidated, Validated, activated, deleted
    - this could be good, when activating i don't ever activate an unvalidated or deleted rule. Already active rules need not be activated again...

I think that I can leave the list read model as-is. I don't expect them to have different actions based on state from the list view, that will come with the details view

- [x] I think I might want to move all the rule crud to a statemachine
  - this would couple a few workflows, but centralize rules on state
  - It would require me to have full state objects as input
  - would simplify the IO dependencies to just `GetRule = ruleId -> Rule` and `SaveRule -> Rule -> unit`
    - pro: I put off consideration of state representation
    - con?: I'm not sure how this relates to optimization. Batch operations should be attainable in the adapter, but what about cases where we need to represent state in UI
      - concluded read model state is a bit different
  - Q: how does the stateMachine know all the different actions?
    - I'd have to create extra states like `UndeletedRule` and `UpdatedRule of UnvalidatedBlockRule`
    - I could also match on an action. That would allow me to push evaluation of both expected state and current state into the state machine
  - [ ] this highlights a potential issue. My current design relies on the background task to apply rules, but if a rule is created that should be currently active, then I want to activate it right away
  - I'm realizing that passing the action and current state is the same as having a statemachine per command

- [ ] balance activity state with other states
  - active rules need to move to "Pending Delete" and the activity watcher should enforce pending deletes until their schedule runs out or end of day, then delete them
  - potential here to require friend approval before removing
  - What info do an inactive and active rule need?
    - depends
      - OPT: have a CreatedRule state that looks something like `(ValidatedRuleData, state)`
        - pro: means I don't need all info available in later active/inactive models
        - Q: what about updated rules, they need the same enforcement
    - OPT: Directly return an Active/Inactive rule on create or update. means those states need to include all the data that `UnvalidatedBlockRule` does
    - OPT: split unvalidated out of states? no go, i'd still need a way of handling updated events
  - I also need a `PendingUpdate`, where an active rule does not apply update to activity state until the current trigger is over

- [ ] make sure storage is in response to events returned from workflow
- [ ] consider making real DTOs (With `toDomain` and `toDto` method)

state machine
- create
  - can only be unvalidated, so validate -> Active/Inactive
- Update
  - validation fails -> return error
  - validation succeeds -> 
    - active | PendingUpdate -> PendingUpdates
    - PendingDelete -> PendingDelete
    - inactive -> Active/Inactive
    - Deleted -> Deleted (error?)
- Delete
  - active | PendingUpdate  | PendingDelete -> PendingDelete
  - inactive -> delete
  - unvalidated, Deleted -> do nothing or error
- Activate (the only state that should be available is Inactive)
  - deleted -> error
  - _ -> Active 
- Deactivate
  - PendingUpdate -> Inactive (updated?)
  - PendingDelete -> Deleted
  - Unvalidated -> error
  - Deleted -> Deleted

Valid/invalid seems to be separate from the other states, I should probably treat it separately and only group the known valid states together for the machine
- this complicates what the validator should return, should it require someone else to pass it a constructor? should it just retun inactive and pipe through a separate function to determine activity?

## Modeling Errors
Per Chapter 10 of Domain Modeling Made functional
- i should model domain error like validation of input data
- I can choose to model or not model infrastructure errors
  - the app is small and the consequences of exceptions limited. The only strategy i'd really use if for users or the lowest level infrastructure to retry the action
  - I expect everything to be on one machine. I don't expect network interruptions or fickle operating conditions
  - thus I think I should just let exceptions be thrown and catch them at top level


## adding capabilities

If I were to add capabilities, they would pretty much all belong to the read models...
- ListBlockRules
  - ViewRuleDetails
  - potentially, delete
- ViewRuleDetails
  - Delete
  - Update
  - activate now?
  - back to list
- Delete
  - back to list
  - maybe undo?
- Update
  - See new details
  - back to list

Are capabilities part of the domain?
- I think it depends. Overall, I'd say yes, but they are relatively volatile.
- They can change with the user experience we want to provide. We may provide several different user experiences over the same functionality.
  - I suppose that the domain can define all allowable actions and the UI can decide to ignore some of them
  - There is no case where we want the UI to allow actions disallowed by our capability model


## Why am I stuck...
I feel like it's wrong to include all the triggers and block targets in active/inactive block rules.
Why? It feels like an excess of data...
Do I need that info to apply a block rule... yes, I do. However, i was planning on just passing ids because that feels less coupled.

The format for CRUD and the format for rule state are conflicting. However, there does need to be a relationship because there is a domain relationship between the actions...

Here's a thought. I need all that info to apply the block rule at some point. I can write a version taking all the info needed to apply the rule. 
If I have a context that uses less than that amount of info I can just wrap the method in an adapter that gets the full record and passes it in. That would actually be more composable because it pushes the data collection dependency out of the rule application.

Hmm. I'm realizing that I don't need the triggers and the targets together. I only need them one at a time...

```fs
let isTriggerListActive rules = 
  // check each type of rule

let applyBlockTargets targets =
  // match on each kind of target
```

In a world with unlimited resources, I would keep my activity logic in-app like this. I suppose a user isn't likely to have more than a few dozen block rules. It does feel wrong loading all the rules and cutting myself off from larger scale performance though...
- This is where datomic transducer-based queries would be nice. I could treat the DB just like it was an in-memory collection
- could I do that in f#... I don't think that sql type providers work with pattern matching...
  - here's a proof of concept post https://jefclaes.be/2015/10/bulk-sql-projections-with-f-and-type.html
  - It doesn't appear to be part of the query expression though https://docs.microsoft.com/en-us/dotnet/fsharp/language-reference/query-expressions
  - I'm not finding any good projection libraries. Makes sense. Datomic has the benefit of running clojure on both ends. F# would need to translate the expression to some other language like SQL and not everything can be mapped.
    - the other approach would be loading each record to the program. Could be individual, buffered,... any case would not be good performance wise. They all lose to a proper query

- So what do I do?
  - push activity determination to someone else?
  - expect a full list of rules and run through them in the core workflow...

If I pushed off activity the core flow would look something like
```fs
type RuleContext = {Time: DateTime} // could include modes, location, etc
let UpdateRuleActivity ruleContext getActiveRules getRulesTriggeredByContext activateRule deactivateRule = 
  let currentActiveRules = getActiveRules
  let triggeredRules = getRulesTriggeredByContext
  let deactivatingRules = Set.diff currentActiveRules - triggeredRules
  let deactivatedRules = deactivatingRules |> List.map deactivateRule
  //(fun rule -> match rule with | PendingUpdate -> | PendingDelete -> | Active -> Inactive | Inactive -> Inactive)
  let activatedRules triggeredRules |> List.map activateRule 
  List.concat activatedRules deactivatedRules
  |> List.map statusToEvents

```

This is kindof a mess. It can't decide if it works only ids or not. I could just have a `setActive ids`. That would push the diff behavior out and make the flow more focused. However, I want the activation/deactivation rules to be an assured part of the workflow. That requires me to know about the greater state of the rule...
I suppose that activate and deactive could return the events...

Key question: where is it appropriate to inject "getter" functions versus just take a proper list.
- hypothesis: I think the top-level workflow should probably take care of most of the getters. In most cases it can probably materialize a tight enough list to pass to child functions. 

Note: the id-only version still has me making plenty of queries. it forces me to get individual state in the looped `activate` and `deactivate` functions. This could be avoided if we didn't care about the relationship between rule updates and application, but is unavoidable as-is.

Let's assume I'll just load the whole rule list. What does the workflow look like

```fs
type RuleContext = {Time: DateTime} // could include modes, location, etc
type ActivityTransitionEvents = | Activiated rule of StatefulBlockRule | Deactivated rule of StatefulBlockRule
let UpdateRuleActivity getRules getTransitionState deactivateRule activateRule ruleContext  = 
  // let getTransitionstate = (fun rule -> match isRuleTriggered ruleContext rule.Triggers with | true -> Activated )
  getRules ()
  |> List.map getTransitionState
  |> List.map (fun transition -> | Activated -> activateRule | Deactivated > deactivateRule)
  // what do I want to return? events: activated, deactivated, unchanged? I need the new rule states to save off
  // where do I handle pending actions?
  // opt: save every rule in it's entirety. I do seem to need all the data for active/inactive/pending update in this view.
  // opt: leave it to the event handling. no, this pushes it out of the domain
  // opt: pass in a save function. reasonable, but a bit messy

  // realized that I don't need to save the whole block rule. I pass in stateful rules, but I pass out events. The events can contain less data than the state  
```

What about persistence?
- On one hand, I see managers as the head of the system, so I expect them to manage save behavior
- On the other hand, he doensn't demonstrate saving in the workflow.
  - He does demonstrates side-effects like sending the acknowledgement as events, which are returned...
  - DTOs at the least need to be determined outside of the workflow, but that works with either injected save methods or returning data to save

Further review of the book (pages 240-242) makes it clear that injecting functions for persistence actions is not meant to be part of the core workflow, only part of the composition root. 
- The goal is to separate decisions from storage of the decisions or reaction to the decisions.

This means I probably need more data in my events

Q: Are events ever created below the top level workflow?
- A: the book examples only create the events in the top level worflow, and only as the last step


TODO i'm not sure what to inject in the `UpdateRuleActivity` flow
  - volitile logic -> what consitutes a transition `getTransitionState` and what happens to a rule on activate / deactivate
  - I had that at first, but I collapsed it because passing the transition state was kinda confusing and created duplicate pattern matching. To create events i need to know what transition happened and the original state. Even if it's kinda duplicate I guess I should probably encode that info
  - Hmm. the problem is that there isn't any action taken other than the events. There is no persistence. Instead persistence happens relative to the events. Creating another round of pattern matching outside this workflow... 

notice: My desire it to act on the `StatefulBlockRule` directly and save it off. The issue is that this makes the assumption of a repository-style store. Yes, it could be adapted to event stream, but it reveals that there is some collection sitting out there that I expect to be able to mutate. 
- returning events doesn't make that assumption. It can easily be thrown away as a dry run.
- The issue with returning events is that I end up with nearly mirror representations of state inside and outside the workflow

I notice that i don't generally need the rule data inside my workflows. I mostly just need the states, except in the `UpdateRuleActivity` flow.
- I thought I would need the states to update the rules in the workflow, but the rules aren't updated in the workflow and the events don't need most of the available data for a downstream consumer to update the store
- this is good in that less knowledge is shared. it is tricky in that I feel like i'm leaving rules implied. I't does allow me to handle things like pending state differently though.
- 

## Thoughts from the process

Wow, this was so easy. I didn't feel like I waffled on decisions much, there were fewer sources to consider because I don't care about any implementation details. I only care about normalizing how I think about the problem space.

I especially like the categorization of workflows as transforms (separate from triggers and data). That helped me to unexpectedly think of the schedule monitoring as a workflow.
At first I didn't think it was a workflow because it is hidden from the user and doesn't modify rule info. However, it is causing a expected domain transformation on rule state, the active status. 

It was kinda unintuitive that I should pass commands and return events.
It does create more indirection in the type signatures, but that also means it pushes off assumptions about the implementation. In particular, I was unsure what to return on create and update events. I originally returned an Id like I currently tend to do in my systems. However, that doesn't make sense from a domain perspective. Returning the id as a result is an artifact of implementation and not a domain expectation 

I noticed a few times that I got lost in trying to make the model. I was trying to balance too much in my brain and lost focus.
I was always able to pick back up by looking back at the event storm artifacts

I presently feel like the types are piling up and I need to figure out how to organize them. I want to keep the commands together, but I also want to see the details of types expected by a flow all together. I can't have both.

At first I was hesitant to split the create and update error unions. They felt like they were pretty much the same thing, but I separated them for sake of the model. As I kept modeling, I ended up realizing more errors distinct to each command
 

The resuling model doesn't look so different from what I had, but the thought process that arrives there is more predictable and domain-driven rather than implementation- and experience-driven

I'm not sure how capabilities fit in here... I'd guess at a lower level than the command? 
- I suppose it can be implemented on either side. Since the command contains context data and the action performed, we could run capability authorization either inside or outside the individual command implementations. Most likely we'd add it as a wrapper function in the composition
- further actions would be part of the return type, but they don't belong to the domain model. Would I construct them in the wrapper as well?
- INSIGHT: I see managers as the head of the system, thus I expect them to return the capabilities. However, they can't directly return the capabilities because action references depend on the communication medium (urls if an http api, function pointers if in-process)
  - I suppose actions could be referred to by guids all around, then it's just a matter of setting up how medium-dependent formatters and handlers...


Using state-based data models also allows states like `DeletedRule`, this could be useful for archival purposes. And deleted rules can't be activated, updated, etc 


I'm really getting tripped up on state in read models
- also, how do I prevent writes to deleted rules? Is it really necessary to load the whole rule? 
  - -> see state machine deliberations
- The read model might want to display more info than is normally afforded for a given state (like details)
- I think the read model might have to define it's own states. 
  - While it models states separately, it could potentially reuse the state objects if it the display needs match the usage needs in a given context
- It doesn't feel like there is good model reuse between reads and transforms... And that may be the right call


It seems like really following the event, transform, and state decomposition pretty much just turns my managers into state machines, which is great.
- there can still be flow (like validating first), but it pushes a lot of conditional action out into the adapter.
- state machines are a more direct translation from the domain
- it also simplifies the simplest storage case. Since the cases of the entity are all under one type, we can just serialize the whole type and not worry different actions on the storage

I also got really tripped up on where to handle persistance. My past experience really wants to inject storage actions into the workflow

## Key takeaways
- need to think more on where capabilities like, in or out of domain
- this model enables great incremental development
  - less gap between event storm and code modeling
  - more knowledge of system represented in the model, surfacing issues faster
- CRUD takes the form of a state machine with `Action w/ data -> current state -> new state`
  - data is retrieved and persisted independent of state and saved the same, the actual persistent action is determined out of scope based on the state model
  - a state machine per command is the same as a state machine that take a command and a current state.
    - If I need to broker different kinds of actions, then I can aggregate on either side of the commands, depending on what is appropriate for the design
  - read models don't live in this state machine, so they need to pass enough data to fetch the existing state object

!!! I realized that commands can nest. I could include a list of  add-note/delete-note commands as part of the update-partner command. Then I could choose to call them together or separate without needing to write any new command handlers or event handlers
- there is also no ambiguity in resolving against current state because each level is in terms of an action (or difference)

I'm realizing command convention (event-based) makes for very flexible invocation. Endpoints don't really matter. The identity is contained in the command. Thus
- events can be nested / batched 
- events can be streamed/queued
  - we could serialize any subset of commands pretty easily
- events can be invoked individually
- events can be held for later execution
In a way, it makes our api a batch language