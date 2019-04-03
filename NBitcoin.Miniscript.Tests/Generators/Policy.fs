namespace NBitcoin.Miniscript.Tests.Generators

module internal Policy =
    open FsCheck
    open NBitcoin.Miniscript.MiniscriptParser
    open NBitcoin.Miniscript.Tests.Generators.NBitcoin
    
    let multiContentsGen = gen { let! n = Gen.choose (1, 20) |> Gen.map uint32
                                 let! subN = Gen.choose ((int n), 20)
                                 let! subs = Gen.arrayOfLength subN pubKeyGen
                                 return (n, subs) }
    
    let nonRecursivePolicyGen : Gen<Policy> =
        Gen.frequency [ (2, Gen.map Key pubKeyGen)
                        
                        (1, 
                         Gen.map (fun (num, pks) -> Multi(num, pks)) 
                             multiContentsGen)
                        (2, Gen.map Hash uint256Gen)
                        (2, Arb.generate<uint32> |> Gen.map NBitcoin.LockTime |> Gen.map(Time)) ]
    
    let policy =
        let rec policy' s =
            match s with
            | 0 -> nonRecursivePolicyGen
            | n when n > 0 -> 
                let subPolicyGen = policy' (n / 2)
                Gen.frequency [ (2, nonRecursivePolicyGen)
                                (3, recursivePolicyGen subPolicyGen) ]
            | _ -> invalidArg "s" "Only positive arguments are allowed!"
        
        and recursivePolicyGen (subPolicyGen : Gen<Policy>) =
            Gen.oneof 
                [ Gen.map (fun (t, ps) -> Threshold(t, ps)) 
                      (thresholdContentsGen subPolicyGen)
                  
                  Gen.map2 (fun subP1 subP2 -> And(subP1, subP2)) subPolicyGen 
                      subPolicyGen
                  
                  Gen.map2 (fun subP1 subP2 -> Or(subP1, subP2)) subPolicyGen 
                      subPolicyGen
                  
                  Gen.map2 (fun subP1 subP2 -> AsymmetricOr(subP1, subP2)) 
                      subPolicyGen subPolicyGen ]
        
        and thresholdContentsGen (subGen : Gen<_>) = gen { let! n = Gen.choose 
                                                                        (1, 6) 
                                                                    |> Gen.map 
                                                                           uint32
                                                           let! subN = Gen.choose 
                                                                           ((int 
                                                                                 n), 
                                                                            6)
                                                           let! subs = Gen.arrayOfLength 
                                                                           subN 
                                                                           subGen
                                                           return (n, subs) }
        Gen.sized policy'
