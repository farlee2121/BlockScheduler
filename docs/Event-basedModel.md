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
  - if block running, don't remove until normal period finished or end of day

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
type CreateRuleError = 
    | InvalidName of ErrorReason
    | InvalidTarget of (UnvalidatedBlockTarget, ErrorReason)
    | InvalidTrigger of (UnvalidatedBlockTrigger, ErrorReason)
    | Unknown of ErrorReason

type UpdateRuleCommand = Command<(RuleId, UnvalidatedBlockWindow list)>
type RuleUpdated = | RuleUpdated of RuleId
type UpdateRuleError = 
    | InvalidName of ErrorReason
    | InvalidTarget of (UnvalidatedBlockTarget, ErrorReason) 
    | InvalidTrigger of (UnvalidatedBlockTrigger, ErrorReason) 
    | UnknownRule 
    | Unknown of string

type DeleteBlockRuleCommand = Command<RuleId>
type RuleDeleted = | RuleDeleted of RuleId
type RuleDeletedError = | InvalidRule | Unknown of string

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

type BlockRule = 
    | Unvalidated of UnvalidatedBlockRule
    | Validated of ValidatedBlockRule
    | Deleted of RuleId

type RuleTransforms = 
    | Create of CreateBlockRuleCommand
    | Update of UpdateRuleCommand
    | Delete of DeleteBlockRuleCommand
    // activate?


type ValidateRuleName = string -> RuleName
type ValidateBlockTarget = UnvalidatedBlockTarget -> ValidatedBlockTarget
type ValidateBlockTrigger = UnvalidatedBlockTrigger -> ValidatedBlockTrigger
type ValidateBlockRule = ValidateRuleName -> ValidateBlockTarget -> ValidateBlockTrigger -> ValidatedBlockRule
type SaveBlockRule = BlockRule -> unit
type GetBlockRule = RuleId -> BlockRule


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
TODO: refactor for named rules with multiple targets, finish defining rule state models
- the states are a bit muddled, validated, activated, inactive, and unvalidated are not really alternative states
- Unvalidated, Validated, activated, deleted
  - this could be good, when activating i don't ever activate an unvalidated or deleted rule. Already active rules need not be activated again...

I think that I can leave the list read model as-is. I don't expect them to have different actions based on state from the list view, that will come with the details view

I think I might want to move all the rule crud to a statemachine
- this would couple a few workflows, but centralize rules on state
- It would require me to have full state objects as input
- would simplify the IO commands to just `GetRule = ruleId -> Rule` and `SaveRule -> Rule -> unit`
  - pro: I put off consideration of state representation
  - con?: I'm not sure how this relates to optimization. Batch operations should be attainable in the adapter, but what about cases where we need 
- Q: how does the stateMachine know all the different actions?
  - I'd have to create extra states like `UndeletedRule` and `UpdatedRule of UnvalidatedBlockRule`
  - I could also match on an action. That would allow me to push evaluation of both expected state and current state into the state machine
- this highlights a potential issue. My current design relies on the background task to apply rules, but if a rule is created that should be currently active, then I want to activate it right away



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


## Key takeaways
- need to think more on where capabilities like, in or out of domain
- this model enables great incremental development
  - less gap between event storm and code modeling
  - more knowledge of system represented in the model, surfacing issues faster
- CRUD takes the form of a state machine with `Action w/ data -> current state -> new state`
  - data is retrieved and persisted independent of state and saved the same, the actual persistent action is determined out of scope based on the state model
  - read models don't live in this state machine, so they need to pass enough data to fetch the existing state object