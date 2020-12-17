
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
- I thought I could just pass a function that takes unit, but that doesn't let me manage cleanup
- Oh! The constructor/provider can return Dispose as part of the API, A bit messy, but works
- a testfixture/testApi reader monad expression
  - leave the api application implicit / part of the computation expression

There doesn't appear to be a way to implement IDisposable on a record or tuple