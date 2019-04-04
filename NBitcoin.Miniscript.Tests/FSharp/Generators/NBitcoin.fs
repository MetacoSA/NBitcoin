namespace NBitcoin.Miniscript.Tests.Generators

module internal NBitcoin =
    open FsCheck
    open NBitcoin.Miniscript.Tests.Generators.Primitives
    
    let pubKeyGen =
        let k = NBitcoin.Key() // prioritize speed for randomness
        Gen.constant (k) |> Gen.map (fun k -> k.PubKey)
    
    let uint256Gen = bytesOfNGen 32 |> Gen.map NBitcoin.uint256
