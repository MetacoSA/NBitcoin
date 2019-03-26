namespace NBitcoin.Miniscript

open NBitcoin
open NBitcoin.Miniscript.Utils
open NBitcoin.Miniscript.MiniscriptParser
open Miniscript.AST

module Compiler =
    type CompiledNode =
        | Pk of NBitcoin.PubKey
        | Multi of uint32 * PubKey []
        | Hash of uint256
        | Time of LockTime
        | Threshold of uint32 * CompiledNode []
        | And of left : CompiledNode * right : CompiledNode
        | Or of left : CompiledNode * right : CompiledNode * leftProb : float * rightProb : float

    type Cost =
        { ast : AST
          pkCost : uint32
          satCost : float
          dissatCost : float }

    /// Intermediary value before computing parent Cost
    type CostTriple =
        { parent : AST
          left : Cost
          right : Cost
          /// In case of F ast, we can tell the compiler that
          /// it can be combined as an E expression in two ways.
          /// This is equivalent to `->` in this macro
          /// ref: https://github.com/apoelstra/rust-miniscript/blob/ac36d4bacd6440458a57b4bd2013ea1c27058709/src/policy/compiler.rs#L333
          condCombine : bool }

    module Cost =
        /// Casts F -> E
        let likely (fcost : Cost) : Cost =
            { ast = ETree(E.Likely(fcost.ast.castFUnsafe()))
              pkCost = fcost.pkCost + 4u
              satCost = fcost.satCost * 1.0
              dissatCost = 2.0 }
        
        let unlikely (fcost : Cost) : Cost =
            { ast = ETree(E.Unlikely(fcost.ast.castFUnsafe()))
              pkCost = fcost.pkCost + 4u
              satCost = fcost.satCost * 2.0
              dissatCost = 1.0 }
        
        let fromPairToTCost (left : Cost) (right : Cost) (newAst : T) 
            (lweight : float) (rweight : float) =
            match newAst with
            | T.Time _ | T.HashEqual _ | T.CastE _ -> failwith "unreachable"
            | T.And _ -> 
                { ast = TTree newAst
                  pkCost = left.pkCost + right.pkCost
                  satCost = left.satCost + right.satCost
                  dissatCost = 0.0 }
            | T.ParallelOr _ -> 
                { ast = TTree newAst
                  pkCost = left.pkCost + right.pkCost + 1u
                  satCost =
                      (left.satCost + right.dissatCost) * lweight 
                      + (right.satCost + left.dissatCost) * rweight
                  dissatCost = 0.0 }
            | T.CascadeOr _ | T.CascadeOrV _ -> 
                { ast = TTree newAst
                  pkCost = left.pkCost + right.pkCost + 3u
                  satCost =
                      left.satCost * lweight 
                      + (right.satCost + left.dissatCost) * rweight
                  dissatCost = 0.0 }
            | T.SwitchOr _ -> 
                { ast = TTree newAst
                  pkCost = left.pkCost + right.pkCost + 3u
                  satCost =
                      (left.satCost + 2.0) * lweight 
                      + (right.satCost + 1.0) * rweight
                  dissatCost = 0.0 }
            | T.SwitchOrV _ -> 
                { ast = TTree newAst
                  pkCost = left.pkCost + right.pkCost + 4u
                  satCost =
                      (left.satCost + 2.0) * lweight 
                      + (right.satCost + 1.0) * rweight
                  dissatCost = 0.0 }
            | T.DelayedOr _ -> 
                { ast = TTree newAst
                  pkCost = left.pkCost + right.pkCost + 4u
                  satCost =
                      72.0 + (left.satCost + 2.0) * lweight 
                      + (right.satCost + 1.0) * rweight
                  dissatCost = 0.0 }
        
        let fromPairToVCost (left : Cost) (right : Cost) (newAst : V) 
            (lweight : float) (rweight : float) =
            match newAst with
            | V.CheckSig _ | V.CheckMultiSig _ | V.Time _ | V.HashEqual _ | V.Threshold _ -> 
                failwith "unreachable"
            | V.And _ -> 
                { ast = VTree newAst
                  pkCost = left.pkCost + right.pkCost
                  satCost = left.satCost + right.satCost
                  dissatCost = 0.0 }
            | V.CascadeOr _ -> 
                { ast = VTree newAst
                  pkCost = left.pkCost + right.pkCost + 2u
                  satCost =
                      (left.satCost * lweight) 
                      + (right.satCost + left.dissatCost) * rweight
                  dissatCost = 0.0 }
            | V.SwitchOr _ -> 
                { ast = VTree newAst
                  pkCost = left.pkCost + right.pkCost + 3u
                  satCost =
                      (left.satCost + 2.0) * lweight 
                      + (right.satCost + 1.0) * rweight
                  dissatCost = 0.0 }
            | V.SwitchOrT _ -> 
                { ast = VTree newAst
                  pkCost = left.pkCost + right.pkCost + 4u
                  satCost =
                      (left.satCost + 2.0) * lweight 
                      + (right.satCost + 1.0) * rweight
                  dissatCost = 0.0 }
            | V.DelayedOr _ -> 
                { ast = VTree newAst
                  pkCost = left.pkCost + right.pkCost + 4u
                  satCost =
                      (72.0 + left.satCost + 2.0) * lweight 
                      + (72.0 + right.satCost + 1.0) * rweight
                  dissatCost = 0.0 }
        
        let fromPairToFCost (left : Cost) (right : Cost) (newAst : F) 
            (lweight : float) (rweight : float) =
            match newAst with
            | F.CheckSig _ | F.CheckMultiSig _ | F.Time _ | F.HashEqual _ | F.Threshold _ -> 
                failwith "unreachable"
            | F.And _ -> 
                { ast = FTree newAst
                  pkCost = left.pkCost + right.pkCost
                  satCost = left.satCost + right.satCost
                  dissatCost = 0.0 }
            | F.CascadeOr _ -> 
                { ast = FTree newAst
                  pkCost = left.pkCost + right.pkCost + 3u
                  satCost =
                      left.satCost * lweight 
                      + (right.satCost + left.dissatCost) * rweight
                  dissatCost = 0.0 }
            | F.SwitchOr _ -> 
                { ast = FTree newAst
                  pkCost = left.pkCost + right.pkCost + 3u
                  satCost =
                      (left.satCost + 2.0) * lweight 
                      + (right.satCost + 1.0) * rweight
                  dissatCost = 0.0 }
            | F.SwitchOrV _ -> 
                { ast = FTree newAst
                  pkCost = left.pkCost + right.pkCost + 4u
                  satCost =
                      (left.satCost + 2.0) * lweight 
                      + (right.satCost + 1.0) * rweight
                  dissatCost = 0.0 }
            | F.DelayedOr _ -> 
                { ast = FTree newAst
                  pkCost = left.pkCost + right.pkCost + 5u
                  satCost =
                      72.0 + (left.satCost + 2.0) * lweight 
                      + (right.satCost + 1.0) * rweight
                  dissatCost = 0.0 }
        
        let fromPairToECost (left : Cost) (right : Cost) (newAst : E) 
            (lweight : float) (rweight : float) =
            let pkCost = left.pkCost + right.pkCost
            match newAst with
            | E.CheckSig _ | E.CheckMultiSig _ | E.Time _ | E.Threshold _ | E.Likely _ | E.Unlikely _ | E.ParallelAnd _ -> 
                { ast = ETree newAst
                  pkCost = pkCost + 1u
                  satCost = left.satCost + right.satCost
                  dissatCost = left.dissatCost + right.dissatCost }
            | E.CascadeAnd _ -> 
                { ast = ETree newAst
                  pkCost = pkCost + 4u
                  satCost = left.satCost + right.satCost
                  dissatCost = left.dissatCost }
            | E.ParallelOr _ -> 
                { ast = ETree newAst
                  pkCost = pkCost + 1u
                  satCost =
                      left.satCost * lweight 
                      + (right.satCost + left.dissatCost) * rweight
                  dissatCost = left.dissatCost + right.dissatCost }
            | E.CascadeOr _ -> 
                { ast = ETree newAst
                  pkCost = left.pkCost + right.pkCost + 3u
                  satCost =
                      left.satCost * lweight 
                      + (right.satCost + left.dissatCost) * rweight
                  dissatCost = left.dissatCost + right.dissatCost }
            | E.SwitchOrLeft _ -> 
                { ast = ETree newAst
                  pkCost = pkCost + 3u
                  satCost =
                      (left.satCost + 2.0) * lweight 
                      + (right.satCost + 1.0) * rweight
                  dissatCost = left.dissatCost + 2.0 }
            | E.SwitchOrRight _ -> 
                { ast = ETree newAst
                  pkCost = pkCost + 3u
                  satCost =
                      (left.satCost + 1.0) * lweight 
                      + (right.satCost + 2.0) * rweight
                  dissatCost = left.dissatCost + 1.0 }
        
        // TODO: Consider about treating swap case (eft <=> right) here.
        let fromTriple (triple : CostTriple) (lweight : float) (rweight : float) : Cost [] =
            match triple.parent with
            | TTree t -> 
                fromPairToTCost triple.left triple.right t lweight rweight 
                |> Array.singleton
            | ETree e -> 
                fromPairToECost triple.left triple.right e lweight rweight 
                |> Array.singleton
            | FTree f -> 
                match triple.condCombine with
                | (false) -> 
                    fromPairToFCost triple.left triple.right f lweight rweight 
                    |> Array.singleton
                | (true) -> 
                    let costBeforeCast =
                        fromPairToFCost triple.left triple.right f lweight rweight
                    [| likely (costBeforeCast)
                       unlikely (costBeforeCast) |]
            | VTree v -> 
                fromPairToVCost triple.left triple.right v lweight rweight 
                |> Array.singleton
        
        let min_cost (a : Cost, b : Cost, p_sat : float, p_dissat : float) =
            let weight_one =
                (float a.pkCost) + p_sat * a.satCost + p_dissat * a.dissatCost
            let weight_two =
                (float b.pkCost) + p_sat * b.satCost + p_dissat * a.dissatCost
            if weight_one < weight_two then a
            else if weight_one > weight_two then b
            else if a.satCost < b.satCost then a
            else b
        
        let fold_costs (p_sat : float) (p_dissat : float) (cs : Cost []) =
            cs
            |> Array.toList
            |> List.reduce (fun a b -> min_cost (a, b, p_sat, p_dissat))
        
        // equivalent to rules! macro in rust-miniscript
        let getMinimumCost (triples : CostTriple []) (p_sat) (p_dissat) 
            (lweight : float) (rweight : float) : Cost =
            triples
            |> Array.map (fun p -> fromTriple p 0.0 0.0)
            |> Array.concat
            |> fold_costs p_sat p_dissat

    module CompiledNode =
        /// bytes length when a number is encoded as bitcoin CScriptNum
        let private scriptNumCost n =
            if n <= 16u then 1u
            else if n < 0x100u then 2u
            else if n < 0x10000u then 3u
            else if n < 0x1000000u then 4u
            else 5u
        
        let private minCost (one : Cost, two : Cost, p_sat : float, p_dissat) =
            let weight_one =
                (float one.pkCost) + p_sat * one.satCost + p_dissat * one.dissatCost
            let weight_two =
                (float two.pkCost) + p_sat * two.satCost + p_dissat * two.dissatCost
            if weight_one < weight_two then one
            else if weight_two < weight_one then one
            else if one.satCost < two.satCost then one
            else two
        
        let private getPkCost m (pks : PubKey []) =
            match (m > 16u, pks.Length > 16) with
            | (true, true) -> 4
            | (true, false) -> 3
            | (false, true) -> 3
            | (false, false) -> 2
        
        let rec fromPolicy (p : Policy) : CompiledNode =
            match p with
            | Key k -> Pk k
            | Policy.Multi(m, pks) -> Multi(m, pks)
            | Policy.Hash h -> Hash h
            | Policy.Time t -> Time t
            | Policy.Threshold(n, subexprs) -> 
                let ps = subexprs |> Array.map fromPolicy
                Threshold(n, ps)
            | Policy.And(e1, e2) -> And(fromPolicy e1, fromPolicy e2)
            | Policy.Or(e1, e2) -> Or(fromPolicy e1, fromPolicy e2, 0.5, 0.5)
            | Policy.AsymmetricOr(e1, e2) -> 
                Or(fromPolicy e1, fromPolicy e2, 127.0 / 128.0, 1.0 / 128.0)
        
        // TODO: cache
        let rec best_t (node : CompiledNode, p_sat : float, p_dissat : float) : Cost =
            match node with
            | Pk _ | Multi _ | Threshold _ -> 
                let e = best_e (node, p_sat, p_dissat)
                { ast = TTree(T.CastE(e.ast.castEUnsafe()))
                  pkCost = e.pkCost
                  satCost = e.satCost
                  dissatCost = 0.0 }
            | Time t -> 
                let num_cost = scriptNumCost (!> t)
                { ast = TTree(T.Time t)
                  pkCost = 1u + uint32 num_cost
                  satCost = 0.0
                  dissatCost = 0.0 }
            | Hash h -> 
                { ast = TTree(T.HashEqual h)
                  pkCost = 39u
                  satCost = 33.0
                  dissatCost = 0.0 }
            | And(l, r) -> 
                let vl = best_v (l, p_sat, 0.0)
                let vr = best_v (r, p_sat, 0.0)
                let tl = best_t (l, p_sat, 0.0)
                let tr = best_t (r, p_sat, 0.0)
                
                let possibleCases =
                    [| { parent =
                             TTree
                                 (T.And(vl.ast.castVUnsafe(), tr.ast.castTUnsafe()))
                         left = vl
                         right = tr
                         condCombine = false }
                       { parent =
                             TTree
                                 (T.And(vr.ast.castVUnsafe(), tl.ast.castTUnsafe()))
                         left = vl
                         right = tr
                         condCombine = false } |]
                Cost.getMinimumCost possibleCases p_sat p_dissat 0.0 0.0
            | Or(l, r, lweight, rweight) -> 
                let le = best_e (l, (p_sat * lweight), (p_sat * rweight))
                let re = best_e (r, (p_sat * rweight), (p_sat * lweight))
                let lw = best_w (l, (p_sat * lweight), (p_sat * rweight))
                let rw = best_w (r, (p_sat * rweight), (p_sat * lweight))
                let lt = best_t (l, (p_sat * lweight), 0.0)
                let rt = best_t (r, (p_sat * lweight), 0.0)
                let lv = best_v (l, (p_sat * lweight), 0.0)
                let rv = best_v (r, (p_sat * lweight), 0.0)
                let maybelq = best_q (l, (p_sat * lweight), 0.0)
                let mayberq = best_q (r, (p_sat * lweight), 0.0)
                
                let possibleCases =
                    [| { parent =
                             TTree
                                 (T.ParallelOr
                                      (le.ast.castEUnsafe(), rw.ast.castWUnsafe()))
                         left = le
                         right = rw
                         condCombine = false }
                       { parent =
                             TTree
                                 (T.ParallelOr
                                      (re.ast.castEUnsafe(), lw.ast.castWUnsafe()))
                         left = re
                         right = lw
                         condCombine = false }
                       { parent =
                             TTree
                                 (T.CascadeOr
                                      (le.ast.castEUnsafe(), rt.ast.castTUnsafe()))
                         left = le
                         right = rt
                         condCombine = false }
                       { parent =
                             TTree
                                 (T.CascadeOr
                                      (re.ast.castEUnsafe(), lt.ast.castTUnsafe()))
                         left = re
                         right = lt
                         condCombine = false }
                       { parent =
                             TTree
                                 (T.CascadeOrV
                                      (le.ast.castEUnsafe(), rv.ast.castVUnsafe()))
                         left = le
                         right = rv
                         condCombine = false }
                       { parent =
                             TTree
                                 (T.CascadeOrV
                                      (re.ast.castEUnsafe(), lv.ast.castVUnsafe()))
                         left = re
                         right = lv
                         condCombine = false }
                       { parent =
                             TTree
                                 (T.SwitchOr
                                      (lt.ast.castTUnsafe(), rt.ast.castTUnsafe()))
                         left = lt
                         right = rt
                         condCombine = false }
                       { parent =
                             TTree
                                 (T.SwitchOr
                                      (rt.ast.castTUnsafe(), lt.ast.castTUnsafe()))
                         left = rt
                         right = lt
                         condCombine = false }
                       { parent =
                             TTree
                                 (T.SwitchOrV
                                      (lv.ast.castVUnsafe(), rv.ast.castVUnsafe()))
                         left = lv
                         right = rv
                         condCombine = false }
                       { parent =
                             TTree
                                 (T.SwitchOrV
                                      (rv.ast.castVUnsafe(), lv.ast.castVUnsafe()))
                         left = rv
                         right = lv
                         condCombine = false } |]
                
                let casesWithQ =
                    match maybelq, mayberq with
                    | Some lq, Some rq -> 
                        Array.append possibleCases [| { parent =
                                                            TTree
                                                                (T.DelayedOr
                                                                     (lq.ast.castQUnsafe
                                                                          (), 
                                                                      rq.ast.castQUnsafe
                                                                          ()))
                                                        left = lq
                                                        right = rq
                                                        condCombine = false } |]
                    | _ -> possibleCases
                
                Cost.getMinimumCost casesWithQ p_sat 0.0 lweight rweight
        
        and best_e (node : CompiledNode, p_sat : float, p_dissat : float) : Cost =
            match node with
            | Pk k -> 
                { ast = ETree(E.CheckSig k)
                  pkCost = 35u
                  satCost = 72.0
                  dissatCost = 1.0 }
            | Multi(m, pks) -> 
                let num_cost = getPkCost m pks
                
                let options =
                    [ { ast = ETree(E.CheckMultiSig(m, pks))
                        pkCost = uint32 (num_cost + 34 * pks.Length + 1)
                        satCost = 2.0
                        dissatCost = 1.0 } ]
                if not (p_dissat > 0.0) then options.[0]
                else 
                    let bestf = best_f (node, p_sat, 0.0)
                    
                    let options2 =
                        [ Cost.likely (bestf)
                          Cost.unlikely (bestf) ]
                    List.concat [ options; options2 ]
                    |> List.toArray
                    |> Cost.fold_costs p_sat p_dissat
            | Time n -> 
                let num_cost = scriptNumCost (!> n)
                { ast = ETree(E.Time n)
                  pkCost = 5u + num_cost
                  satCost = 2.0
                  dissatCost = 1.0 }
            | Hash h -> 
                let fcost = best_f (node, p_sat, p_dissat)
                minCost (Cost.likely fcost, Cost.unlikely fcost, p_sat, p_dissat)
            | And(l, r) -> 
                let le = best_e (l, p_sat, p_dissat)
                let re = best_e (r, p_sat, p_dissat)
                let lw = best_w (l, p_sat, p_dissat)
                let rw = best_w (r, p_sat, p_dissat)
                let lf = best_f (l, p_sat, 0.0)
                let rf = best_f (r, p_sat, 0.0)
                let lv = best_v (l, p_sat, 0.0)
                let rv = best_v (r, p_sat, 0.0)
                
                let possibleCases =
                    [| { parent =
                             ETree
                                 (E.ParallelAnd
                                      (le.ast.castEUnsafe(), rw.ast.castWUnsafe()))
                         left = le
                         right = rw
                         condCombine = false }
                       { parent =
                             ETree
                                 (E.ParallelAnd
                                      (re.ast.castEUnsafe(), lw.ast.castWUnsafe()))
                         left = re
                         right = lw
                         condCombine = false }
                       { parent =
                             ETree
                                 (E.CascadeAnd
                                      (le.ast.castEUnsafe(), rf.ast.castFUnsafe()))
                         left = le
                         right = rf
                         condCombine = false }
                       { parent =
                             ETree
                                 (E.CascadeAnd
                                      (re.ast.castEUnsafe(), lf.ast.castFUnsafe()))
                         left = re
                         right = lf
                         condCombine = false }
                       { parent =
                             FTree
                                 (F.And(lv.ast.castVUnsafe(), rf.ast.castFUnsafe()))
                         left = lv
                         right = rf
                         condCombine = true }
                       { parent =
                             FTree
                                 (F.And(rv.ast.castVUnsafe(), lf.ast.castFUnsafe()))
                         left = rv
                         right = lf
                         condCombine = true } |]
                Cost.getMinimumCost possibleCases p_sat p_dissat 0.5 0.5
            | Or(l, r, lweight, rweight) -> 
                let le_par =
                    best_e (l, (p_sat * lweight), (p_dissat + p_sat * rweight))
                let re_par =
                    best_e (r, (p_sat * lweight), (p_dissat + p_sat * rweight))
                let lw_par =
                    best_w (l, (p_sat * lweight), (p_dissat + p_sat * rweight))
                let rw_par =
                    best_w (r, (p_sat * lweight), (p_dissat + p_sat * rweight))
                let le_cas = best_e (l, (p_sat * lweight), (p_dissat))
                let re_cas = best_e (r, (p_sat * lweight), (p_dissat))
                let le_cond_par = best_e (l, (p_sat * lweight), (p_sat * rweight))
                let re_cond_par = best_e (r, (p_sat * lweight), (p_sat * lweight))
                let lv = best_v (l, (p_sat * lweight), 0.0)
                let rv = best_v (r, (p_sat * rweight), 0.0)
                let lf = best_f (l, (p_sat * lweight), 0.0)
                let rf = best_f (r, (p_sat * rweight), 0.0)
                let maybelq = best_q (l, (p_sat * lweight), 0.0)
                let mayberq = best_q (r, (p_sat * rweight), 0.0)
                
                let possibleCases =
                    [| { parent =
                             ETree
                                 (E.ParallelOr
                                      (le_par.ast.castEUnsafe(), 
                                       rw_par.ast.castWUnsafe()))
                         left = le_par
                         right = rw_par
                         condCombine = false }
                       { parent =
                             ETree
                                 (E.ParallelOr
                                      (re_par.ast.castEUnsafe(), 
                                       lw_par.ast.castWUnsafe()))
                         left = re_par
                         right = lw_par
                         condCombine = false }
                       { parent =
                             ETree
                                 (E.CascadeOr
                                      (le_par.ast.castEUnsafe(), 
                                       re_cas.ast.castEUnsafe()))
                         left = le_par
                         right = re_cas
                         condCombine = false }
                       { parent =
                             ETree
                                 (E.CascadeOr
                                      (re_par.ast.castEUnsafe(), 
                                       le_cas.ast.castEUnsafe()))
                         left = re_par
                         right = le_cas
                         condCombine = false }
                       { parent =
                             ETree
                                 (E.SwitchOrLeft
                                      (le_cas.ast.castEUnsafe(), 
                                       rf.ast.castFUnsafe()))
                         left = le_cas
                         right = rf
                         condCombine = false }
                       { parent =
                             ETree
                                 (E.SwitchOrLeft
                                      (re_cas.ast.castEUnsafe(), 
                                       lf.ast.castFUnsafe()))
                         left = re_cas
                         right = lf
                         condCombine = false }
                       { parent =
                             ETree
                                 (E.SwitchOrRight
                                      (le_cas.ast.castEUnsafe(), 
                                       rf.ast.castFUnsafe()))
                         left = le_cas
                         right = rf
                         condCombine = false }
                       { parent =
                             ETree
                                 (E.SwitchOrRight
                                      (re_cas.ast.castEUnsafe(), 
                                       lf.ast.castFUnsafe()))
                         left = re_cas
                         right = lf
                         condCombine = false }
                       { parent =
                             FTree
                                 (F.CascadeOr
                                      (le_cas.ast.castEUnsafe(), 
                                       rv.ast.castVUnsafe()))
                         left = le_cas
                         right = rv
                         condCombine = true }
                       { parent =
                             FTree
                                 (F.CascadeOr
                                      (re_cas.ast.castEUnsafe(), 
                                       lv.ast.castVUnsafe()))
                         left = re_cas
                         right = lv
                         condCombine = true }
                       { parent =
                             FTree
                                 (F.SwitchOr
                                      (lf.ast.castFUnsafe(), rf.ast.castFUnsafe()))
                         left = lf
                         right = rf
                         condCombine = true }
                       { parent =
                             FTree
                                 (F.SwitchOr
                                      (rf.ast.castFUnsafe(), lf.ast.castFUnsafe()))
                         left = rf
                         right = lf
                         condCombine = true }
                       { parent =
                             FTree
                                 (F.SwitchOrV
                                      (lv.ast.castVUnsafe(), rv.ast.castVUnsafe()))
                         left = lv
                         right = rv
                         condCombine = true }
                       { parent =
                             FTree
                                 (F.SwitchOrV
                                      (rv.ast.castVUnsafe(), lv.ast.castVUnsafe()))
                         left = rv
                         right = lv
                         condCombine = true } |]
                
                let casesWithQ =
                    match maybelq, mayberq with
                    | Some lq, Some rq -> 
                        Array.append possibleCases [| { parent =
                                                            FTree
                                                                (F.DelayedOr
                                                                     (lq.ast.castQUnsafe
                                                                          (), 
                                                                      rq.ast.castQUnsafe
                                                                          ()))
                                                        left = lq
                                                        right = rq
                                                        condCombine = true }
                                                      { parent =
                                                            FTree
                                                                (F.DelayedOr
                                                                     (rq.ast.castQUnsafe
                                                                          (), 
                                                                      lq.ast.castQUnsafe
                                                                          ()))
                                                        left = rq
                                                        right = lq
                                                        condCombine = true } |]
                    | _ -> possibleCases
                
                Cost.getMinimumCost casesWithQ p_sat p_dissat lweight rweight
            | Threshold(n, subs) -> 
                let num_cost = scriptNumCost n
                let avgCost = float n / float subs.Length
                let e =
                    best_e 
                        (subs.[0], (p_sat * avgCost), 
                         (p_dissat + p_sat * (1.0 - avgCost)))
                let ws =
                    subs 
                    |> Array.map 
                           (fun s -> 
                           best_w 
                               (s, (p_sat * avgCost), 
                                (p_dissat + p_sat * (1.0 - avgCost))))
                let pk_cost =
                    ws 
                    |> Array.fold (fun acc w -> acc + w.pkCost) 
                           (1u + num_cost + e.pkCost)
                let sat_cost =
                    ws |> Array.fold (fun acc w -> acc + w.satCost) e.satCost
                let dissat_cost =
                    ws |> Array.fold (fun acc w -> acc + w.dissatCost) e.dissatCost
                let wsast = ws |> Array.map (fun w -> w.ast.castWUnsafe())
                
                let cond =
                    { ast = ETree(E.Threshold(n, e.ast.castEUnsafe(), wsast))
                      pkCost = pk_cost
                      satCost = sat_cost
                      dissatCost = dissat_cost }
                
                let f = best_f (node, p_sat, 0.0)
                let cond1 = Cost.likely (f)
                let cond2 = Cost.unlikely (f)
                let nonCond = Cost.min_cost (cond1, cond2, p_sat, p_dissat)
                Cost.min_cost (cond, nonCond, p_sat, p_dissat)
        
        and best_q (node : CompiledNode, p_sat : float, p_dissat : float) : Cost option =
            match node with
            | Pk pk -> 
                { ast = QTree(Q.Pubkey(pk))
                  pkCost = 34u
                  satCost = 0.0
                  dissatCost = 0.0 }
                |> Some
            | And(l, r) -> 
                let maybelq = best_q (l, p_sat, p_dissat)
                let mayberq = best_q (r, p_sat, p_dissat)
                
                let cost v q =
                    { ast = QTree(Q.And(v.ast.castVUnsafe(), q.ast.castQUnsafe()))
                      pkCost = v.pkCost + q.pkCost
                      satCost = v.satCost + q.satCost
                      dissatCost = 0.0 }
                
                let op =
                    match maybelq, mayberq with
                    | None, Some rq -> 
                        let lv = best_v (l, p_sat, p_dissat)
                        [| cost lv rq |]
                    | Some lq, None -> 
                        let rv = best_v (r, p_sat, p_dissat)
                        [| cost rv lq |]
                    | Some lq, Some rq -> 
                        let lv = best_v (l, p_sat, p_dissat)
                        let rv = best_v (r, p_sat, p_dissat)
                        [| cost lv rq
                           cost rv lq |]
                    | None, None -> [||]
                
                if op.Length = 0 then None
                else 
                    op
                    |> Cost.fold_costs p_sat p_dissat
                    |> Some
            | Or(l, r, lweight, rweight) -> 
                let maybelq = best_q (l, (p_sat * lweight), 0.0)
                let mayberq = best_q (r, (p_sat + rweight), 0.0)
                match maybelq, mayberq with
                | Some lq, Some rq -> 
                    [| { ast =
                             QTree(Q.Or(lq.ast.castQUnsafe(), rq.ast.castQUnsafe()))
                         pkCost = lq.pkCost + rq.pkCost + 3u
                         satCost =
                             lweight * (2.0 + lq.satCost) 
                             + rweight * (1.0 + rq.satCost)
                         dissatCost = 0.0 }
                       { ast =
                             QTree(Q.Or(rq.ast.castQUnsafe(), lq.ast.castQUnsafe()))
                         pkCost = rq.pkCost + lq.pkCost + 3u
                         satCost =
                             lweight * (1.0 + lq.satCost) 
                             + rweight * (2.0 + rq.satCost)
                         dissatCost = 0.0 } |]
                    |> Cost.fold_costs p_sat p_dissat
                    |> Some
                | _ -> None
            | _ -> None
        
        and best_w (node : CompiledNode, p_sat : float, p_dissat : float) : Cost =
            match node with
            | Pk k -> 
                { ast = WTree(W.CheckSig(k))
                  pkCost = 36u
                  satCost = 72.0
                  dissatCost = 1.0 }
            | Time t -> 
                let num_cost = scriptNumCost (!> t)
                { ast = WTree(W.Time(t))
                  pkCost = 6u + num_cost
                  satCost = 2.0
                  dissatCost = 1.0 }
            | Hash h -> 
                { ast = WTree(W.HashEqual(h))
                  pkCost = 45u
                  satCost = 33.0
                  dissatCost = 1.0 }
            | _ -> 
                let c = best_e (node, p_sat, p_dissat)
                { c with ast = WTree(W.CastE(c.ast.castEUnsafe()))
                         pkCost = c.pkCost + 2u }
        
        and best_f (node : CompiledNode, p_sat : float, p_dissat : float) : Cost =
            match node with
            | Pk k -> 
                { ast = FTree(F.CheckSig(k))
                  pkCost = 36u
                  satCost = 72.0
                  dissatCost = 1.0 }
            | Multi(m, pks) -> 
                let num_cost = getPkCost m pks
                { ast = FTree(F.CheckMultiSig(m, pks))
                  pkCost = uint32 (num_cost + 34 * pks.Length) + 2u
                  satCost = 1.0 + 72.0 * float m
                  dissatCost = 0.0 }
            | Time t -> 
                let num_cost = scriptNumCost (!> t)
                { ast = FTree(F.Time(t))
                  pkCost = 2u + num_cost
                  satCost = 0.0
                  dissatCost = 0.0 }
            | Hash h -> 
                { ast = FTree(F.HashEqual(h))
                  pkCost = 40u
                  satCost = 33.0
                  dissatCost = 0.0 }
            | And(l, r) -> 
                let vl = best_v (l, p_sat, 0.0)
                let vr = best_v (r, p_sat, 0.0)
                let fl = best_f (l, p_sat, 0.0)
                let fr = best_f (r, p_sat, 0.0)
                
                let possibleCases =
                    [| { parent =
                             FTree
                                 (F.And(vl.ast.castVUnsafe(), fr.ast.castFUnsafe()))
                         left = vl
                         right = fr
                         condCombine = false }
                       { parent =
                             FTree
                                 (F.And(vr.ast.castVUnsafe(), fl.ast.castFUnsafe()))
                         left = vr
                         right = fl
                         condCombine = false } |]
                Cost.getMinimumCost possibleCases p_sat 0.0 0.5 0.5
            | Or(l, r, lweight, rweight) -> 
                let le_par = best_e (l, (p_sat * lweight), (p_sat + rweight))
                let re_par = best_e (r, (p_sat * rweight), (p_sat * lweight))
                let lf = best_f (l, (p_sat * lweight), 0.0)
                let rf = best_f (r, (p_sat * rweight), 0.0)
                let lv = best_v (l, (p_sat * lweight), 0.0)
                let rv = best_v (r, (p_sat * rweight), 0.0)
                let maybelq = best_q (l, (p_sat * lweight), 0.0)
                let mayberq = best_q (r, (p_sat * rweight), 0.0)
                
                let possibleCases =
                    [| { parent =
                             FTree
                                 (F.CascadeOr
                                      (le_par.ast.castEUnsafe(), 
                                       rv.ast.castVUnsafe()))
                         left = le_par
                         right = rv
                         condCombine = false }
                       { parent =
                             FTree
                                 (F.CascadeOr
                                      (re_par.ast.castEUnsafe(), 
                                       lv.ast.castVUnsafe()))
                         left = re_par
                         right = lv
                         condCombine = false }
                       { parent =
                             FTree
                                 (F.SwitchOr
                                      (lf.ast.castFUnsafe(), rf.ast.castFUnsafe()))
                         left = lf
                         right = rf
                         condCombine = false }
                       { parent =
                             FTree
                                 (F.SwitchOr
                                      (rf.ast.castFUnsafe(), lf.ast.castFUnsafe()))
                         left = rf
                         right = lf
                         condCombine = false }
                       { parent =
                             FTree
                                 (F.SwitchOrV
                                      (lv.ast.castVUnsafe(), rv.ast.castVUnsafe()))
                         left = lv
                         right = rv
                         condCombine = false }
                       { parent =
                             FTree
                                 (F.SwitchOrV
                                      (rv.ast.castVUnsafe(), lv.ast.castVUnsafe()))
                         left = rv
                         right = lv
                         condCombine = false } |]
                
                let casesWithQ =
                    match maybelq, mayberq with
                    | Some lq, Some rq -> 
                        Array.append possibleCases [| { parent =
                                                            FTree
                                                                (F.DelayedOr
                                                                     (lq.ast.castQUnsafe
                                                                          (), 
                                                                      rq.ast.castQUnsafe
                                                                          ()))
                                                        left = lq
                                                        right = rq
                                                        condCombine = false } |]
                    | _ -> possibleCases
                
                Cost.getMinimumCost casesWithQ p_sat 0.0 lweight rweight
            | Threshold(n, subs) -> 
                let num_cost = scriptNumCost n
                let avg_cost = float n / float subs.Length
                let e =
                    best_e 
                        (subs.[0], (p_sat * avg_cost), 
                         (p_dissat + p_sat * (1.0 - avg_cost)))
                let ws =
                    subs 
                    |> Array.map 
                           (fun s -> 
                           best_w 
                               (s, (p_sat * avg_cost), 
                                (p_dissat + p_sat * (1.0 - avg_cost))))
                let pk_cost =
                    ws 
                    |> Array.fold (fun acc w -> acc + w.pkCost + 1u) 
                           (2u + num_cost + e.pkCost)
                let sat_cost =
                    ws |> Array.fold (fun acc w -> acc + w.satCost) e.satCost
                let dissat_cost =
                    ws |> Array.fold (fun acc w -> acc + w.dissatCost) e.dissatCost
                let wsast = ws |> Array.map (fun w -> w.ast.castWUnsafe())
                { ast = FTree(F.Threshold(n, e.ast.castEUnsafe(), wsast))
                  pkCost = pk_cost
                  satCost = sat_cost * avg_cost + dissat_cost * (1.0 - avg_cost)
                  dissatCost = 0.0 }
        
        and best_v (node : CompiledNode, p_sat : float, p_dissat : float) : Cost =
            match node with
            | Pk k -> 
                { ast = VTree(V.CheckSig(k))
                  pkCost = 35u
                  satCost = 0.0
                  dissatCost = 0.0 }
            | Multi(m, pks) -> 
                let num_cost = getPkCost m pks
                { ast = VTree(V.CheckMultiSig(m, pks))
                  pkCost = uint32 (num_cost + 34 * pks.Length + 1)
                  satCost = 1.0 + 72.0 * float m
                  dissatCost = 0.0 }
            | Time t -> 
                let num_cost = scriptNumCost (!> t)
                { ast = VTree(V.Time(t))
                  pkCost = 2u + num_cost
                  satCost = 0.0
                  dissatCost = 0.0 }
            | Hash h -> 
                { ast = VTree(V.HashEqual(h))
                  pkCost = 39u
                  satCost = 33.0
                  dissatCost = 0.0 }
            | And(l, r) -> 
                let lv = best_v (l, p_sat, 0.0)
                let rv = best_v (r, p_sat, 0.0)
                { ast = VTree(V.And(lv.ast.castVUnsafe(), rv.ast.castVUnsafe()))
                  pkCost = lv.pkCost + rv.pkCost
                  satCost = lv.satCost + rv.satCost
                  dissatCost = 0.0 }
            | Or(l, r, lweight, rweight) -> 
                let le_par = best_e (l, (p_sat * lweight), (p_sat * rweight))
                let re_par = best_e (r, (p_sat * rweight), (p_sat * lweight))
                let lt = best_t (l, (p_sat * lweight), 0.0)
                let rt = best_t (r, (p_sat * rweight), 0.0)
                let lv = best_v (l, (p_sat * lweight), 0.0)
                let rv = best_v (r, (p_sat * rweight), 0.0)
                let maybelq = best_q (l, (p_sat * lweight), 0.0)
                let mayberq = best_q (r, (p_sat * rweight), 0.0)
                
                let possibleCases =
                    [| { parent =
                             VTree
                                 (V.CascadeOr
                                      (le_par.ast.castEUnsafe(), 
                                       rv.ast.castVUnsafe()))
                         left = le_par
                         right = rv
                         condCombine = false }
                       { parent =
                             VTree
                                 (V.CascadeOr
                                      (re_par.ast.castEUnsafe(), 
                                       lv.ast.castVUnsafe()))
                         left = re_par
                         right = lv
                         condCombine = false }
                       { parent =
                             VTree
                                 (V.SwitchOr
                                      (lv.ast.castVUnsafe(), rv.ast.castVUnsafe()))
                         left = lv
                         right = rv
                         condCombine = false }
                       { parent =
                             VTree
                                 (V.SwitchOr
                                      (rv.ast.castVUnsafe(), lv.ast.castVUnsafe()))
                         left = rv
                         right = lv
                         condCombine = false }
                       { parent =
                             VTree
                                 (V.SwitchOrT
                                      (lt.ast.castTUnsafe(), rt.ast.castTUnsafe()))
                         left = lt
                         right = rt
                         condCombine = false }
                       { parent =
                             VTree
                                 (V.SwitchOrT
                                      (rt.ast.castTUnsafe(), lt.ast.castTUnsafe()))
                         left = rt
                         right = lt
                         condCombine = false } |]
                
                let casesWithQ =
                    match maybelq, mayberq with
                    | Some lq, Some rq -> 
                        Array.append possibleCases [| { parent =
                                                            VTree
                                                                (V.DelayedOr
                                                                     (lq.ast.castQUnsafe
                                                                          (), 
                                                                      rq.ast.castQUnsafe
                                                                          ()))
                                                        left = lq
                                                        right = rq
                                                        condCombine = false } |]
                    | _ -> possibleCases
                
                Cost.getMinimumCost casesWithQ p_sat 0.0 lweight rweight
            | Threshold(n, subs) -> 
                let num_cost = scriptNumCost n
                let avg_cost = float n / float subs.Length
                let e =
                    best_e 
                        (subs.[0], (p_sat * avg_cost), (p_sat * (1.0 - avg_cost)))
                let ws =
                    subs 
                    |> Array.map 
                           (fun s -> 
                           best_w 
                               (s, (p_sat * avg_cost), (p_sat * (1.0 - avg_cost))))
                let pk_cost =
                    ws 
                    |> Array.fold (fun acc w -> acc + w.pkCost + 1u) 
                           (1u + num_cost + e.pkCost)
                let sat_cost =
                    ws |> Array.fold (fun acc w -> acc + w.satCost) e.satCost
                let dissat_cost =
                    ws |> Array.fold (fun acc w -> acc + w.dissatCost) e.dissatCost
                let wsast = ws |> Array.map (fun w -> w.ast.castWUnsafe())
                { ast = VTree(V.Threshold(n, e.ast.castEUnsafe(), wsast))
                  pkCost = pk_cost
                  satCost = sat_cost * avg_cost + dissat_cost * (1.0 - avg_cost)
                  dissatCost = 0.0 }

    type CompiledNode with
        static member fromPolicy (p : Policy) = CompiledNode.fromPolicy p
        member this.compile() =
            let node = CompiledNode.best_t (this, 1.0, 0.0)
            MiniScript.fromAST (node.ast)

        member this.compileUnsafe() =
            match this.compile() with
            | Ok miniscript -> miniscript
            | Error e -> failwith e
        