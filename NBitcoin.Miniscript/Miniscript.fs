namespace NBitcoin.Miniscript

open NBitcoin.Miniscript.AST
open NBitcoin.Miniscript.Decompiler
open NBitcoin.Miniscript.Compiler
open NBitcoin.Miniscript.Satisfy
open NBitcoin

/// wrapper for top-level AST
type Miniscript = Miniscript of T

[<AutoOpen>]
module Miniscript =
    let fromAST (t : AST) : Result<Miniscript, string> =
        match t.CastT() with
        | Ok t -> Ok(Miniscript(t))
        | o -> Error (sprintf "AST was not top-level (T) representation\n%A" o)

    let fromASTUnsafe(t: AST) =
        match fromAST t with
        | Ok t -> t
        | Error e -> failwith e

    let parse (s: string) =
        match s with
        | AbstractPolicy p -> (CompiledNode.FromPolicy p).Compile() |> fromAST
        | _ -> Error("failed to parse String policy")

    let parseUnsafe (s: string) =
        match parse s with
        | Ok m -> m
        | Error e -> failwith e

    let toAST (m : Miniscript) =
        match m with
        | Miniscript a -> TTree(a)

    let fromScriptUnsafe (s : NBitcoin.Script) =
        let res = parseScriptUnsafe s
        match fromAST res with
        | Ok r -> r
        | Error e -> failwith e

    let toScript (m : Miniscript) : Script =
        let ast = toAST m
        ast.ToScript()

    let satisfy (Miniscript t) (providers: ProviderSet) =
        satisfyT (providers) t

type Miniscript with
    member this.ToScript() = Miniscript.toScript this
    member this.ToAST() = Miniscript.toAST this
    member this.SatisFy(providers) = Miniscript.satisfy this providers
