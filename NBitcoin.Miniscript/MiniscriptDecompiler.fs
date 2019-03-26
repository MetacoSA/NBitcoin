module NBitcoin.Miniscript.Decompiler

open NBitcoin
open System
open NBitcoin.Miniscript.Utils.Parser
open Miniscript.AST

/// Subset of Bitcoin Script which is used in Miniscript
type Token =
    | BoolAnd
    | BoolOr
    | Add
    | Equal
    | EqualVerify
    | CheckSig
    | CheckSigVerify
    | CheckMultiSig
    | CheckMultiSigVerify
    | CheckSequenceVerify
    | FromAltStack
    | ToAltStack
    | Drop
    | Dup
    | If
    | IfDup
    | NotIf
    | Else
    | EndIf
    | ZeroNotEqual
    | Size
    | Swap
    | Tuck
    | Verify
    | Hash160
    | Sha256
    | Number of uint32
    | Hash160Hash of uint160
    | Sha256Hash of uint256
    | Pk of NBitcoin.PubKey
    | Any

type TokenCategory =
    | BoolAnd
    | BoolOr
    | Add
    | Equal
    | EqualVerify
    | CheckSig
    | CheckSigVerify
    | CheckMultiSig
    | CheckMultiSigVerify
    | CheckSequenceVerify
    | FromAltStack
    | ToAltStack
    | Drop
    | Dup
    | If
    | IfDup
    | NotIf
    | Else
    | EndIf
    | ZeroNotEqual
    | Size
    | Swap
    | Tuck
    | Verify
    | Hash160
    | Sha256
    | Number
    | Hash160Hash
    | Sha256Hash
    | Pk
    | Any

type ParseException(msg, ex : exn) =
    inherit Exception(msg, ex)
    new(msg) = ParseException(msg, null)

type Token with
    member this.GetItem() =
        match this with
        | Number n -> box n |> Some
        | Hash160Hash h -> box h |> Some
        | Sha256Hash h -> box h |> Some
        | Pk pk -> box pk |> Some
        | _ -> None
    member this.GetItemUnsafe() =
        match this with
        | Number n -> n :> obj
        | Hash160Hash h -> h :> obj
        | Sha256Hash h -> h :> obj
        | Pk pk -> pk :> obj
        | i -> failwith (sprintf "failed to get item from %A" i)

    // usual reflection is not working for extracing name of each case. So we need this.
    member this.GetCategory() =
        match this with
        | BoolAnd -> TokenCategory.BoolAnd
        | BoolOr -> TokenCategory.BoolOr
        | Add -> TokenCategory.Add
        | Equal -> TokenCategory.Equal
        | EqualVerify -> TokenCategory.EqualVerify
        | CheckSig -> TokenCategory.CheckSig
        | CheckSigVerify -> TokenCategory.CheckSigVerify
        | CheckMultiSig -> TokenCategory.CheckMultiSig
        | CheckMultiSigVerify -> TokenCategory.CheckMultiSigVerify
        | CheckSequenceVerify -> TokenCategory.CheckSequenceVerify
        | FromAltStack -> TokenCategory.FromAltStack
        | ToAltStack -> TokenCategory.ToAltStack
        | Drop -> TokenCategory.Drop
        | Dup -> TokenCategory.Dup
        | If -> TokenCategory.If
        | IfDup -> TokenCategory.IfDup
        | NotIf -> TokenCategory.NotIf
        | Else -> TokenCategory.Else
        | EndIf -> TokenCategory.EndIf
        | ZeroNotEqual -> TokenCategory.ZeroNotEqual
        | Size -> TokenCategory.Size
        | Swap -> TokenCategory.Swap
        | Tuck -> TokenCategory.Tuck
        | Verify -> TokenCategory.Verify
        | Hash160 -> TokenCategory.Hash160
        | Sha256 -> TokenCategory.Sha256
        | Number _ -> TokenCategory.Number
        | Hash160Hash _ -> TokenCategory.Hash160Hash
        | Sha256Hash _ -> TokenCategory.Sha256Hash
        | Pk _ -> TokenCategory.Pk
        | Any -> TokenCategory.Any

let private tryGetItemFromOp (op: Op) =
    let size = op.PushData.Length
    match size with
    | 20 -> Ok(Token.Hash160Hash(uint160 (op.PushData, false)))
    | 32 -> Ok(Token.Sha256Hash(uint256 (op.PushData, false)))
    | 33 -> 
        try 
            Ok(Token.Pk(NBitcoin.PubKey(op.PushData)))
        with :? FormatException as ex -> 
            Error(ParseException("Invalid Public Key", ex))
    | _ -> 
        match op.GetInt().HasValue with
        | true -> 
            let v = op.GetInt().Value
            /// no need to check v >= 0 since it is checked in NBitcoin side
            Ok(Token.Number(uint32 v))
        | false -> 
            Error(ParseException(sprintf "Invalid push with Opcode %O" op))

let private castOpToToken (op : Op) : Result<Token, ParseException> =
    match (op.Code) with
    | OpcodeType.OP_BOOLAND -> Ok(Token.BoolAnd)
    | OpcodeType.OP_BOOLOR -> Ok(Token.BoolOr)
    | OpcodeType.OP_EQUAL -> Ok(Token.Equal)
    | OpcodeType.OP_EQUALVERIFY -> Ok(Token.EqualVerify)
    | OpcodeType.OP_CHECKSIG -> Ok(Token.CheckSig)
    | OpcodeType.OP_CHECKSIGVERIFY -> Ok(Token.CheckSigVerify)
    | OpcodeType.OP_CHECKMULTISIG -> Ok(Token.CheckMultiSig)
    | OpcodeType.OP_CHECKMULTISIGVERIFY -> Ok(Token.CheckMultiSigVerify)
    | OpcodeType.OP_CHECKSEQUENCEVERIFY -> Ok(Token.CheckSequenceVerify)
    | OpcodeType.OP_FROMALTSTACK -> Ok(Token.FromAltStack)
    | OpcodeType.OP_TOALTSTACK -> Ok(Token.ToAltStack)
    | OpcodeType.OP_DROP -> Ok(Token.Drop)
    | OpcodeType.OP_DUP -> Ok(Token.Dup)
    | OpcodeType.OP_IF -> Ok(Token.If)
    | OpcodeType.OP_IFDUP -> Ok(Token.IfDup)
    | OpcodeType.OP_NOTIF -> Ok(Token.NotIf)
    | OpcodeType.OP_ELSE -> Ok(Token.Else)
    | OpcodeType.OP_ENDIF -> Ok(Token.EndIf)
    | OpcodeType.OP_0NOTEQUAL -> Ok(Token.ZeroNotEqual)
    | OpcodeType.OP_SIZE -> Ok(Token.Size)
    | OpcodeType.OP_SWAP -> Ok(Token.Swap)
    | OpcodeType.OP_TUCK -> Ok(Token.Tuck)
    | OpcodeType.OP_VERIFY -> Ok(Token.Verify)
    | OpcodeType.OP_HASH160 -> Ok(Token.Hash160)
    | OpcodeType.OP_SHA256 -> Ok(Token.Sha256)
    | OpcodeType.OP_ADD -> Ok(Token.Add)
    | OpcodeType.OP_0 -> Ok(Token.Number 0u)
    | OpcodeType.OP_1 -> Ok(Token.Number 1u)
    | OpcodeType.OP_2 -> Ok(Token.Number 2u)
    | OpcodeType.OP_3 -> Ok(Token.Number 3u)
    | OpcodeType.OP_4 -> Ok(Token.Number 4u)
    | OpcodeType.OP_5 -> Ok(Token.Number 5u)
    | OpcodeType.OP_6 -> Ok(Token.Number 6u)
    | OpcodeType.OP_7 -> Ok(Token.Number 7u)
    | OpcodeType.OP_8 -> Ok(Token.Number 8u)
    | OpcodeType.OP_9 -> Ok(Token.Number 9u)
    | OpcodeType.OP_10 -> Ok(Token.Number 10u)
    | OpcodeType.OP_11 -> Ok(Token.Number 11u)
    | OpcodeType.OP_12 -> Ok(Token.Number 12u)
    | OpcodeType.OP_13 -> Ok(Token.Number 13u)
    | OpcodeType.OP_14 -> Ok(Token.Number 14u)
    | OpcodeType.OP_15 -> Ok(Token.Number 15u)
    | OpcodeType.OP_16 -> Ok(Token.Number 16u)
    | otherOp when (byte 0x01) <= (byte otherOp) && (byte otherOp) < (byte 0x4B) -> 
        tryGetItemFromOp op
    | otherOp when (byte 0x4B) <= (byte otherOp) -> 
        Error(ParseException(sprintf "MiniScript does not support pushdata bigger than 33. Got %s" (otherOp.ToString())))
    | unknown ->
        Error(ParseException(sprintf "Unknown Opcode to MiniScript %s" (unknown.ToString())))

type State = {
    ops: Op[]
    position: int
}

type TokenParser = Parser<AST> 

let nextToken state =
    if state.ops.Length - 1 < state.position then
        state, None
    else
        let newState = { state with position = state.position + 1 }
        let tk = state.ops.[state.position]
        newState, Some(tk)


module TokenParser =
    let pToken (cat: TokenCategory) =
        let name = sprintf "pToken %A" cat
        let innerFn state =
            if state.position < 0 then
                Error(name, "no more input", 0)
            else
                let pos = state.position
                let ops = state.ops.[pos]
                let r = castOpToToken ops
                match r with
                | Error pex ->
                    let msg = sprintf "opcode %s is not supported by MiniScript %s" ops.Name pex.Message
                    Error(name, msg, pos)
                | Ok actualToken ->
                    let actualCat = actualToken.GetCategory()
                    if cat = Any || cat = actualCat then
                        let newState = { state with position=state.position - 1 }
                        let item = actualToken.GetItem()
                        Ok (actualToken.GetItem(), newState) 
                    else
                        let msg = sprintf "token is not the one expected \nactual: %A\nexpected: %A" actualCat cat
                        Error(name, msg, pos)
        {parseFn=innerFn; name=name}
    let mutable pENoPostProcess, pENoPostProcessImpl = createParserForwardedToRef<AST, State>()

    let mutable pW, pWImpl = createParserForwardedToRef<AST, State>()
    let mutable pE, pEImpl = createParserForwardedToRef<AST, State>()
    let mutable pV, pVImpl = createParserForwardedToRef<AST, State>()
    let mutable pQ, pQImpl = createParserForwardedToRef<AST, State>()
    let mutable pT, pTImpl = createParserForwardedToRef<AST, State>()
    let mutable pF, pFImpl = createParserForwardedToRef<AST, State>()

    // ---- common helpers ----
    let private pTime1 = (pToken EndIf)
                         >>. (pToken (Drop))
                         >>. (pToken (CheckSequenceVerify))
                         >>. (pToken Number)
                         .>> (pToken If) .>> (pToken Dup)

    // TODO: restrict to only specific number
    let private pNumberN n =
        let numberValidateParser (maybeNumberObj: obj option) =
            let name = sprintf "number validator %d" n
            let innerFn state =
                let actual = maybeNumberObj.Value :?> uint32
                if actual = n then
                    Ok(n, state)
                else
                    let msg = sprintf "failed in number validation\nexpected: %d\nactual: %d" n actual
                    Error(name, msg, state.position)

            {parseFn=innerFn;name=name}
        (pToken Number) >>= numberValidateParser

    let private multisigBind (expectedType: ASTType) (nAndPks: obj option * obj option list, maybeMObj: obj option) =
        let n = (fst nAndPks).Value :?> uint32
        let pks = (snd nAndPks)
                  |> List.rev
                  |> List.toArray
                  |> Array.map(fun pkobj -> pkobj.Value :?> PubKey)
        let m = maybeMObj.Value :?> uint32
        let name = sprintf "Parser for Multisig of type %A" expectedType
        let innerFn (state: State) =
            if pks.Length = (int n) then
                match expectedType with
                | EExpr -> Ok(ETree(E.CheckMultiSig(m, pks)), state)
                | VExpr -> Ok(VTree(V.CheckMultiSig(m, pks)), state)
                | _ -> failwith "unreachable!"
            else
                let msg = (sprintf "Invalid Multisig Script\nn was %d but actual pubkey length was %d" n pks.Length)
                Error(name, msg, state.position)

        {parseFn=innerFn; name=name}


    // ---- W ---------
    let pWCheckSig = (pToken CheckSig)
                     >>. (pToken Pk) .>> (pToken Swap)
                     |>> fun maybePKObj -> WTree(W.CheckSig (maybePKObj.Value :?> NBitcoin.PubKey))
                     <?> "Parser W.Checksig"

    let pWTime = (pTime1
                 .>> (pToken Swap)
                 |>> fun o -> WTree(W.Time(LockTime(o.Value :?> uint32))))
                 <?> "Parser W.Time"

    let pWCastE = (pToken FromAltStack)
                  >>. (pE) .>> (pToken ToAltStack)
                  |>> fun expr ->
                      WTree(W.CastE(expr.castEUnsafe()))

    let pWHashEqual = (pToken EndIf >>. pF .>> pToken If .>> pToken ZeroNotEqual .>> pToken Size .>> pToken Swap)
                      >>=(
                          fun ast ->
                              let name = "pWHashEqualValidator"
                              let innerFn state =
                                  match ast.castF() with
                                  | Ok fexpr ->
                                      match fexpr with
                                      | F.HashEqual hash ->
                                          Ok(WTree(W.HashEqual(hash)), state)
                                      | e ->
                                          let msg =  sprintf "unexpected expr\nexpected: F.HashEqual\nactual: %A" e
                                          Error(name, msg, state.position)
                                  | Error e -> failwith "unreachable"
                              {parseFn=innerFn; name=name}
                      )

    // ---- E ---------
    let pEParallelAnd = ((pToken BoolAnd)
                        >>. pW .>>. pE
                        |>> fun (astW, astE) ->
                            ETree(E.ParallelAnd(astE.castEUnsafe(), astW.castWUnsafe())))
                         <?> "Parser E.ParallelAnd"

    let pEParallelOr = ((pToken BoolOr)
                        >>. pW .>>. pE
                        |>> fun (astW, astE) ->
                            ETree(E.ParallelOr(astE.castEUnsafe(), astW.castWUnsafe())))
                         <?> "Parser E.ParallelAnd"

    let pEThreshold = (((pToken Equal) >>. (pToken Number))
                      .>>. (many1 (pToken Add >>. pW))
                      .>>. (pENoPostProcess)
                      |>> fun (kws, east) ->
                        let k = (fst kws).Value :?> uint32
                        let e = east.castEUnsafe()
                        let ws = (snd kws)
                                 |> List.toArray
                                 |> Array.rev
                                 |> Array.map(fun ast -> ast.castWUnsafe())
                        ETree(E.Threshold(k, e, ws))
                      ) <?> "Parser E.Threshold"

    let pECheckSig = (pToken CheckSig)
                     >>. (pToken Pk)
                     |>> fun maybePKObj -> ETree(E.CheckSig (maybePKObj.Value :?> NBitcoin.PubKey))
                     <?> "Parser E.Checksig"

    let pECheckMultisig = (pToken CheckMultiSig) >>. (pToken Number)
                          .>>. (many1 (pToken Pk))
                          .>>. (pToken Number)
                          >>= multisigBind EExpr

    let pETime = pWTime
                 <|> (pTime1 |>> fun maybeNumberObj -> ETree(E.Time(LockTime(maybeNumberObj.Value :?> uint32))))


    let private pLikelyPrefix = (pToken EndIf) >>. pNumberN(0u) >>. pToken Else >>. pF

    let pEUnlikely = pLikelyPrefix
                    .>> pToken If
                    |>> fun (fexpr) -> ETree(E.Unlikely(fexpr.castFUnsafe()))

    let pELikely = pLikelyPrefix
                   .>> pToken NotIf
                   |>> fun (fexpr) -> ETree(E.Likely(fexpr.castFUnsafe()))

    let pECascadeAnd = (pToken EndIf) >>. pF .>> pToken Else
                       .>>. ((pNumberN 0u) >>. (pToken NotIf) >>. pE)
                       |>> fun (rightF, leftE) ->
                           ETree(E.CascadeAnd(leftE.castEUnsafe(), rightF.castFUnsafe()))

    let pESwitchOrLeft = ((pToken EndIf) >>. pF .>> pToken Else)
                         .>>. ((pE) .>> pToken If)
                         |>> fun (rightF, leftE) ->
                             ETree(E.SwitchOrLeft(leftE.castEUnsafe(), rightF.castFUnsafe()))

    let pESwitchOrRight = (pToken EndIf >>. pF .>> pToken Else)
                          .>>. (pE .>> pToken NotIf)
                          |>> fun (rightF, leftE) ->
                              ETree(E.SwitchOrRight(leftE.castEUnsafe(), rightF.castFUnsafe()))

    // ---- V -------
    let pVDelayedOr = (((pToken CheckSigVerify)
                      >>. (pToken EndIf) >>. pQ) .>>. (pToken Else >>. pQ .>> pToken If)
                      |>> fun (q1, q2) -> 
                          VTree(V.DelayedOr(q2.castQUnsafe(), q1.castQUnsafe()))
                      ) <?> "P.VDelayedOr"

    let pVHashEqual = ((pToken EqualVerify) >>. ((pToken Sha256Hash)
                      .>> (pToken Sha256) .>> (pToken EqualVerify) .>> (pNumberN 32u) .>> (pToken Size))
                      |>> (fun maybeHashObj ->
                            let hash = maybeHashObj.Value :?> uint256
                            VTree(V.HashEqual(hash))
                        )
                      ) <?> "Parser pVHashEqual"

    let pVThreshold = ((pToken EqualVerify) >>. (pToken Number))
                      .>>. (many1 (pToken Add >>. pW))
                      .>>. (pE)
                      |>> fun (kws, east) ->
                        let k = (fst kws).Value :?> uint32
                        let e = east.castEUnsafe()
                        let ws = (snd kws)
                                 |> List.toArray
                                 |> Array.rev
                                 |> Array.map(fun ast -> ast.castWUnsafe())
                        VTree(V.Threshold(k, e, ws))

    let pVCheckSig = ((pToken CheckSigVerify)
                     >>. (pToken Pk)
                     |>> fun maybePkObj -> VTree(V.CheckSig(maybePkObj.Value :?> PubKey))
                     ) <?> "Parser pVCheckSig"

    let pVCheckMultisig = (pToken CheckMultiSigVerify)
                          >>. (pToken Number)
                          .>>. (many1 (pToken Pk))
                          .>>. (pToken Number)
                          >>= multisigBind VExpr

    let pVTime = pToken Drop >>. pToken CheckSequenceVerify >>. pToken Number
                 |>> fun maybeNumberObj ->
                     let n = maybeNumberObj.Value :?> uint32
                     VTree(V.Time(LockTime(n)))

    let pVSwitchOr = (pToken EndIf >>. pV .>> pToken Else)
                     .>>. (pV .>> pToken If)
                     |>> fun (rightV, leftV) ->
                         VTree(V.SwitchOr(leftV.castVUnsafe(), rightV.castVUnsafe()))

    let pVCascadeOr = (pToken EndIf >>. pV .>> pToken NotIf)
                      .>>. pE
                      |>> fun (rightV, leftE) ->
                          VTree(V.CascadeOr(leftE.castEUnsafe(), rightV.castVUnsafe()))

    let pVSwitchOrT = (pToken Verify >>. pToken EndIf >>. pT .>> pToken Else)
                      .>>. (pT .>> pToken If)
                      |>> fun (rightT, leftT) ->
                          VTree(V.SwitchOrT(leftT.castTUnsafe(), rightT.castTUnsafe()))

    // ---- Q -------
    let pQPubKey = ((pToken Pk)
                   |>> fun pk -> QTree(Q.Pubkey(pk.Value :?> NBitcoin.PubKey))
                   ) <?> "P.QPubKey"


    let pQOr = ((pToken EndIf) >>. pQ)
               .>>. ((pToken Else) >>. pQ .>> pToken(If))
               |>> fun (l, r) -> QTree(Q.Or(r.castQUnsafe(), l.castQUnsafe()))
    // ---- T -------

    let pTHashEqual = ((pToken Equal
                       >>. pToken Sha256Hash
                       .>> pToken Sha256
                       .>> pToken EqualVerify
                       .>> pNumberN 32u
                       .>> pToken Size)
                       |>> fun maybeHash -> TTree(T.HashEqual(maybeHash.Value :?> uint256)))
                       <?> "Parser T.HashEqual"

    let pTDelayedOr = ((pToken CheckSig) >>. (pToken EndIf)
                      >>. pQ .>>. (pToken Else >>. pQ .>> pToken If)
                      |>> fun (q1, q2) -> TTree(T.DelayedOr(q2.castQUnsafe(), q2.castQUnsafe()))
                      ) <?> "Parser T.DelayedOr"

    let pTTime = ((pToken CheckSequenceVerify) >>. (pToken Number)
                 |>> fun (maybeNumberObj) ->
                     let n = maybeNumberObj.Value :?> uint32
                     TTree(T.Time(LockTime(n)))
                  ) <?> "Parser T.Time"

    let pTSwitchOr = ((pToken EndIf >>. pT .>> pToken Else)
                     .>>. (pT .>> pToken If)
                     |>> fun (rightT, leftT) ->
                         TTree(T.SwitchOr(leftT.castTUnsafe(), rightT.castTUnsafe()))
                      ) <?> "Parser T.SwitchOr"

    let pTCascadeOr = (pToken EndIf >>. pT .>> pToken NotIf .>> pToken IfDup)
                      .>>. pE
                      |>> fun (rightT, leftE) ->
                         TTree(T.CascadeOr(leftE.castEUnsafe(), rightT.castTUnsafe()))
    // ---- F -------
    let pFTime = (pToken ZeroNotEqual)
                 >>. (pToken CheckSequenceVerify)
                 >>. (pToken Number)
                 |>> fun (maybeNumberObj) ->
                     let n = maybeNumberObj.Value :?> uint32
                     FTree(F.Time(LockTime(n)))

    let pFSwitchOr = ((pToken EndIf) >>. pF .>> pToken Else)
                     .>>. (pF .>> pToken If) 
                     |>> fun (rightF, leftF) ->
                         FTree(F.SwitchOr(leftF.castFUnsafe(), rightF.castFUnsafe()))

    let pFFromV = (pNumberN 1u >>. pV)
                  >>=(
                      fun ast ->
                          let name = "pFFromV"
                          let innerFn state =
                              match ast.castVUnsafe() with
                              | V.CheckSig pk ->
                                  Ok(FTree(F.CheckSig(pk)), state)
                              | V.CheckMultiSig (m, pks) ->
                                  Ok(FTree(F.CheckMultiSig(m, pks)), state)
                              | V.HashEqual hash ->
                                  Ok(FTree(F.HashEqual(hash)), state)
                              | V.Threshold(k, e, ws)->
                                  Ok(FTree(F.Threshold(k, e, ws)), state)
                              | V.CascadeOr(l, r)->
                                  Ok(FTree(F.CascadeOr(l, r)), state)
                              | V.SwitchOr(l, r)->
                                  Ok(FTree(F.SwitchOrV(l, r)), state)
                              | V.DelayedOr(l, r)->
                                  Ok(FTree(F.DelayedOr(l, r)), state)
                              | e ->
                                  let msg =  sprintf "unexpected expr\nactual: %A" e
                                  Error(name, msg, state.position)
                          {parseFn=innerFn; name=name}
                  )

    // ---- Composition ----
    let mutable SubExpressionParser, SubExpressionParserImpl = createParserForwardedToRef<AST, State>()
    let private shouldPostProcess(info: AST * State) =
        let ast = fst info
        let state = snd info
        if state.position = -1 then
            Ok(false)
        else
            /// If last opcode is a certain one, no need for post processing.
            let checkLastOp state =
                let lastOp = state.ops.[state.position]
                let lastToken = castOpToToken lastOp
                match lastToken with
                | Error e -> Error ("PostProcess",
                                    sprintf "Unexpected Exception in post process\nerror: %A" e,
                                    0)
                | Ok(Token.If)
                | Ok(Token.NotIf)
                | Ok(Token.Else) -> Ok(false)
                | Ok(Token.ToAltStack) -> Ok(false)
                | _ -> Ok(true)

            match ast.GetASTType() with
            | TExpr
            | VExpr
            | EExpr 
            | QExpr
            | FExpr ->
                checkLastOp state
            | _ -> Ok(false)

    let postProcess (ast: AST) =
        let name = "postProcess"
        let innerFn state =
            match shouldPostProcess(ast, state) with
            | Error e ->
                Error e
            | Ok(false) ->
                Ok((ast), state)
            | Ok(true) ->
                let rightAST = ast

                match run SubExpressionParser state with
                | Error e ->
                    Error e
                | Ok result ->
                    let leftAST, state = result
                    let leftV = leftAST.castVUnsafe()
                    match (rightAST.GetASTType()) with
                    | TExpr -> Ok(TTree(T.And(leftV, rightAST.castTUnsafe())), state)
                    | EExpr ->
                        Ok(TTree(T.And(leftV, rightAST.castTUnsafe())), state)
                    | QExpr -> Ok(QTree(Q.And(leftV, rightAST.castQUnsafe())), state)
                    | FExpr ->
                        match rightAST.castT() with
                        | Ok t -> Ok(TTree(t), state)
                        | Error _ -> Ok(FTree(F.And(leftV, rightAST.castFUnsafe())), state)
                    | VExpr -> Ok(VTree(V.And(leftV, rightAST.castVUnsafe())), state)
                    | _ -> failwith "unreachable"

        {parseFn=innerFn; name = name}

    /// validate AST is a specific type
    let pTryCastToType (expected: ASTType) (ast: AST) =
        let name = "pIsTypeOf"
        let innerFn state =
            if ast.GetASTType() = expected then
                Ok(ast, state)
            else if expected = TExpr && ast.IsT() then
                Ok(TTree(ast.castTUnsafe()), state)
            else
                let msg = sprintf "AST is not the expected type\nexpected: %A\nactual: %A" expected ast
                Error(name, msg, state.position)
        {parseFn=innerFn; name=name}

    do pENoPostProcessImpl := choice [
        pECheckSig
        pEParallelAnd
        pEParallelOr
        pEThreshold
        pECheckMultisig
        pETime
        pESwitchOrLeft
        pESwitchOrRight
        pELikely
        pEUnlikely
        pECascadeAnd
        ]

    do SubExpressionParserImpl := (choice [
                                            pWCheckSig; pWTime; pWCastE; pWHashEqual
                                            pENoPostProcess
                                            pVDelayedOr
                                            pVHashEqual
                                            pVThreshold
                                            pVCheckSig
                                            pVCheckMultisig
                                            pVTime
                                            pVSwitchOr
                                            pVCascadeOr
                                            pVSwitchOrT
                                            pQPubKey; pQOr
                                            pTHashEqual; pTDelayedOr; pTTime; pTSwitchOr; pTCascadeOr
                                            pFTime
                                            pFSwitchOr
                                            pFFromV
                                         ] >>= postProcess) <?> "SubexpressionParser"

    do pWImpl := SubExpressionParser >>= pTryCastToType WExpr
    do pEImpl := SubExpressionParser >>= pTryCastToType EExpr
    do pVImpl := SubExpressionParser >>= pTryCastToType VExpr
    do pQImpl := SubExpressionParser >>= pTryCastToType QExpr
    do pTImpl := SubExpressionParser >>= pTryCastToType TExpr
    do pFImpl := SubExpressionParser >>= pTryCastToType FExpr

let parseScript (sc: Script) =
    let ops = (sc.ToOps() |> Seq.toArray)
    let initialState = {ops=ops; position=ops.Length - 1}
    run TokenParser.SubExpressionParser initialState |> Result.map(fst)

let parseScriptUnsafe sc =
    match parseScript sc with
    | Ok r -> r
    | Error e -> failwith (printParserError e)
