# NBitcoin.Tests

You can group the unit tests per "Trait" in the Test Explorer.
Here are the trait NBitcoin is using :

"Core" means I ported it directly from the bitcoind source, they have no dependency.
"UnitTest" means the test is self contained without dependency, but not ported from Bitcoin Core.
"Benchmark" are simple method you can use or modify to measure performance of NBitcoin, they have some file system dependencies.
"PropertyTest" are Property based test. i.e. It generates some test data automatically and test against all those data.
For PropertyTest, we split tests into following categories.
(These are an exactly same with the one described in ref: https://fsharpforfunandprofit.com/posts/property-based-testing-2/)

1. Commutativity
2. BidirectionalConversion
3. Immutability
4. Idempotence
5. Induction
6. Verification
7. Oracle
