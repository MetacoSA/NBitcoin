module FNBitcoin.MiniScriptParser

open NBitcoin
open System.Text.RegularExpressions
open System

type Policy =
    | Key of PubKey
    | Multi of uint32 * PubKey []
    | Hash of uint256
    | Time of NBitcoin.LockTime
    | Threshold of uint32 * Policy []
    | And of Policy * Policy
    | Or of Policy * Policy
    | AsymmetricOr of Policy * Policy
    override this.ToString() =
        match this with
        | Key k1 -> sprintf "pk(%s)" (string (k1.ToHex()))
        | Multi(m, klist) -> 
            klist
            |> Seq.map (fun k -> string (k.ToHex()))
            |> Seq.reduce (fun a b -> sprintf "%s,%s" a b)
            |> sprintf "multi(%d,%s)" m
        | Hash h -> sprintf "hash(%s)" (string (h.ToString()))
        | Time t -> sprintf "time(%s)" (string (t.ToString()))
        | Threshold(m, plist) -> 
            plist
            |> Array.map (fun p -> p.ToString())
            |> Array.reduce (fun a b -> sprintf "%s,%s" a b)
            |> sprintf "thres(%d,%s)" m
        | And(p1, p2) -> sprintf "and(%s,%s)" (p1.ToString()) (p2.ToString())
        | Or(p1, p2) -> sprintf "or(%s,%s)" (p1.ToString()) (p2.ToString())
        | AsymmetricOr(p1, p2) -> 
            sprintf "aor(%s,%s)" (p1.ToString()) (p2.ToString())


// parser
let quoted = Regex(@"\((.*)\)")

let rec (|SurroundedByBrackets|_|) (s : string) =
    let s2 = s.Trim()
    if s2.StartsWith("(") && s2.EndsWith(")") then 
        Some(s2.TrimStart('(').TrimEnd(')'))
    else None

let (|Expression|_|) (prefix : string) (s : string) =
    let s = s.Trim()
    if s.StartsWith(prefix) then Some(s.Substring(prefix.Length))
    else None

let (|PubKeyPattern|_|) (s : string) =
    try 
        Some(PubKey(s))
    with :? FormatException as ex -> None

let (|PubKeysPattern|_|) (s : string) =
    let s = s.Trim().Split(',')
    match UInt32.TryParse(s.[0]) with
    | (false, _) -> None
    | (true, i) -> 
        try 
            let pks =
                s.[1..s.Length - 1] |> Array.map (fun hex -> PubKey(hex.Trim()))
            Some(i, pks)
        with :? FormatException -> None

let (|Hash|_|) (s : string) =
    try 
        Some(uint256 (s.Trim()))
    with :? FormatException -> None

let (|Time|_|) (s : string) =
    try 
        Some(uint32 (s.Trim()))
    with :? FormatException -> None

// Split with "," but only when not surroounded by parenthesis
let rec safeSplit (s : string) (acc : string list) (index : int) (openNum : int) 
        (currentChunk : char []) =
    if s.Length = index then 
        let lastChunk = String.Concat(Array.append currentChunk [| ')' |])
        let lastAcc = List.append acc [ lastChunk ]
        lastAcc |> List.toArray
    else 
        let c = s.[index]
        if c = '(' then 
            let newChunk = Array.append currentChunk [| c |]
            safeSplit s acc (index + 1) (openNum + 1) newChunk
        elif c = ')' then 
            let newChunk = Array.append currentChunk [| c |]
            safeSplit s acc (index + 1) (openNum - 1) newChunk
        elif openNum = 0 && (c = ',') then 
            let newElement = String.Concat(currentChunk)
            let newAcc = List.append acc [ newElement ]
            safeSplit s newAcc (index + 1) (openNum) [||]
        else 
            let newChunk = Array.append currentChunk [| c |]
            safeSplit s acc (index + 1) (openNum) newChunk

let rec (|Policy|_|) s =
    let s = Regex.Replace(s, @"[|\s|\n|\r\n]+", "")
    match s with
    | Expression "pk" (SurroundedByBrackets(PubKeyPattern pk)) -> Some(Key pk)
    | Expression "multi" (SurroundedByBrackets(PubKeysPattern pks)) -> 
        Multi((fst pks), (snd pks)) |> Some
    | Expression "hash" (SurroundedByBrackets(Hash hash)) -> Some(Hash hash)
    | Expression "time" (SurroundedByBrackets(Time t)) -> Some(Time(LockTime(t)))
    // recursive matches
    | Expression "thres" (SurroundedByBrackets(Threshold thres)) -> 
        Some(Threshold(thres))
    | Expression "and" (SurroundedByBrackets(And(expr1, expr2))) -> 
        And(expr1, expr2) |> Some
    | Expression "or" (SurroundedByBrackets(Or(expr1, expr2))) -> 
        Or(expr1, expr2) |> Some
    | Expression "aor" (SurroundedByBrackets(AsymmetricOr(expr1, expr2))) -> 
        AsymmetricOr(expr1, expr2) |> Some
    | _ -> None

and (|Threshold|_|) (s : string) =
    let s = safeSplit s [] 0 0 [||]
    let thresholdStr = s.[0]
    match UInt32.TryParse(thresholdStr) with
    | (true, threshold) -> 
        let subPolicy = s.[1..s.Length - 1] |> Array.choose ((|Policy|_|))
        if subPolicy.Length <> s.Length - 1 then None
        else Some(threshold, subPolicy)
    | (false, _) -> None

and (|And|_|) (s : string) = twoSubExpressions s

and (|Or|_|) (s : string) = twoSubExpressions s

and (|AsymmetricOr|_|) (s : string) = twoSubExpressions s

and twoSubExpressions (s : string) =
    let s = safeSplit s [] 0 0 [||]
    if s.Length <> 2 then None
    else 
        let subPolicies = s |> Array.choose ((|Policy|_|))
        if subPolicies.Length <> s.Length then None
        else Some(subPolicies.[0], subPolicies.[1])
