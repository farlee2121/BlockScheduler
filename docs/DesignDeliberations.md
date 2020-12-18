
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
```


I was thinking
```fs
type FixtureTest<'a> = 
    | Complete of Expecto.Test
    | Incomplete of ('a -> Test) // Idea is that I can apply sequencing, labels, etc in this function
    | List of FixtureTest<'a> list
       // is there any way I can make this so that I don't have to wrap every expecto function...
    // I could if the local context has some sort of post-application continuation... maybe ('a -> test)

```

