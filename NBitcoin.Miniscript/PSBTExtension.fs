namespace NBitcoin
open System
open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open NBitcoin.Miniscript
open NBitcoin.Miniscript.Utils
open NBitcoin.BIP174

type PSBTFinalizationException(msg: string, ex: exn) =
    inherit System.Exception(msg, ex)
    new (msg) = PSBTFinalizationException(msg, null)

[<Extension>]
type PSBTExtension =
    static member private keyFn (psbtin: PSBTInput) (sc: Script) (pk: PubKey): TransactionSignature =
        let sigHash = if psbtin.SighashType = SigHash.Undefined then SigHash.All else psbtin.SighashType
        match psbtin.PartialSigs.TryGetValue(pk.Hash) with
        | (true, sigPair) -> TransactionSignature(snd sigPair, sigHash)
        | (false, _) -> null

    static member private getSig (partialSigs: IDictionary<_, _>) =
         try
            partialSigs.First() |> Some
         with
         | :? InvalidOperationException as e ->
            None

    static member private tryCheckWitness
        (hashFn: Func<PreImageHash, PreImage>)
        (age: uint32)
        (psbt: PSBT)
        (sigHash)
        (dummyTX: Transaction)
        (index: int)
        (prevOut: TxOut)
        (ctx: ScriptEvaluationContext)
        (isP2SH: bool)
        (spk: Script): Result<PSBT, PSBTFinalizationException> =
        let psbtin = psbt.Inputs.[index]
        if PayToWitPubKeyHashTemplate.Instance.CheckScriptPubKey(spk) then
            match PSBTExtension.getSig psbtin.PartialSigs with
            | None -> Error(PSBTFinalizationException("No signature for p2pkh"))
            | Some sigPair ->
                let txSig = TransactionSignature(snd sigPair.Value, sigHash)
                dummyTX.Inputs.[index].WitScript <- PayToWitPubKeyHashTemplate.Instance.GenerateWitScript(txSig, fst sigPair.Value)
                let ss = if isP2SH then Script(Op.GetPushOp(spk.ToBytes())) else Script.Empty
                if not (ctx.VerifyScript(ss, dummyTX, index, prevOut)) then
                    let errorMsg = sprintf "Script verification failed for p2wpkh %s" (ctx.Error.ToString())
                    Error(PSBTFinalizationException(errorMsg))
                else
                    psbt.Inputs.[index].FinalScriptSig <- ss
                    psbt.Inputs.[index].FinalScriptWitness <- dummyTX.Inputs.[index].WitScript
                    psbt.Inputs.[index].ClearForFinalize()
                    Ok(psbt)
        // p2wsh
        else if PayToWitScriptHashTemplate.Instance.CheckScriptPubKey(spk) then
            match Miniscript.fromScript psbtin.WitnessScript with
            | Error msg ->
                let errorMsg = "Failed to parse p2wsh as a Miniscript: " + msg
                Error(PSBTFinalizationException(errorMsg))
            | Ok ms ->
                match ms.Satisfy(PSBTExtension.keyFn psbtin psbtin.WitnessScript, hashFn, age) with
                | Error fCase ->
                    let msg = sprintf "Failed to satisfy p2wsh script: %A" fCase
                    Error(PSBTFinalizationException(msg))
                | Ok items ->
                    let pushes = items |> List.toArray |> Array.map(fun i -> i.ToPushOps())
                    let ss = if isP2SH then Script(Op.GetPushOp(spk.ToBytes())) else Script.Empty
                    dummyTX.Inputs.[index].WitScript <- PayToWitScriptHashTemplate.Instance.GenerateWitScript(pushes, psbtin.WitnessScript)
                    if not (ctx.VerifyScript(ss, dummyTX, index, prevOut)) then
                        let msg = sprintf "Script verification failed for following p2wsh;\nErrorCode: %s\nScript:%s\nPushItems: %A"
                                            (ctx.Error.ToString())
                                            (psbtin.RedeemScript.ToString())
                                            items
                        Error (PSBTFinalizationException(msg))
                    else
                        psbt.Inputs.[index].FinalScriptWitness <- dummyTX.Inputs.[index].WitScript
                        psbt.Inputs.[index].FinalScriptSig <- ss
                        psbt.Inputs.[index].ClearForFinalize()
                        Ok(psbt)
        else
            let msg = sprintf "Unknown type of script %s" (spk.ToString())
            Error(PSBTFinalizationException(msg))

    static member private isBareP2SH (psbtin: PSBTInput) =
        (isNull psbtin.WitnessScript) && (PayToWitTemplate.Instance.CheckScriptPubKey(psbtin.RedeemScript) |> not) 

    [<Extension>]
    static member FinalizeIndex(psbt: PSBT,
                                index: int,
                                [<Optional; DefaultParameterValue(null: Func<PreImageHash, PreImage>)>] hashFn: Func<PreImageHash, PreImage>,
                                [<Optional; DefaultParameterValue(0u)>] age: uint32) =
        let psbtin = psbt.Inputs.[index]
        let txin: TxIn = psbt.tx.Inputs.[index]
        if psbtin.IsFinalized() then
            Ok(psbt)
        else if isNull (psbtin.GetOutput(txin.PrevOut)) then
            Error(PSBTFinalizationException("Can not fiinlize PSBTInput without utxo"))
        else
            let prevOut: TxOut = psbtin.GetOutput(txin.PrevOut)
            let dummyTX = psbt.tx.Clone()
            let sigHash = if psbtin.SighashType = SigHash.Undefined then SigHash.All else psbtin.SighashType
            let mutable context = ScriptEvaluationContext()
            context.SigHash <- sigHash

            let spk = prevOut.ScriptPubKey
            let tryCheckWitness = PSBTExtension.tryCheckWitness hashFn age psbt sigHash dummyTX index prevOut context

            // p2pkh
            if (PayToPubkeyHashTemplate.Instance.CheckScriptPubKey(spk)) then
                let sigPair = psbtin.PartialSigs.First()
                match PSBTExtension.getSig psbtin.PartialSigs with
                | None -> Error(PSBTFinalizationException("No signature for p2pkh"))
                | Some sigPair ->
                    let txSig = TransactionSignature(snd sigPair.Value, sigHash)
                    let ss = PayToPubkeyHashTemplate.Instance.GenerateScriptSig(txSig, fst sigPair.Value)
                    if not (context.VerifyScript(ss, dummyTX, index, prevOut)) then
                        let errorMsg = sprintf "Script verification failed for p2pkh %s" (context.Error.ToString())
                        Error(PSBTFinalizationException(errorMsg))
                    else
                        psbtin.FinalScriptSig <- ss
                        psbt.Inputs.[index].ClearForFinalize()
                        Ok(psbt)
            // p2sh
            else if spk.IsPayToScriptHash then
                if PSBTExtension.isBareP2SH psbtin then
                    match Miniscript.fromScript psbtin.RedeemScript with
                    | Error msg ->
                        let msg = "Failed to parse p2sh as a Miniscript: " + msg
                        Error(PSBTFinalizationException(msg))
                    | Ok ms ->
                        match ms.Satisfy(PSBTExtension.keyFn psbtin psbtin.RedeemScript, hashFn, age) with
                        | Error fCase ->
                            let msg = sprintf "Failed to satisfy p2sh redeem script: %A" fCase
                            Error(PSBTFinalizationException(msg))
                        | Ok items ->
                            let pushes = items |> List.toArray |> Array.map(fun i -> i.ToPushOps())
                            let ss = PayToScriptHashTemplate.Instance.GenerateScriptSig(pushes, psbtin.RedeemScript)
                            if not (context.VerifyScript(ss, dummyTX, index, prevOut)) then
                                let msg = sprintf "Script verification failed for following p2sh;\nErrorCode: %s\nScript:%s\nPushItems: %A"
                                                  (context.Error.ToString())
                                                  (psbtin.RedeemScript.ToString())
                                                  items
                                Error (PSBTFinalizationException(msg))
                            else
                                psbtin.FinalScriptSig <- ss
                                psbt.Inputs.[index].ClearForFinalize()
                                Ok(psbt)
                else
                    // p2sh-p2wpkh, p2sh-p2wsh
                    tryCheckWitness true (psbtin.RedeemScript)
            else
                // p2wpkh, p2wsh
                tryCheckWitness false (spk)


    [<Extension>]
    static member FinalizeIndexUnsafe(psbt: PSBT,
                                      index: int,
                                      [<Optional; DefaultParameterValue(null: Func<PreImageHash, PreImage>)>] hashFn: Func<PreImageHash, PreImage>,
                                      [<Optional; DefaultParameterValue(0u)>] age: uint32) =

        match psbt.FinalizeIndex(index, hashFn, age) with
        | Ok psbt -> psbt
        | Error e -> raise e

    // Finalize all inputs.
    [<Extension>]
    static member Finalize(psbt: PSBT,
                           [<Optional; DefaultParameterValue(null: Func<PreImageHash, PreImage>)>] hashFn: Func<PreImageHash, PreImage>,
                           [<Optional; DefaultParameterValue(0u)>] age: uint32) =
        let inline resultFolder (acc) (r): Result<PSBT, _> =
            match acc, r with
            | Error e1 , Error e2 -> Error(e1 @ e2)
            | Error e, Ok _ -> Error e
            | Ok _, Error e -> Error e
            | Ok _, Ok psbt2 -> Ok psbt2

        let r = seq { 0 .. psbt.Inputs.Count - 1 }
                |> Seq.map(fun i -> psbt.FinalizeIndex(i, hashFn, age))
                |> Seq.map(Result.mapError(fun e -> [e]))
                |> Seq.reduce resultFolder
                |> Result.mapError(fun es -> AggregateException(es |> List.map(fun e -> e :> exn)))
        r
    [<Extension>]
    static member FinalizeUnsafe(psbt: PSBT,
                                 [<Optional; DefaultParameterValue(null: Func<PreImageHash, PreImage>)>] hashFn: Func<PreImageHash, PreImage>,
                                 [<Optional; DefaultParameterValue(0u)>] age: uint32) =
        match psbt.Finalize(hashFn, age) with
        | Ok psbt -> psbt
        | Error e -> raise e