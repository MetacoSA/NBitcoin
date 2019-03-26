module FNBitcoin.MiniScriptAST

open NBitcoin
open FNBitcoin.Utils
open System.Text

// TODO: Use unativeint instead of uint?

/// "E"xpression. takes more than one inputs from the stack, if it satisfies the condition,
/// It will leave 1 onto the stack, otherwise leave 0
/// E and W are the only type which is able to dissatisfy without failing the whole script.
type E =
    | CheckSig of PubKey
    | CheckMultiSig of uint32 * PubKey []
    | Time of LockTime
    | Threshold of (uint32 * E * W [])
    | ParallelAnd of (E * W)
    | CascadeAnd of (E * F)
    | ParallelOr of (E * W)
    | CascadeOr of (E * E)
    | SwitchOrLeft of (E * F)
    | SwitchOrRight of (E * F)
    | Likely of F
    | Unlikely of F

/// "W"rapped. say top level element is `X`, then consume items from the next element.
/// and leave one of [1,X] [X,1] if it satisfied the condition. otherwise
/// leave [0,X] or [X,0] onto the stack.
and W =
    | CheckSig of PubKey
    | HashEqual of uint256
    | Time of LockTime
    | CastE of E

/// "Q"ueue. Similar to F, but leaves public key buffer on the stack instead of 1
and Q =
    | Pubkey of PubKey
    | And of (V * Q)
    | Or of (Q * Q)


/// "F"orced. Similar to T, but always leaves 1 on the stack.
and F =
    | CheckSig of PubKey
    | CheckMultiSig of uint32 * PubKey []
    | Time of LockTime
    | HashEqual of uint256
    | Threshold of (uint32 * E * W [])
    | And of (V * F)
    | CascadeOr of (E * V)
    | SwitchOr of (F * F)
    | SwitchOrV of (V * V)
    | DelayedOr of (Q * Q)

/// "V"erify. Similar to the T, but does not leave anything on the stack
and V =
    | CheckSig of PubKey
    | CheckMultiSig of uint32 * PubKey []
    | Time of LockTime
    | HashEqual of uint256
    | Threshold of (uint32 * E * W [])
    | And of (V * V)
    | CascadeOr of (E * V)
    | SwitchOr of (V * V)
    | SwitchOrT of (T * T)
    | DelayedOr of (Q * Q)

/// "T"opLevel representation. Must be satisfied, and leave zero (or non-zero) value onto the stack
and T =
    | Time of LockTime
    | HashEqual of uint256
    | And of (V * T)
    | ParallelOr of (E * W)
    | CascadeOr of (E * T)
    | CascadeOrV of (E * V)
    | SwitchOr of (T * T)
    | SwitchOrV of (V * V)
    | DelayedOr of (Q * Q)
    | CastE of E

type AST =
    | ETree of E
    | QTree of Q
    | WTree of W
    | FTree of F
    | VTree of V
    | TTree of T

type ASTType =
    | EExpr
    | QExpr
    | WExpr
    | FExpr
    | VExpr
    | TExpr

let private EncodeUint (n: uint32) =
    Op.GetPushOp(int64 n).ToString()

let private EncodeInt (n: int32) =
    Op.GetPushOp(int64 n).ToString()

type E with
    
    member this.print() =
        match this with
        | CheckSig pk -> sprintf "E.pk(%s)" (pk.ToHex())
        | CheckMultiSig(m, pks) -> 
            sprintf "E.multi(%d,%s)" m 
                (pks 
                 |> Array.fold (fun acc k -> sprintf "%s,%s" acc (k.ToString())) 
                        "")
        | Time t -> sprintf "E.time(%s)" (t.ToString())
        | Threshold(num, e, ws) -> 
            sprintf "E.thres(%d,%s,%s)" num (e.print()) 
                (ws 
                 |> Array.fold (fun acc w -> sprintf "%s,%s" acc (w.print())) "")
        | ParallelAnd(e, w) -> sprintf "E.and_p(%s,%s)" (e.print()) (w.print())
        | CascadeAnd(e, f) -> sprintf "E.and_c(%s,%s)" (e.print()) (f.print())
        | ParallelOr(e, w) -> sprintf "E.or_p(%s,%s)" (e.print()) (w.print())
        | CascadeOr(e, e2) -> sprintf "E.or_c(%s,%s)" (e.print()) (e2.print())
        | SwitchOrLeft(e, f) -> sprintf "E.or_s(%s,%s)" (e.print()) (f.print())
        | SwitchOrRight(e, f) -> sprintf "E.or_r(%s,%s)" (e.print()) (f.print())
        | Likely f -> sprintf "E.lift_l(%s)" (f.print())
        | Unlikely f -> sprintf "E.lift_u(%s)" (f.print())
    
    member this.Serialize(sb : StringBuilder) : StringBuilder =
        match this with
        | CheckSig pk -> sb.AppendFormat(" {0} OP_CHECKSIG", pk)
        | CheckMultiSig(m, pks) -> 
            sb.AppendFormat(" {0}", (EncodeUint m)) |> ignore
            for pk in pks do
                do sb.AppendFormat(" {0}", (pk.ToHex())) |> ignore
            sb.AppendFormat(" {0} OP_CHECKMULTISIG", EncodeInt(pks.Length)) |> ignore
            sb
        | Time t -> 
            sb.AppendFormat(" OP_DUP OP_IF {0} OP_CSV OP_DROP OP_ENDIF", EncodeUint(!> t))
        | Threshold(k, e, ws) -> 
            e.Serialize(sb) |> ignore
            for w in ws do
                w.Serialize(sb) |> ignore
                sb.Append(" OP_ADD") |> ignore
            sb.AppendFormat(" {0} OP_EQUAL", (EncodeUint k))
        | ParallelAnd(l, r) -> 
            l.Serialize(sb) |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_BOOLAND")
        | CascadeAnd(l, r) -> 
            l.Serialize(sb) |> ignore
            sb.Append(" OP_NOTIF 0 OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF")
        | ParallelOr(l, r) -> 
            l.Serialize(sb) |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_BOOLOR")
        | CascadeOr(l, r) -> 
            l.Serialize(sb) |> ignore
            sb.Append(" OP_IFDUP OP_NOTIF") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF")
        | SwitchOrLeft(l, r) -> 
            sb.Append(" OP_IF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF")
        | SwitchOrRight(l, r) -> 
            sb.Append(" OP_NOTIF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF")
        | Likely(f) -> 
            sb.Append(" OP_NOTIF") |> ignore
            f.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE 0 OP_ENDIF")
        | Unlikely(f) -> 
            sb.Append(" OP_IF") |> ignore
            f.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE 0 OP_ENDIF")
    
    member this.toE() = this
    member this.toT() =
        match this with
        | ParallelOr(l, r) -> T.ParallelOr(l, r)
        | x -> T.CastE(x)

and Q with
    
    member this.print() =
        match this with
        | Pubkey p -> sprintf "Q.pk(%s)" (p.ToString())
        | And(v, q) -> sprintf "Q.and(%s,%s)" (v.print()) (q.print())
        | Or(q1, q2) -> sprintf "Q.or(%s,%s)" (q1.print()) (q2.print())
    
    member this.Serialize(sb : StringBuilder) : StringBuilder =
        match this with
        | Pubkey pk -> sb.AppendFormat(" {0}", (pk.ToHex()))
        | And(l, r) -> 
            l.Serialize(sb) |> ignore
            r.Serialize(sb)
        | Or(l, r) -> 
            sb.Append(" OP_IF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF")

and W with
    
    member this.print() =
        match this with
        | CheckSig pk -> sprintf "W.pk(%s)" (pk.ToString())
        | HashEqual u -> sprintf "W.hash(%s)" (u.ToString())
        | Time t -> sprintf "W.time(%s)" (t.ToString())
        | CastE e -> e.print()
    
    member this.Serialize(sb : StringBuilder) : StringBuilder =
        match this with
        | CheckSig pk -> 
            sb.Append(" OP_SWAP") |> ignore
            sb.AppendFormat(" {0}", (pk.ToHex())) |> ignore
            sb.Append(" OP_CHECKSIG")
        | HashEqual h -> 
            sb.Append
                (sprintf " OP_SWAP OP_SIZE OP_0NOTEQUAL OP_IF OP_SIZE %s OP_EQUALVERIFY OP_SHA256"
                    (EncodeInt 32)) 
            |> ignore
            sb.AppendFormat(" {0}", h.ToString()) |> ignore
            sb.Append(" OP_EQUALVERIFY 1 OP_ENDIF")
        | Time t -> 
            sb.AppendFormat
                (" OP_SWAP OP_DUP OP_IF {0} OP_CSV OP_DROP OP_ENDIF", (EncodeUint (!> t)))
        | CastE e -> 
            sb.Append(" OP_TOALTSTACK") |> ignore
            e.Serialize(sb) |> ignore
            sb.Append(" OP_FROMALTSTACK")

and F with
    
    member this.print() =
        match this with
        | CheckSig pk -> sprintf "F.pk(%s)" (pk.ToString())
        | CheckMultiSig(m, pks) -> 
            sprintf "F.multi(%d,%s)" m 
                (pks 
                 |> Array.fold (fun acc k -> sprintf "%s,%s" acc (k.ToString())) 
                        "")
        | Time t -> sprintf "F.time(%s)" (t.ToString())
        | HashEqual h -> sprintf "F.hash(%s)" (h.ToString())
        | Threshold(num, e, ws) -> 
            sprintf "F.thres(%d,%s,%s)" num (e.print()) 
                (ws 
                 |> Array.fold (fun acc w -> sprintf "%s,%s" acc (w.print())) "")
        | And(l, r) -> sprintf "F.and(%s,%s)" (l.print()) (r.print())
        | CascadeOr(l, r) -> sprintf "F.or_v(%s,%s)" (l.print()) (r.print())
        | SwitchOr(l, r) -> sprintf "F.or_s(%s,%s)" (l.print()) (r.print())
        | SwitchOrV(l, r) -> sprintf "F.or_a(%s,%s)" (l.print()) (r.print())
        | DelayedOr(l, r) -> sprintf "F.or_d(%s,%s)" (l.print()) (r.print())
    
    member this.toE() = this
    
    member this.toT() =
        match this with
        | CascadeOr(l, r) -> T.CascadeOrV(l, r)
        | SwitchOrV(l, r) -> T.SwitchOrV(l, r)
        | x -> failwith (sprintf "%s is not a T" (x.print()))
    
    member this.Serialize(sb : StringBuilder) : StringBuilder =
        match this with
        | CheckSig pk -> 
            sb.AppendFormat(" {0} OP_CHECKSIGVERIFY 1", (pk.ToHex()))
        | CheckMultiSig(m, pks) -> 
            sb.AppendFormat(" {0}", (EncodeUint m)) |> ignore
            for pk in pks do
                sb.AppendFormat(" {0}", (pk.ToHex())) |> ignore
            sb.AppendFormat(" {0} OP_CHECKMULTISIGVERIFY 1", (EncodeInt pks.Length))
        | Time t -> sb.AppendFormat(" {0} OP_CSV OP_0NOTEQUAL", (EncodeUint (!> t)))
        | HashEqual h -> 
            sb.AppendFormat
                (" OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 {0} OP_EQUALVERIFY 1", h)
        | Threshold(k, e, ws) -> 
            e.Serialize(sb) |> ignore
            for w in ws do
                w.Serialize(sb) |> ignore
                sb.Append(" OP_ADD") |> ignore
            sb.AppendFormat(" {0} OP_EQUALVERIFY 1", (EncodeUint k))
        | And(l, r) -> 
            l.Serialize(sb) |> ignore
            r.Serialize(sb)
        | SwitchOr(l, r) -> 
            sb.AppendFormat(" OP_IF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF")
        | SwitchOrV(l, r) -> 
            sb.AppendFormat(" OP_IF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF 1")
        | CascadeOr(l, r) -> 
            l.Serialize(sb) |> ignore
            sb.Append(" OP_NOTIF") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF 1")
        | DelayedOr(l, r) -> 
            sb.Append(" OP_IF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF OP_CHECKSIGVERIFY 1")

and V with
    
    member this.print() =
        match this with
        | CheckSig pk -> sprintf "V.pk(%s)" (pk.ToString())
        | CheckMultiSig(m, pks) -> 
            sprintf "V.multi(%d,%s)" m 
                (pks 
                 |> Array.fold (fun acc k -> sprintf "%s,%s" acc (k.ToString())) 
                        "")
        | Time t -> sprintf "V.time(%s)" (t.ToString())
        | HashEqual h -> sprintf "V.hash(%s)" (h.ToString())
        | Threshold(num, e, ws) -> 
            sprintf "V.thres(%d,%s,%s)" num (e.print()) 
                (ws 
                 |> Array.fold (fun acc w -> sprintf "%s,%s" acc (w.print())) "")
        | And(l, r) -> sprintf "V.and(%s,%s)" (l.print()) (r.print())
        | CascadeOr(l, r) -> sprintf "V.or_v(%s,%s)" (l.print()) (r.print())
        | SwitchOr(l, r) -> sprintf "V.or_s(%s,%s)" (l.print()) (r.print())
        | SwitchOrT(l, r) -> sprintf "V.or_a(%s,%s)" (l.print()) (r.print())
        | DelayedOr(l, r) -> sprintf "V.or_d(%s,%s)" (l.print()) (r.print())
    
    member this.Serialize(sb : StringBuilder) : StringBuilder =
        match this with
        | CheckSig pk -> 
            sb.AppendFormat(" {0} OP_CHECKSIGVERIFY ", (pk.ToHex()))
        | CheckMultiSig(m, pks) -> 
            sb.AppendFormat(" {0}", (EncodeUint m)) |> ignore
            for pk in pks do
                sb.AppendFormat(" {0}", (pk.ToHex())) |> ignore
            sb.AppendFormat(" {0} OP_CHECKMULTISIGVERIFY", (EncodeInt pks.Length))
        | Time t -> sb.AppendFormat(" {0} OP_CSV OP_DROP", (EncodeUint (!> t)))
        | HashEqual h -> 
            sb.AppendFormat
                (" OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 {0} OP_EQUALVERIFY", h)
        | Threshold(k, e, ws) -> 
            e.Serialize(sb) |> ignore
            for w in ws do
                w.Serialize(sb) |> ignore
                sb.Append(" OP_ADD") |> ignore
            sb.AppendFormat(" {0} OP_EQUALVERIFY", (EncodeUint k))
        | And(l, r) -> 
            l.Serialize(sb) |> ignore
            r.Serialize(sb)
        | SwitchOr(l, r) -> 
            sb.AppendFormat(" OP_IF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF")
        | SwitchOrT(l, r) -> 
            sb.AppendFormat(" OP_IF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF OP_VERIFY")
        | CascadeOr(l, r) -> 
            l.Serialize(sb) |> ignore
            sb.Append(" OP_NOTIF") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF")
        | DelayedOr(l, r) -> 
            sb.Append(" OP_IF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF OP_CHECKSIGVERIFY")

and T with
    
    member this.print() =
        match this with
        | Time t -> sprintf "T.time(%s)" (t.ToString())
        | HashEqual h -> sprintf "T.hash(%s)" (h.ToString())
        | And(l, r) -> sprintf "T.and_p(%s,%s)" (l.print()) (r.print())
        | ParallelOr(l, r) -> sprintf "T.or_vp(%s,%s)" (l.print()) (r.print())
        | CascadeOr(l, r) -> sprintf "T.or_c(%s,%s)" (l.print()) (r.print())
        | CascadeOrV(l, r) -> sprintf "T.or_v(%s,%s)" (l.print()) (r.print())
        | SwitchOr(l, r) -> sprintf "T.or_s(%s,%s)" (l.print()) (r.print())
        | SwitchOrV(l, r) -> sprintf "T.or_a(%s,%s)" (l.print()) (r.print())
        | DelayedOr(l, r) -> sprintf "T.or_d(%s,%s)" (l.print()) (r.print())
        | CastE e -> sprintf "T.%s" (e.print())
    
    member this.Serialize(sb : StringBuilder) : StringBuilder =
        match this with
        | Time t -> sb.AppendFormat(" {0} OP_CSV", (EncodeUint (!> t)))
        | HashEqual h -> 
            sb.AppendFormat
                (" OP_SIZE 20 OP_EQUALVERIFY OP_SHA256 {0} OP_EQUAL", h)
        | And(l, r) -> 
            l.Serialize(sb) |> ignore
            r.Serialize(sb)
        | ParallelOr(l, r) -> 
            l.Serialize(sb) |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_BOOLOR")
        | CascadeOr(l, r) -> 
            l.Serialize(sb) |> ignore
            sb.Append(" OP_IFDUP OP_NOTIF") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF")
        | CascadeOrV(l, r) -> 
            l.Serialize(sb) |> ignore
            sb.Append(" OP_NOTIF") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF 1")
        | SwitchOr(l, r) -> 
            sb.AppendFormat(" OP_IF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF")
        | SwitchOrV(l, r) -> 
            sb.AppendFormat(" OP_IF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF 1")
        | DelayedOr(l, r) -> 
            sb.Append(" OP_IF") |> ignore
            l.Serialize(sb) |> ignore
            sb.Append(" OP_ELSE") |> ignore
            r.Serialize(sb) |> ignore
            sb.Append(" OP_ENDIF OP_CHECKSIG")
        | CastE e -> e.Serialize(sb)

type AST with
    
    member this.Print() =
        match this with
        | ETree e -> e.print()
        | QTree q -> q.print()
        | WTree w -> w.print()
        | FTree f -> f.print()
        | VTree v -> v.print()
        | TTree t -> t.print()
    
    member this.ToScript() =
        let sb = StringBuilder()
        match this with
        | ETree e -> 
            let s = e.Serialize(sb)
            NBitcoin.Script(s.ToString())
        | QTree q -> 
            let s = q.Serialize(sb)
            NBitcoin.Script(s.ToString())
        | WTree w -> 
            let s = w.Serialize(sb)
            NBitcoin.Script(s.ToString())
        | FTree f -> 
            let s = f.Serialize(sb)
            NBitcoin.Script(s.ToString())
        | VTree v -> 
            let s = v.Serialize(sb)
            NBitcoin.Script(s.ToString())
        | TTree t -> 
            let s = t.Serialize(sb)
            let str = s.ToString()
            NBitcoin.Script(str)
    member this.GetASTType() =
        match this with
        | ETree _ -> EExpr
        | QTree _ -> QExpr
        | WTree _ -> WExpr
        | FTree _ -> FExpr
        | VTree _ -> VExpr
        | TTree _ -> TExpr

    member this.IsT() =
        match this with
        | ETree _ 
        | TTree _ -> true
        | FTree f ->
            match f with
            | F.CascadeOr _
            | F.SwitchOrV _ -> true
            | _ -> false
        | _ -> false

    member this.castT() : Result<T, string> =
        match this with
        | TTree t -> Ok t
        | FTree f ->
            match f with
            | F.CascadeOr(l, r) ->  Ok(T.CascadeOrV(l, r))
            | F.SwitchOrV(l, r) ->  Ok(T.SwitchOrV(l, r))
            | _ -> Error(sprintf "failed to cast %s" (this.Print()))
        | ETree e ->
            match e with
            | E.ParallelOr(l, r) ->  Ok(T.ParallelOr(l, r))
            | otherE -> Ok(T.CastE(otherE))
        | _ -> Error(sprintf "failed to cast %s" (this.Print()))
    
    member this.castE() : Result<E, string> =
        match this with
        | ETree e -> Ok e
        | _ -> Error(sprintf "failed to cast %s" (this.Print()))
    
    member this.castQ() : Result<Q, string> =
        match this with
        | QTree q -> Ok q
        | _ -> Error(sprintf "failed to cast %s" (this.Print()))
    
    member this.castW() : Result<W, string> =
        match this with
        | WTree w -> Ok w
        | _ -> Error(sprintf "failed to cast %s" (this.Print()))
    
    member this.castF() : Result<F, string> =
        match this with
        | FTree f -> Ok f
        | _ -> Error(sprintf "failed to cast %s" (this.Print()))
    
    member this.castV() : Result<V, string> =
        match this with
        | VTree v -> Ok v
        | _ -> Error(sprintf "failed to cast %s" (this.Print()))
    
    member this.castTUnsafe() : T =
        match this.castT() with
        | Ok t -> t
        | Error s -> failwith s
    
    member this.castEUnsafe() : E =
        match this.castE() with
        | Ok e -> e
        | Error s -> failwith s
    
    member this.castQUnsafe() : Q =
        match this.castQ() with
        | Ok q -> q
        | Error s -> failwith s
    
    member this.castWUnsafe() : W =
        match this.castW() with
        | Ok w -> w
        | Error s -> failwith s
    
    member this.castFUnsafe() : F =
        match this.castF() with
        | Ok f -> f
        | Error s -> failwith s
    
    member this.castVUnsafe() : V =
        match this.castV() with
        | Ok v -> v
        | Error s -> failwith s
