open Expecto
module Program =
    [<EntryPoint>]
    let main argv =
        // The test runner doesn't seem to like when I use the WithCLIArgs verions
        // Can run `dotnet test -- Expecto.join-with="/"
        // note that i've only has . and / work as separators. You can see this is hard-baked into the code https://github.com/haf/expecto/blob/2733bea93f4015214dcbcb394ff8cf5f42782206/Expecto/Expecto.fs#L452
        // Tests.runTestsInAssemblyWithCLIArgs [(JoinWith "/")] argv

        Tests.runTestsInAssembly defaultConfig argv
        