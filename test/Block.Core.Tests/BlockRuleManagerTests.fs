module BlockRuleManagerTests
open Expecto
open FsCheck
open ExpectoExtensions
open BlockRuleManager


type RuleCrudTestApi = {
    ListRules: ListRules
    CreateRule: CreateRule
    UpdateRule: UpdateRule
    DeleteRule: DeleteRule
}
let buildRuleCrudTests setup cleanup =

    let test' = testWithEnv setup cleanup
    let withEnv' = withEnv setup cleanup
    // let testProperty' = testProperty with Con setup cleanup FsCheckConfig.defaultConfig

    testList "Block Rule CRUD" [
        test' "List empty when no rules created" <| fun testApi ->
            Expect.isEmpty' (testApi.ListRules ())
        testProperty "Created shows in list" (fun (rules: (Blockable * Schedule) list) -> withEnv' (fun testApi ->
            let mapCreated (blockable, schedule) = ((blockable, schedule), testApi.CreateRule blockable schedule)
            let created = rules |> List.map mapCreated
            let listed = testApi.ListRules ()
            
            false
        ))
        // delete a rule that doesn't exist
        // update a rule that doesn't exist
        // created rule is listed
        // incomplete rules? that shouldn't even be possible. Should be covered by the constructor
        // update any listed rule
        // delete any listed rule
    ]


[<Tests>]
let ``BlockRuleManager Spec - InMemory`` =
    testList "BlockRuleManager Spec" [
    
    ]