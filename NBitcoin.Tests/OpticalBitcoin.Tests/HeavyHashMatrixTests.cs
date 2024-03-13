using Xunit;
using NBitcoin.Altcoins;

namespace NBitcoin.Tests
{
    [Trait("Altcoins", "Altcoins")]
    public class HeavyHashMatrixTests
    {
        private static string failureMsgBase = "[FAILED]: HeavyHashMatrixTests. ";

        [Fact]
        public void TestMatrixRank()
        {
            string failureMsg = failureMsgBase + "Matrix rank cehcking.";

            Assert.True(
                HeavyHashMatrix.IsFullRankGaus(
                    new ulong[,]{{1, 2, 3}, {4, 5, 6}, {1, 15, 0}}, 3
                ),
                "(1)" + failureMsg
            );

            Assert.False(
                HeavyHashMatrix.IsFullRankGaus(
                    new ulong[,]{{0, 0}, {0, 0}}, 2
                ),
                "(2)" + failureMsg
            );

            Assert.False(
                HeavyHashMatrix.IsFullRankGaus(
                    new ulong[,]{{1, 2}, {4, 8}}, 2
                ),
                "(3)" + failureMsg
            );

            Assert.True(
                HeavyHashMatrix.IsFullRankGaus(
                    HeavyHashRef.Matrix, 64
                ),
                "(3)" + failureMsg
            );
        }
    }
}
