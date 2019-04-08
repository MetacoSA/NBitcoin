namespace NBitcoin.Miniscript

open NBitcoin
open System

type SignatureProvider = PubKey -> TransactionSignature option
type PreImageHash = uint256
type PreImage = uint256
type PreImageProvider = PreImageHash -> PreImage option

type ProviderSet = (SignatureProvider option * PreImageProvider option * LockTime option)

type CSVOffset = BlockHeight of uint32 | UnixTime of DateTimeOffset
type FailureCase =
    | MissingSig of PubKey list
    | MissingHash of uint256
    | NotMatured of CSVOffset
    | LockTimeTypeMismatch 
    | Nested of FailureCase list
    | CurrentTimeNotSpecified

type SatisfiedItem =
    | PreImage of uint256
    | Signature of TransactionSignature
    | RawPush of byte[]
    with member this.ToBytes(): byte array =
            match this with
            | RawPush i -> i
            | PreImage i -> i.ToBytes()
            | Signature i -> i.ToBytes()

type SatisfactionResult = Result<SatisfiedItem list, FailureCase>

module internal Satisfy =
    open NBitcoin.Miniscript.AST
    open NBitcoin.Miniscript.Utils
    open NBitcoin
    open System

    let satisfyCost (res: SatisfiedItem list): int =
        res |> List.fold(fun a b -> 1 + b.ToBytes().Length + a) 0

    let (>>=) xR f = Result.bind f xR

    // ------- helpers --------
    let satisfyCheckSig (maybeKeyFn: SignatureProvider option) k =
        match maybeKeyFn with
        | None -> Error(MissingSig([k]))
        | Some keyFn ->
            match keyFn k with
            | None -> Error (MissingSig [k])
            | Some(txSig) -> Ok([Signature(txSig)])

    let satisfyCheckMultisig (maybeKeyFn: SignatureProvider option) (m, pks) =
        match maybeKeyFn with
        | None -> Error(MissingSig(pks |> List.ofArray))
        | Some keyFn ->
            let maybeSigList = pks
                               |> Array.map(keyFn)
                               |> Array.toList

            let sigList = maybeSigList |> List.choose(id) |> List.map(Signature)

            if sigList.Length >= (int32 m) then
                Ok(sigList)
            else
                let sigNotFoundPks = maybeSigList
                                     |> List.zip (pks |> Array.toList)
                                     |> List.choose(fun (pk, maybeSig) ->
                                                        if maybeSig.IsNone then Some(pk) else None)
                Error(MissingSig(sigNotFoundPks))

    let satisfyHashEqual (maybeHashFn: PreImageProvider option) h =
        match maybeHashFn with
        | None -> Error(MissingHash(h))
        | Some fn ->
            match fn h with
            | None -> Error(MissingHash h)
            | Some v -> Ok([PreImage(v)])

    let satisfyCSVCore (age: LockTime) (t: LockTime) =
        let offset = (int32 t.Value) - (int32 age.Value)
        if (age.IsHeightLock && t.IsHeightLock) then
            if (offset > 0) then
                Error(NotMatured(BlockHeight (uint32 offset)))
            else
                Ok([])
        else if (age.IsTimeLock && t.IsTimeLock) then
            if (offset > 0) then
                Error(NotMatured(UnixTime(DateTimeOffset.FromUnixTimeSeconds(int64 (offset)))))
            else
                Ok([])
        else
            Error(LockTimeTypeMismatch)

    let satisfyCSV (age: LockTime option) (t: LockTime) =
        match age with
        | None -> Error(CurrentTimeNotSpecified)
        | Some a -> satisfyCSVCore a t

    let rec satisfyThreshold (providers) (k, e, ws): SatisfactionResult =
        let keyFn, hashFn, age = providers
        let flatten l = List.collect id l
        let wsList = ws |> Array.toList

        let wResult = wsList
                      |> List.rev
                      |> List.map(satisfyW providers)
        let wOkList = wResult
                      |> List.filter(fun wr -> match wr with | Ok w -> true;| _ -> false)
                      |> List.map(fun wr -> match wr with | Ok w -> w; | _ -> failwith "unreachable")

        let wErrorList = wResult
                         |> List.filter(fun wr -> match wr with | Error w -> true;| _ -> false)
                         |> List.map(fun wr -> match wr with | Error e -> e; | _ -> failwith "unreachable")

        let eResult = satisfyE (keyFn, hashFn, age) e |> List.singleton
        let eOkList = eResult
                      |> List.filter(fun wr -> match wr with | Ok w -> true;| _ -> false)
                      |> List.map(fun wr -> match wr with | Ok w -> w; | _ -> failwith "unreachable")

        let eErrorList = eResult
                         |> List.filter(fun wr -> match wr with | Error w -> true;| _ -> false)
                         |> List.map(fun wr -> match wr with | Error e -> e; | _ -> failwith "unreachable")


        let satisfiedTotal = wOkList.Length + eOkList.Length

        if satisfiedTotal >= (int k) then
            let dissatisfiedW = List.zip wsList wResult
                                |> List.choose(fun (w, wr) -> match wr with | Error _ -> Some(w); | _ -> None)
                                |> List.map(dissatisfyW)
            let dissatisfiedE = match eResult.[0] with | Error _ -> [dissatisfyE e] | Ok _ -> []
            Ok(flatten (wOkList @ eOkList @ dissatisfiedW @ dissatisfiedE))
        else
            Error(Nested(wErrorList @ eErrorList))

    and satisfyAST providers (ast: AST) =
        match ast.GetASTType() with
        | EExpr -> satisfyE providers (ast.CastEUnsafe())
        | FExpr -> satisfyF providers (ast.CastFUnsafe())
        | WExpr -> satisfyW providers (ast.CastWUnsafe())
        | QExpr -> satisfyQ providers (ast.CastQUnsafe())
        | TExpr -> satisfyT providers (ast.CastTUnsafe())
        | VExpr -> satisfyV providers (ast.CastVUnsafe())

    and dissatisfyAST (ast: AST) =
        match ast.GetASTType() with
        | EExpr -> dissatisfyE (ast.CastEUnsafe())
        | WExpr -> dissatisfyW (ast.CastWUnsafe())
        | _ -> failwith "unreachable"

    and satisfyParallelOr providers (l: AST, r: AST) =
        match (satisfyAST providers l), (satisfyAST providers r) with
        | Ok(lItems), Ok (rItems) -> // return the one has less cost
            let lDissat = dissatisfyAST l
            let rDissat = dissatisfyAST r
            if (satisfyCost rDissat + satisfyCost lItems <= satisfyCost rItems + satisfyCost lDissat) then
                Ok(rDissat @ lItems)
            else
                Ok(lDissat @ rItems)
        | Ok(lItems), Error _ ->
            let rDissat = dissatisfyAST r
            Ok(lItems @ rDissat)
        | Error _, Ok(rItems) ->
            let lDissat = dissatisfyAST l
            Ok(rItems @ lDissat)
        | Error e1, Error e2 -> Error(Nested([e1; e2]))

    and satisfyCascadeOr providers (l, r) =
        match (satisfyAST providers l), (satisfyAST providers r) with
        | Error e, Error _ -> Error e
        | Ok lItems, Error _ -> Ok(lItems)
        | Error _, Ok rItems ->
            let lDissat = dissatisfyAST l
            Ok(rItems @ lDissat)
        | Ok lItems, Ok rItems ->
            let lDissat = dissatisfyAST l
            if satisfyCost lItems <= satisfyCost rItems + satisfyCost lDissat then
                Ok(lItems)
            else
                Ok(rItems)

    and satisfySwitchOr providers (l, r) =
        match (satisfyAST providers l), (satisfyAST providers r) with
        | Error e, Error _ -> Error e
        | Ok lItems, Error _ -> Ok(lItems @ [RawPush([|byte 1|])])
        | Error e, Ok rItems -> Ok(rItems @ [RawPush([|byte 0|])])
        | Ok lItems, Ok rItems -> // return the one has less cost
            if satisfyCost(lItems) + 2 <= satisfyCost rItems + 1 then
                Ok(lItems @ [RawPush([|byte 1|])])
            else
                Ok(rItems @ [RawPush([|byte 0|])])

    and satisfyE (providers: ProviderSet) (e: E) =
        let keyFn, hashFn, age = providers
        match e with
        | E.CheckSig k -> satisfyCheckSig keyFn k
        | E.CheckMultiSig(m, pks) -> satisfyCheckMultisig keyFn (m, pks)
        | E.Time t ->  satisfyCSV age t
        | E.Threshold i ->
            satisfyThreshold providers i
        | E.ParallelAnd(e, w) ->
            satisfyE providers e
                >>= (fun eitem -> satisfyW providers w >>= (fun witem -> Ok(eitem @ witem)))
        | E.CascadeAnd(e, f) ->
            satisfyE providers e
                >>= (fun eitem -> satisfyF providers f >>= (fun fitem -> Ok(eitem @ fitem)))
        | E.ParallelOr(e, w) -> satisfyParallelOr providers (ETree(e), WTree(w))
        | E.CascadeOr(e1, e2) -> satisfyCascadeOr providers (ETree(e1), ETree(e2))
        | E.SwitchOrLeft(e, f) -> satisfySwitchOr providers (ETree(e), FTree(f))
        | E.SwitchOrRight(e, f) -> satisfySwitchOr providers (ETree(e), FTree(f))
        | E.Likely f ->
            satisfyF providers f |> Result.map(fun items -> items @ [RawPush([||])])
        | E.Unlikely f ->
            satisfyF providers f |> Result.map(fun items -> items @ [RawPush([|byte 1|])])

    and satisfyW (providers: ProviderSet) w: SatisfactionResult =
        let keyFn, hashFn, age = providers
        match w with
        | W.CheckSig pk -> satisfyCheckSig keyFn pk
        | W.HashEqual h -> satisfyHashEqual hashFn h
        | W.Time t ->
            satisfyCSV age t |> Result.map(fun items -> items @ [RawPush([| byte 1 |])])
        | W.CastE e  -> satisfyE providers e

    and satisfyT (providers) t =
        let (keyFn, hashFn, age) = providers
        match t with
        | T.Time t -> Ok([RawPush([||])])
        | T.HashEqual h -> satisfyHashEqual hashFn h
        | T.And(v, t) ->
            let rRes = satisfyT providers t
            let lRes = satisfyV providers v
            rRes >>= (fun rItems -> lRes >>= fun(lItems) -> Ok(rItems @ lItems))
        | T.ParallelOr(e, w) -> satisfyParallelOr providers (ETree(e), WTree(w))
        | T.CascadeOr(e, t) -> satisfyCascadeOr providers (ETree(e), TTree(t))
        | T.CascadeOrV(e, v) -> satisfyCascadeOr providers (ETree(e), VTree(v))
        | T.SwitchOr(t1, t2) -> satisfySwitchOr providers (TTree(t1), TTree(t2))
        | T.SwitchOrV(v1, v2) -> satisfySwitchOr providers (VTree(v1), VTree(v2))
        | T.DelayedOr(q1, q2) -> satisfySwitchOr providers (QTree(q1), QTree(q2))
        | T.CastE e -> satisfyE providers e

    and satisfyQ (providers) q =
        let (keyFn, hashFn, age) = providers
        match q with
        | Q.Pubkey pk -> satisfyCheckSig (keyFn) pk
        | Q.And(l, r) ->
            let rRes = satisfyQ providers r
            let lRes = satisfyV providers l
            rRes >>= (fun rItems -> lRes >>= fun(lItems) -> Ok(rItems @ lItems))
        | Q.Or(l, r) -> satisfySwitchOr providers (QTree(l), QTree(r))

    and satisfyF (providers) f =
        let (keyFn, hashFn, age) = providers
        match f with
        | F.CheckSig pk -> satisfyCheckSig keyFn pk
        | F.CheckMultiSig(m, pks) -> satisfyCheckMultisig keyFn (m, pks)
        | F.Time t -> satisfyCSV age t
        | F.HashEqual h -> satisfyHashEqual hashFn h
        | F.Threshold i -> satisfyThreshold providers i
        | F.And(v ,f) ->
            let rRes = satisfyF providers f
            let lRes = satisfyV providers v
            rRes >>= (fun rItems -> lRes >>= fun(lItems) -> Ok(rItems @ lItems))
        | F.CascadeOr(e, v) -> satisfyCascadeOr providers (ETree(e), VTree(v))
        | F.SwitchOr(f1, f2) -> satisfySwitchOr providers (FTree(f1), FTree(f2))
        | F.SwitchOrV(v1, v2) -> satisfySwitchOr providers (VTree(v1), VTree(v2))
        | F.DelayedOr(q1, q2) -> satisfySwitchOr providers (QTree(q1), QTree(q2))

    and satisfyV providers v =
        let (keyFn, hashFn, age) = providers
        match v with
        | V.CheckSig pk -> satisfyCheckSig keyFn pk
        | V.CheckMultiSig (m, pks) -> satisfyCheckMultisig keyFn (m, pks)
        | V.Time t -> satisfyCSV age t
        | V.HashEqual h -> satisfyHashEqual hashFn h
        | V.Threshold i -> satisfyThreshold providers i
        | V.And(v1, v2) ->
            let rRes = satisfyV providers v2
            let lRes = satisfyV providers v1
            rRes >>= (fun rItems -> lRes >>= fun(lItems) -> Ok(rItems @ lItems))
        | V.SwitchOr (v1, v2) -> satisfySwitchOr providers (VTree(v1), VTree(v2))
        | V.SwitchOrT (t1, t2) -> satisfySwitchOr providers (TTree(t1), TTree(t2))
        | V.CascadeOr(e, v) -> satisfyCascadeOr providers (ETree(e), VTree(v))
        | V.DelayedOr(q1, q2) -> satisfySwitchOr providers (QTree(q1), QTree(q2))

    and dissatisfyE (e: E): SatisfiedItem list =
        match e with
        | E.CheckSig pk -> [RawPush([| byte 0 |])]
        | E.CheckMultiSig (m, pks) -> [RawPush[| byte 0 |]; RawPush[| byte(m + 1u)|]]
        | E.Time t -> [RawPush([| byte 0 |])]
        | E.Threshold (_, e, ws) ->
            let wDissat = ws |> Array.toList |> List.rev |> List.map(dissatisfyW) |> List.collect id
            let eDissat = dissatisfyE e
            wDissat @ eDissat
        | E.ParallelAnd (e, w) ->
             (dissatisfyW w) @ (dissatisfyE e)
        | E.CascadeAnd (e, _) ->
             (dissatisfyE e)
        | E.ParallelOr (e, w)  ->
             (dissatisfyW w) @ (dissatisfyE e)
        | E.CascadeOr (e, e2) ->
             (dissatisfyE e2) @ (dissatisfyE e)
        | E.SwitchOrLeft (e, _) ->
             (dissatisfyE e) @ [RawPush[| byte 1 |]]
        | E.SwitchOrRight (e, _) ->
             (dissatisfyE e) @ [RawPush[||]]
        | E.Likely f -> [RawPush[| byte 1 |]]
        | E.Unlikely f -> [RawPush[||]]

    and dissatisfyW (w: W): SatisfiedItem list =
        match w with
        | W.CheckSig _ -> [RawPush[||]]
        | W.HashEqual _ -> [RawPush[||]]
        | W.Time _ -> [RawPush[||]]
        | W.CastE e -> dissatisfyE e

