namespace NBitcoin.Miniscript

open System
open System.Collections.Generic
open System.Runtime.InteropServices

open NBitcoin.Miniscript.AST
open NBitcoin.Miniscript.Decompiler
open NBitcoin.Miniscript.Compiler
open NBitcoin.Miniscript.Satisfy
open NBitcoin.Miniscript.MiniscriptParser
open NBitcoin

/// Exception types to enable consumer to use try-catch style handling instead of `Result<T>`
/// Why we define it here instead of putting it into `Utis` ?
/// Because a code for core logics should never throw `Exception` and instead use Result,
/// And we must basically restrict public-facing interfaces to this file.
type MiniscriptException(msg: string, ex: exn) =
    inherit Exception(msg, ex)
    new (msg) = MiniscriptException(msg, null)

type MiniscriptSatisfyException(reason: FailureCase, ex: exn) =
    inherit MiniscriptException(sprintf "Failed to satisfy, got: %A" reason, ex)
    new (reason) = MiniscriptSatisfyException(reason, null)

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

    [<CompiledName("FromPolicy")>]
    let public fromPolicy(p: AbstractPolicy) =
            (CompiledNode.FromPolicy p).Compile() |> fromAST

    [<CompiledName("FromPolicyUnsafe")>]
    let public fromPolicyUnsafe(p: AbstractPolicy) =
        match fromPolicy p with
        | Ok p -> p
        | Error e -> failwith e

    [<CompiledName("FromString")>]
    let public fromString (s: string) =
        match s with
        | AbstractPolicy p -> fromPolicy p
        | _ -> Error("failed to parse String policy")

    [<CompiledName("FromStringUnsafe")>]
    let public fromStringUnsafe (s: string) =
        match fromString s with
        | Ok m -> m
        | Error e -> failwith e


    let internal toAST (m : Miniscript) =
        match m with
        | Miniscript a -> TTree(a)

    [<CompiledName("FromScript")>]
    let public fromScript (s : NBitcoin.Script) =
        parseScript s |> Result.mapError(fun e -> e.ToString()) >>= fromAST

    [<CompiledName("FromScriptUnsafe")>]
    let public fromScriptUnsafe (s : NBitcoin.Script) =
        match fromScript s with
        | Ok res -> res
        | Error e -> failwith e

    let private toScript (m : Miniscript) : Script =
        let ast = toAST m
        ast.ToScript()

    [<CompiledName("Satisfy")>]
    let public satisfy (Miniscript t) (providers: ProviderSet) =
        satisfyT (providers) t

    let private dictToFn (d: IDictionary<_ ,_>) k =
        match d.TryGetValue k with
        | (true, v) -> Some v
        | (false, _) -> None

    let private toFSharpFunc<'TIn, 'TOut> (f: Func<'TIn, 'TOut>) =
        fun input ->
            let v = f.Invoke(input)
            if isNull (box v) then None else Some v
    type Miniscript with
        member this.ToScript() = toScript this
        member internal this.ToAST() = toAST this
        /// Facade for F#
        member this.Satisfy(?keyFn: SignatureProvider,
                            ?hashFn: PreImageProvider,
                            ?age: LockTime) =
                                satisfy this (keyFn, hashFn, age)
        member this.SatisfyUnsafe(?keyFn: SignatureProvider,
                                  ?hashFn: PreImageProvider,
                                  ?age: LockTime) =
                                match satisfy this (keyFn, hashFn, age) with
                                | Ok item -> item
                                | Error e -> raise (MiniscriptSatisfyException(e))

        /// Facade for C#
        member this.SatisfyUnsafe([<Optional; DefaultParameterValue(null: Func<PubKey, TransactionSignature>)>] keyFn: Func<PubKey, TransactionSignature>,
                                  [<Optional; DefaultParameterValue(null: Func<PreImageHash, PreImage>)>] hashFn: Func<PreImageHash, PreImage>,
                                  [<Optional; DefaultParameterValue(0u)>] age: uint32) =
                                let maybeFsharpKeyFn = if isNull keyFn then None else Some(toFSharpFunc(keyFn))
                                let maybeFsharpHashFn = if isNull hashFn then None else Some(toFSharpFunc(hashFn))
                                let maybeAge = if age = 0u then None else Some(LockTime(age))
                                this.SatisfyUnsafe(?keyFn=maybeFsharpKeyFn, ?hashFn=maybeFsharpHashFn, ?age=maybeAge)

        member this.Satisfy([<Optional; DefaultParameterValue(null: Func<PubKey, TransactionSignature>)>] keyFn: Func<PubKey, TransactionSignature>,
                            [<Optional; DefaultParameterValue(null: Func<PreImageHash, PreImage>)>] hashFn: Func<PreImageHash, PreImage>,
                            [<Optional; DefaultParameterValue(0u)>] age: uint32) =
                                let maybeFsharpKeyFn = if isNull keyFn then None else Some(toFSharpFunc(keyFn))
                                let maybeFsharpHashFn = if isNull hashFn then None else Some(toFSharpFunc(hashFn))
                                let maybeAge = if age = 0u then None else Some(LockTime(age))
                                this.Satisfy(?keyFn=maybeFsharpKeyFn, ?hashFn=maybeFsharpHashFn, ?age=maybeAge)
