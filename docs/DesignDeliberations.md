
## How to handle block schedules
Decision: I'm not going to handle date ranges. Blocking will be decided on a weekly-only basis

I want to support scenarios like
- always blocked
- Block on certain days all day
- Block during a certain time every day
- Block multiple time ranges every day

I could force the user to specify multiple rules if they want multiple time ranges per day...

Property: a time range always needs a day, even if the day is implicit

Option: A list of days with an explicit time range. Empty day list implies all
- pro: matches my mental model
- con: less flexible under change
  -  can't easily adapt to a day or date option for pairing
- con: more implicit rules
- con: makes days special while not treating time ranges the same
- idea: it would make more sense if I framed these as "constraints". The default is always and these are limiters.
  - that preserves conceptual grouping of constraints
Option: Pair one day with a time range at a time. Leave it to the UI to translate special ranges into day/range pairs
- pro: Most flexible, acts as building block for larger schedules
- con: verbose



## Uniqueness of rules?
what to do about uniqueness of blocks. do I let them create multiple rules per domain? I think yes



## How to Represent Block vs Save
Schedules mean that I can't just use the host file as my data persistence. 

How do I deal with the difference between. Create and Enacting a block...
I don't think I need to worry about it much. I'm realizing that the user's editing of rules is pretty much separate from blocking and unblocking. Blocking is wholly managed by a background service or enacted on existing rules

I should maybe create seprate modules for rule editing vs enactment


## How should the host accessor know about the host file?
option: inject a path
option: inject a writer
option: inject an api in reader monad style

The host file is intrinsically a file, which makes me think that I could just pass a path or writer
It would be hard to unit test if the arg was just a file path
Unit testing would be easiest with the api
- would allow spys
- is this too many layers of abstraction?
- what would the api do? I think just whole file reads or writes
  - If I relegate the section logic then it could make testing easier, but reduces the safety of the library
  - If only the api knows about sections, the the main library could work the same on the whole file or on just a section... probably

What logic does the host accessor have that I care about reusing across different storage mechanisms?
- parsing is not reusable (well, it is for any text, but that's covered by a stream)
- Maintaining unparsable lines / comment lines is also specific to unstructured text
- The types could be reused, but the methods would need to be pretty much completely rewritten

DECISION: I'm not going to worry about abstracting the host accessor from the fact that it works on text


## Database access

Does it make sense to separate a manager and accessor?
- I don't think it is likely a big enough domain to separate them. 
- Hmm. I think this case is effectively a manager, but where the adapter is sufficient for data access
- What would cause me to split off a different accessor?
  - association of the data to different entitites in different contexts
  - non-trivial logic around access or managing owned types

Examples seem to be using a Reader-like approach
- https://www.red-gate.com/simple-talk/dotnet/net-development/creating-your-first-crud-app-with-suave-and-f/
- https://codereview.stackexchange.com/questions/82260/crud-database-layer-for-f-with-typeproviders?newreg=a857a7b328624470b2908907ede5b561

Should my manager methods accept each of the adapters as a whole contract (reader monad style) or should they accept only the actions they need?
- only the actions they need 
  - con: is a bit of a pain to apply
  - pro: but makes for easier composition.
  - pro: only need to define as many of the adapter methods as are required by the methods I use
  - ?: makes it less clear if certain methods are expected to be related 
- Reader-style
  - pro: allows command pattern where dependencies can be applied consistently accross actions.. except probably not because I would have to bundle all dependencies of all methods in the service

## Test Api

Using a builder with individual parameters making up the testApi failed because it leaks state between tests


It look like there are four viable solutions
- Pass a class constructor, and the class is IDisposable
  - Con: requires an explicit interface if I want multiple implementations
- Use testFixture to manage setup and teardown
  - Con: a list of partial tests is not a list of tests and cannot be composed in a test list or labeled without applying the fixture
  - I could pass the factory down through the builders, letting each layer treat it's return as a test list
  - Pro: idomatic to FP
  - Con: !!! Fixtures don't work with property-based tests. This is a pretty big limitation. 
    - Hmm. I might be able to manually wrap the property test in the fixtureFactory (since it takes a function to run...). Hmm, but they how
- I thought I could just pass a function that takes unit, but that doesn't let me manage cleanup
- Oh! The constructor/provider can return Dispose as part of the API, A bit messy, but works
- a testfixture/testApi reader monad expression
  - leave the api application implicit / part of the computation expression

There doesn't appear to be a way to implement IDisposable on a record or tuple

The computation expression doesn't appear to gain me a whole lot, still need to call a get function https://fsharpforfunandprofit.com/posts/monadster-3/#introducing-getm (which isn't better than calling a constructor every test).
Using a reader monad isn't it either. It is functionally no different than how testFixture works (returning potential tests awaiting a fixture factory)


!!! It is worth seeing how the `TestCaseBuilder` is [implemented](https://github.com/haf/expecto/blob/2733bea93f4015214dcbcb394ff8cf5f42782206/Expecto/Expecto.fs#L146).
It shows how a computation expression can be created with different configuration or internal state

New options
- Use `testCase` instead of `test` so that I can wrap every lambda in a fixture
  - the problem is that the type inference can only handle the parameter having one type signature. While every normal test outputs unit, every property test might require different additional parameters. It can't sneakily compile multiple versions like it does with direct references, because they all come through the same function parameter
- create a new test constructor function for tests that use the testApi
  - the same inference issues happens

The inference problem wouldn't exist if I applied the fixture at a higher level, but that again runs into the issue mixing tests and partial tests.
I now remember Wlaschin's words "There is no problem you cannot fix by wrapping it in a type". I could create a union of either a test or a fixture-awaiting test.
This can be collapsed into proper tests at a higher level. They are also all one type so I can compose them as desired. This will also warn me if I try to mix tests of different test api. Those will need to have their fixtures applied separately (always would, even in an OO case)

Here is the Test union in expecto https://github.com/haf/expecto/blob/6575e4475e6e12775b4449fe8c299de191ab7b01/Expecto/Model.fs#L84.
I think i'd pretty much have to copy the whole thing if I want to support labels...

Hmm, this is highly relevant https://github.com/haf/expecto/issues/352#issuecomment-541075223
- something like this would work pretty well if I went the disposable route
```fs
let testWithDispose disposable name code =  
  let testWrap () =
    use resource = disposable () // will this go out of scope if I return a function-value?
    code resource ()
  testCase name testWrap

let testPropertyDispose disposable property = 
  // I can wrap to pass in instances, but not to dispose, but I could directly call dispose in the property

// Super hacky, but I could specify a constant generator and have FsCheck pass the testApi. it muddies the logs... but I could customize the logs if needed
// Grr, that still doesn't allow for me to dispose...
// the every function is called by OnArguments, so I could leverage that to dispose after every test 

let testPropertyDispose disposable property = 
    let prop' =
         let dInst = disposable ()
         prop dInst
    let dispose default' = (fun config name testNum (args:obj list) -> 
                            args |> List.map (function 
                            | :? System.IDisposable as dis -> dis.Dispose()
                            | _ -> () ) |> ignore
                            default' config name testNum args
                        ) 

    testPropertyWithConfig { FsCheckConfig.defaultConfig with receivedArgs = dispose FsCheckConfig.defaultConfig.receivedArgs } name prop' 

// this would also work with other datastructures like including dispose in a record, or converting the record to a disposable
// This model would also definitely work with a computation expression / builder if I felt like that was meaningful
```

Following the disposable line of thought. I create a `TestWithApiBuilder` that takes a setup and teardown function. Each function accepts an environment / state that the computation expression handles passing off (State monad style?). I need to make sure that the expression can handle it that way... I still might run into the same inference issue. Well, the inference would only be a problem if I'm trying to partially apply, not if the setup outputs the environment
```fs
type Setup = () -> 'api * 'env
type Cleanup = 'api * 'env -> 'a

let testWithEnv setup cleanup name builder = //..
let testPropertyWithEnv setup cleanup name predicate = //...
let testPropertyWithEnvAndConfig config setup cleanup name predicate = //...
```

Hmm. This is a good setup but the computation expression is actually no bueno. I'd still need a getter with any expression. Though I could certainly provide that if I want to use the expression, but I don't think I do.

QUESTION: Do I really need a separate interface? Would it be able to use both types as long as they meet the right structural signatures?
TEST: I tried creating an object with the same signature as my record type. F# wasn't able to infer the class satisfied the requirements. I had to explicity add a type annotation
```fs
type SectionTestApi () =
    member this.GetRecords () =
        []
    member this.WriteAll records = 
        ()
```

Interfaces didn't make it better. It wouldn't compile even after annotating to take the interface. I'd have to fix the output type the test list expects in the interface for this to work reasonably. The definitions aren't really any more burdensome than records. The ability to wrap the mutable state is really quite nice. However, the loss of type inference is a big blow.
```fs
type ITestApi<'a, 'err> = 
    abstract member GetRecords: unit -> Result<HostRecord list, 'err>
    abstract member WriteAll: HostRecord list -> 'a
    inherit System.IDisposable

type InMemoryTestApi () =
    let mutable records = []
    interface ITestApi<unit,string>  with 
            member this.WriteAll lines = records <- lines 
            member this.GetRecords () = Ok records
            member this.Dispose () = ()
```
I think this is a big reason to go with a more idomatic structure like a environment/api tuple


I was thinking
```fs
type FixtureTest<'a> = 
    | Complete of Expecto.Test
    | Incomplete of ('a -> Test) // Idea is that I can apply sequencing, labels, etc in this function
    | List of FixtureTest<'a> list
       // is there any way I can make this so that I don't have to wrap every expecto function...
    // I could if the local context has some sort of post-application continuation... maybe ('a -> test)

```




### Tooling sidenote

I was using https://marketplace.visualstudio.com/items?itemName=formulahendry.dotnet-test-explorer&ssr=false#overview
  Pros
  - can sometimes figure out going to source in F#
  - can run tests in parallel
  - Has run and debug code lenses in c#
  - tree mode settings [full, merged, flat]
  - has key bindings by default

  Cons
  - Slow on discovery
  - Testing also kinda slow
  - Need to manually run test discovery

  The speed issue appears to mainly be because of build time. Could solve by using vstest or testing with `--no-build` (and maybe building first if when no test dlls present or leaving it to the user)
  - looks like they let you turn off build https://github.com/formulahendry/vscode-dotnet-test-explorer/blob/ebe7779a1cb0927e21c6899e8943d6d09a8367c7/src/testCommands.ts#L263

This [newer plug-in](https://marketplace.visualstudio.com/items?itemName=derivitec-ltd.vscode-dotnet-adapter)
  Pros
  - Go to code *and* go to test both available in c#
  - Faster test discovery
  - automatically discovers tests on build (even if you build from somewhere else)
  - run and debug buttons in explorer
  - Run and debug codelense
  - appears that it would collect tests from multiple languages in one place

  Cons
  - cannot navigate to code in f#
  - Test discovery very slow if you don't manually set a search pattern in settings
  - can't add runsettings,
    -  but it'd be easy to contribute (add setting to package.json and append it whenever vstest is called)
  - no default keybindings
    - easy to contribute, add to local settings, or standardize for a company in custom package

  The only issue not easily fixable is that it is hard to see the proper test path and I have no control over how tests display in the tree (i.e having separate every level into a tree branch)


## State-based managers?
Inspired by: https://fsharpforfunandprofit.com/posts/calculator-complete-v2/

How could standard crud be state-based?
- the state transitions are more for individual items...
- I think the entity itself would have more notion of state, rather than the repository
  - I could track state for each rule, and only save/update rules. 
  - This way I would only have list and update operations at the edge. Everything else is an operation on the state of an individual entity...


## Bolero vs Blazor
Why is this such a hard decision?
- F# already has type providers OpenApi. I wouldn't have to write excess client mapping (which is always annoying)
  - I think I'd still have to write the API... not sure
- C# has generators now, but there doens't seem to be mature options for build-integrated client generation
- I want F# as my backend, and don't want to deal with paradigm conflicts from a paradigm handoff
  - On the other hand, bolero is a side project. It'll lag behind the capabilities of Blazor. there will likely be framework compatability issues
  - Well... It shows that we can use blazor components within the elmish model... https://fsbolero.io/docs/Blazor
- I really want cross-platform abilities, which don't seem like they'll be available for bolero, but maybe that's false
- I don't really have familiarity with Blazor to contrast the two options

Way forward
- [ ] create UI-only experiment in Blazor (the more sure and stable option)
- [ ] create same experiment in Bolero
- [ ] Try to add mobile bindings to both

First, let's tackle capability-based experiment with a console client


# Capability Refactor
I think the background service shouldn't really be capability-based...
Well, I suppose that it still could be. It wouldn't make much difference though, since there will just be one call that does everything internally. It is the start of authority and there is no follow up.

The start of authority for rule management is the list. 

REALIZATION: Capabilities are intrinsically context-based. They reflect what we expect the end user to be able to do in the given use case. The same underlying functions might be chained differently in different contexts. This is **orchestration**, exactly what managers are supposed to do
- in the workflow manager style, use case managers could be configurations of one manager that can turn response actions on and off

QUESTION: Do I make any methods public other than the entry point and "ProcessCapability"?
- Pro: keeps the followup capabilities flexible, only the manager needs to know about the split
- Con: more opaque what the manager can handle. Have to look at the capability type as well as the interface
- What is the line between manager-capability handlers and global?
  - if global, i'd save some duplicate code, but i'd create a lot of coupling. It'd also make the urls easier in an api-based scenario
  - Managers are supposed to be services. A client could be ok with one endpoint, but the managers can't do that because they may not know about each other and they would have to in order to share a capability endpoint

QUESTION: How do I handle actions that require some unpredictable (non-discrete) user input? Like entering a name?
- the different actions need to take arguments
- How does the client know what arguments an action can take?

Lists some examples of HATEOAS: https://medium.com/openlight/the-trouble-with-hateoas-3ed0da733072
- Github, paypal, aws appstream
- https://docs.github.com/en/free-pro-team@latest/rest/reference/pulls
  - github does it in a way that still has predictable urls
  - All of the page actions are passed in the response. This means the server decides if an action is available or what the link to it looks like. None of tat is encoded in the UI. However, it takes discipline to not use knowledge of the predictable urls

Question: What are the criteria for the client side to change it's api knowledge?
- Well, I feel like the UI has to know about the different kinds of links it could recieve and how it translates those into the UI
- It doesn't change if the url for an action changes, but it does if any actions are *added* or get a new identifier
- The old client can have actions taken away without an update. It can also work as before, without new features, as long as no actions change identifier
- Again, what about the arguments. How does it know the valid args?
  - opt 1: shared knowledge/model with server (introduces potential breakage and more need for version sync)
  - opt 2: args and constraints encoded in the payload
    - con: a bigger payload
    - pro: the UI can keep up as long as available input constraints don't change
    - con: need a somewhat sophisticated input control generator (to choose right inputs and validations)
