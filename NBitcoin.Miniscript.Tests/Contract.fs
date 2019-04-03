module ContractTests

open Expecto
open FNBitcoin.Contract
open FsCheck
open NBitcoin
(*
[<Tests>]
let contractTests =
    testList "Contracts" [
        testProperty "Multisig" <| fun (m: PositiveInt) (n: PositiveInt) ->
            if (m < n) && n.Get < 16 then
                let pubkeys = seq { for i in 0..n.Get do
                                        yield Key().PubKey }
                              |> Seq.toArray
                let verifiableScript = Multisig(m.Get, pubkeys)
                let res = verify {
                    let! v = verifiableScript
                    return v
                }
                match res with
                | Verified s -> Expect.isTrue true "ok"
                | Failed s -> Expect.isTrue false "failed"
            else
                Expect.isTrue true "skip"
    ]

*)
