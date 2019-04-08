namespace NBitcoin.Miniscript
open System
open System.Runtime.CompilerServices
open NBitcoin.BIP174

[<Extension>]
type PSBTExtension =
  [<Extension>]
  static member Finalize(psbt: PSBT) =
    ()