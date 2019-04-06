namespace NBitcoin.Miniscript

open System.Collections.Generic
open System.Runtime.InteropServices

open NBitcoin.Miniscript.AST
open NBitcoin.Miniscript.Decompiler
open NBitcoin.Miniscript.Compiler
open NBitcoin.Miniscript.Satisfy
open NBitcoin

/// wrapper for top-level AST

module public Miniscript =
    type Miniscript = private Miniscript of T

    let internal fromAST (t : AST) : Result<Miniscript, string> =
        match t.CastT() with
        | Ok t -> Ok(Miniscript(t))
        | o -> Error (sprintf "AST was not top-level (T) representation\n%A" o)

    let internal fromASTUnsafe(t: AST) =
        match fromAST t with
        | Ok t -> t
        | Error e -> failwith e

    [<CompiledName("Parse")>]
    let public parse (s: string) =
        match s with
        | AbstractPolicy p -> (CompiledNode.FromPolicy p).Compile() |> fromAST
        | _ -> Error("failed to parse String policy")

    [<CompiledName("ParseUnsafe")>]
    let public parseUnsafe (s: string) =
        match parse s with
        | Ok m -> m
        | Error e -> failwith e

    let internal toAST (m : Miniscript) =
        match m with
        | Miniscript a -> TTree(a)

    [<CompiledName("FromScriptUnsafe")>]
    let public fromScriptUnsafe (s : NBitcoin.Script) =
        let res = parseScriptUnsafe s
        match fromAST res with
        | Ok r -> r
        | Error e -> failwith e

    let public toScript (m : Miniscript) : Script =
        let ast = toAST m
        ast.ToScript()

    let public Satisfy (Miniscript t) (providers: ProviderSet) =
        satisfyT (providers) t

    let private dictToFn (d: IDictionary<_ ,_>) k =
        match d.TryGetValue k with
        | (true, v) -> Some v
        | (false, _) -> None

    type Miniscript with
        member this.ToScript() = toScript this
        member internal this.ToAST() = toAST this
        /// Facade for F#
        member this.Satisfy(?keyFn: SignatureProvider,
                            ?hashFn: PreImageProvider,
                            ?age: LockTime) =
                                Satisfy this (keyFn, hashFn, age)

        /// Facade for C#
        member this.Satisfy([<Optional; DefaultParameterValue(null: IDictionary<PubKey, TransactionSignature>)>] sigDict: IDictionary<PubKey, TransactionSignature>,
                            [<Optional; DefaultParameterValue(null: IDictionary<uint256, uint256>)>] hashDict: IDictionary<uint256, uint256>,
                            [<Optional; DefaultParameterValue(0u)>] age: uint32) =
                                let keyFn = if sigDict = null then None else Some(dictToFn sigDict)
                                let hashFn = if hashDict = null then None else Some(dictToFn hashDict)
                                let age2 = if age = 0u then None else Some(LockTime(age))
                                this.Satisfy(?keyFn=keyFn, ?hashFn=hashFn, ?age=age2)