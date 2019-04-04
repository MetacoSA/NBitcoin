module SatisfyTests

open Expecto
open NBitcoin.Miniscript

[<Tests>]
let tests =
    testList "SatisfyTests" [
        testCase "case 1" <| fun _ ->
            let key = NBitcoin.Key()
            let scriptStr = sprintf "and(pk(%s), time(%d))" (key.PubKey.ToString()) 10000u
            let ms = MiniScript.parseUnsafe scriptStr
            ()
    ]
