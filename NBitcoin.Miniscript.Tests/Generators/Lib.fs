namespace NBitcoin.Miniscript.Tests.Generators

open FsCheck
open NBitcoin.Miniscript.MiniscriptParser
open NBitcoin.Miniscript.Tests.Generators.Policy

type Generators =
    static member Policy() : Arbitrary<Policy> =  // policy |> Arb.fromGen
        { new Arbitrary<Policy>() with
            override this.Generator = policy
            // TODO: This shrinker is far from ideal
            // 1. nested shrinking does not work well
            // 2. Must use Seq instead of List
            override this.Shrinker(p: Policy) =
                let rec shrinkPolicy p =
                    match p with
                    | Key k -> []
                    | Multi(m, pks) -> [Multi(1u, pks.[0..0]); Policy.Key pks.[0]]
                    | Policy.Hash h -> []
                    | Policy.Time t -> []
                    | Policy.Threshold (k, ps) ->
                        let shrinkThres (k, (ps: Policy[])) =
                            let k2 = if k = 1u then k else k - 1u
                            let ps2 = Arb.shrink(ps)
                            ps2 |> Seq.toList |> List.map(fun p -> Policy.Threshold(k2, p))
                        let subexpr = ps |> Array.toList
                        if ps.Length = 1 then subexpr else shrinkThres(k, ps)
                    | Policy.And(p1, p2) ->
                        let shrinkedAnd = shrinkNested Policy.And p1 p2
                        List.concat[shrinkedAnd; [p1; p2;]]
                    | Policy.Or(p1, p2) ->
                        let shrinkedOr = shrinkNested Policy.Or p1 p2
                        List.concat[shrinkedOr; [p1; p2;]]
                    | Policy.AsymmetricOr(p1, p2) ->
                        let shrinkedAOr = shrinkNested Policy.AsymmetricOr p1 p2
                        List.concat[shrinkedAOr; [p1; p2;]]

                /// Helper for shrinking nested types
                and shrinkNested expectedType p1 p2 =
                    let shrinkedSub1 = shrinkPolicy p1
                    let shrinkedSub2 = shrinkPolicy p2
                    shrinkedSub1
                    |> List.collect(fun p1e -> shrinkedSub2
                                               |> List.map(fun p2e -> p1e, p2e))
                    |> List.map expectedType

                shrinkPolicy p |> List.toSeq
        }
