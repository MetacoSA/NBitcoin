namespace NBitcoin.Miniscript.Utils

[<AutoOpen>]
module Utils =
    let inline (!>) (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit : ^a -> ^b )x )

    let resultFolder (acc : Result<'a seq, 'c>) 
        (item : Result<'a, 'c>) =
        match acc, item with
        | Ok x, Ok y -> 
            Ok(seq { 
                   yield! x
                   yield y
               })
        | Ok x, Error y -> Error y
        | Error x, _ -> Error x


    [<RequireQualifiedAccess>]
    module List =
        let rec traverseResult f list =
            let (>>=) x f = Result.bind f x
            let retn = Ok
            let cons head tail = head :: tail

            let initState = retn []
            let folder head tail =
                f head >>= (fun h ->
                    tail >>= (fun t ->
                        retn (cons h t)
                    )
                )
            List.foldBack folder list initState
        let sequenceResult list = traverseResult id list