module SatisfyTests

open Expecto
open NBitcoin
open NBitcoin.Miniscript
open NBitcoin.Miniscript

[<Tests>]
let tests =
    testList "SatisfyTests" [
        testCase "case 1" <| fun _ ->
            let key = NBitcoin.Key()
            let scriptStr = sprintf "and(pk(%s), time(%d))" (key.PubKey.ToString()) 10000u
            let ms = Miniscript.fromStringUnsafe scriptStr
            let t = ms.ToAST().CastTUnsafe()

            let dummyKeyFn pk = None
            let r1 = Satisfy.satisfyT (dummyKeyFn |> Some, None, None) t
            Expect.isError r1 "should not satisfy with dummy function"

            let dummySig = TransactionSignature.Empty

            let keyFn (pk: PubKey) = if pk.Equals(key.PubKey) then Some(dummySig) else None
            let r2 = Satisfy.satisfyT (Some keyFn, None, None) t
            Expect.isError r2 "should not satisfy the time"

            let dummyAge = LockTime 10001
            let r3 = Satisfy.satisfyT (Some keyFn, None, Some dummyAge) t

            Expect.isOk r3 "could not satisfy"

        ftestCase "case 1 using facade" <| fun _ ->
            let key = NBitcoin.Key()
            let scriptStr = sprintf "and(pk(%s), time(%d))" (key.PubKey.ToString()) 10000u
            let ms = Miniscript.fromStringUnsafe scriptStr
            let dummyKeyFn pk = None
            let r1 = ms.Satisfy(?keyFn=Some(dummyKeyFn))
            let dummySig = TransactionSignature.Empty

            let keyFn (pk: PubKey) = if pk.Equals(key.PubKey) then Some(dummySig) else None
            let r2 = ms.Satisfy(?keyFn=Some keyFn)
            Expect.isError r2 "should not satisfy the time"

            let dummyAge = LockTime 10001u
            let r3 = ms.Satisfy(?keyFn=Some keyFn, ?age=Some dummyAge)

            Expect.isOk r3 "could not satisfy"
    ]
