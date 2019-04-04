namespace NBitcoin.Miniscript.Tests.Generators

module internal Primitives =
    open FsCheck
    
    let byteGen = Gen.choose (0, 127) |> Gen.map byte
    let bytesGen = Gen.listOf byteGen
    let nonEmptyBytesGen = Gen.nonEmptyListOf byteGen
    let bytesOfNGen n = Gen.arrayOfLength n byteGen
