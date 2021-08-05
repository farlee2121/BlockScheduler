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
let buildRuleCrudTests testEnv =

    let test' = testWithEnv testEnv
    let testProperty' = testEnvProperty testEnv
    // let testProperty' = testProperty with Con setup cleanup FsCheckConfig.defaultConfig

    testList "Block Rule CRUD" [
        test' "List empty when no rules created" <| fun testApi ->
            Expect.isEmpty' (testApi.ListRules ())

        testProperty' "Created shows in list"  (fun (testApi) (rules: (Blockable * Schedule) list) -> 
            let mapCreated (blockable, schedule) = ((blockable, schedule), testApi.CreateRule blockable schedule)
            let created = rules |> List.map mapCreated
            let listed = testApi.ListRules ()
            
            false
        )
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