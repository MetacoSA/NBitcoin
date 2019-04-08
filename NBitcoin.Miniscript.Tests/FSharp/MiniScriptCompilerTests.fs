module MiniScriptCompilerTests

open Expecto
open Expecto.Logging
open Expecto.Logging.Message
open NBitcoin.Miniscript.Tests.Generators
open NBitcoin.Miniscript
open NBitcoin.Miniscript.Compiler
open NBitcoin.Miniscript.MiniscriptParser

let logger = Log.create "MiniscriptCompiler"

let config =
    { FsCheckConfig.defaultConfig with arbitrary = [ typeof<Generators> ]
                                       maxTest = 500
                                       endSize = 32
                                       receivedArgs =
                                           fun _ name no args -> 
                                               logger.debugWithBP 
                                                   (eventX 
                                                        "For {test} {no}, generated {args}"
                                                    >> setField "test" name
                                                    >> setField "no" no
                                                    >> setField "args" args) }

[<Tests>]
let tests =
    testList "miniscript compiler" [ testPropertyWithConfig config 
                                         "should compile arbitrary input" <| fun (p : AbstractPolicy) -> 
                                             let node = CompiledNode.fromPolicy (p)
                                             let t = node.Compile()
                                             Expect.isOk (t.CastT())

                                     testPropertyWithConfig config
                                        "Should compile arbitrary input to actual bitcoin script" <| fun (p: AbstractPolicy) ->
                                             let m = CompiledNode.fromPolicy(p).Compile()
                                             Expect.isNotNull (m.ToScript()) "script was empty"
                                         ]
