namespace FNBitcoin.Utils

module Parser =
    type ErrorMessage = string
    type ParserName = string
    type Position = int

    type ParserError = ParserName * ErrorMessage * Position

    let printParserError (pe: ParserError) =
        let (name, msg, pos) = pe
        sprintf "name: %s\nmsg: %s\nposition %d" name msg pos

    type ParserResult<'a> = Result<'a, ParserError>

    type Parser<'a, 'u> = {
        parseFn: 'u -> ParserResult<'a * 'u>
        name: ParserName
    }
    type Parser<'a> = Parser<'a, unit>


    // combinators for parser. 1: Monad law
    let bindP f p = 
        let innerFn input =
            let r1 = p.parseFn input
            match r1 with
            | Ok(item, remainingInput) ->
                let p2 = f item
                p2.parseFn remainingInput
            | Error e -> Error e
        {parseFn=innerFn; name="unknown"}

    let (>>=) p f = bindP f p

    let returnP x =
        let name = sprintf "%A" x
        let innerFn input =
            Ok (x, input)
        {parseFn=innerFn; name=name}

    // 2: Functor law
    let mapP f =
        bindP (f >> returnP)

    let (<!>) = mapP
    let (|>>) x f = mapP f x

    // 3: Applicatives 
    let applyP fP xP =
        fP >>= (fun f ->
        xP >>= (fun x -> returnP (f x)))

    let (<*>) = applyP
    let lift2 f xP yP =
        returnP f <*> xP <*> yP


    // 4: parser specific things
    /// get the label from a parser
    let getName (parser) = 
        // get label
        parser.name

    /// update the label in the parser
    let setName parser newName = 
        // change the inner function to use the new label
        let newInnerFn input = 
            let result = parser.parseFn input
            match result with
            | Error (oldLabel,err,pos) -> 
                // if Failure, return new label
                Error (newName,err,pos) 
            | ok -> ok
        // return the Parser
        {parseFn=newInnerFn; name=newName}

    /// infix version of setLabel
    let ( <?> ) = setName

    let andThen (p1) (p2) =
        let l = sprintf "%s andThen %s" (getName p1) (getName p2)
        p1 >>= (fun p1R ->
        p2 >>= (fun p2R ->
            returnP (p1R, p2R)
            )) <?> l

    let (.>>.) = andThen

    let run p input =
        p.parseFn input

    let orElse p1 p2 =
        let name = sprintf "%s orElse %s" (getName p1) (getName p2)
        let innerFn input =
            let r1 = p1.parseFn input
            match r1 with
            | Ok _ -> r1
            | Error e ->
                let r2 = p2.parseFn input
                r2
        {parseFn=innerFn; name=name}

    let (<|>) = orElse

    let choice listOfParsers =
        List.reduce (<|>) listOfParsers

    let rec sequence parserlist =
        let cons head tail = head::tail
        let consP = lift2 cons
        match parserlist with
        | [] -> returnP []
        | head::tail ->
            consP head (sequence tail)

    // parse zero or more occurancs o the specified parser
    let rec star p input =
        let firstResult = p.parseFn input
        match firstResult with
        | Error (_, _, _) -> ([], input)
        | Ok (firstValue, inputAfterFirstPlace) ->
            let (subsequenceValues, remainingInput) =
                star p inputAfterFirstPlace
            let values = firstValue::subsequenceValues
            (values, remainingInput)

    // zero or more occurances
    let many p =
        let name = sprintf "many %s" (getName p)
        let rec innerFn input =
            Ok(star p input)
        {parseFn=innerFn; name=name}

    // one or more
    let many1 p =
        let name = sprintf "many1 %s" (getName p)
        p >>= (fun head -> 
        many p >>= (fun tail ->
            returnP (head::tail)
        )) <?> name

    let opt p =
        let name = sprintf "opt %s" (getName p)
        let some = p |>> Some
        let none = returnP None
        (some <|> none) <?> name

    let (.>>) p1 p2 =
        p1 .>>. p2
        |> mapP (fun (a, b) -> a)

    let (>>.) p1 p2 =
        p1 .>>. p2
        |> mapP (fun (a, b) -> b)

    let createParserForwardedToRef<'a, 'u>() =
        let dummyParser =
            let innerFn input : ParserResult<'a * 'u> = failwith "unfixed forwarded parser"
            {parseFn=innerFn; name="unknown"}
        let parserRef = ref dummyParser
        let innerFn input =
            (!parserRef).parseFn input
        let wrapperParser = {parseFn=innerFn; name="unknown"}
        wrapperParser, parserRef

    