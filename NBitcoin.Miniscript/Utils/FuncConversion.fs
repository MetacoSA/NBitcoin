namespace NBitcoin.Miniscript.Utils
open System
open System.Runtime.CompilerServices

[<Extension>]
module FuncExtension =
  type public CSharpFun =
    static member internal ToFSharpFunc<'a> (action: Action<'a>) = fun a -> action.Invoke(a)
