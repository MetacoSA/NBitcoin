using Xunit;
using System;
using System.Linq;
using NBitcoin.Altcoins;

namespace NBitcoin.Tests
{
    [Trait("Altcoins", "Obtc")]
    public class HeavyHashCryptoTests
    {
        private static string failureMsgBase = "[FAILED]: HeavyHashCryptoTests. ";
        public static byte[] GetBytesFromString(string bytesString, bool fLittleEndian){
			byte[] bytes = bytesString.Split('-').Select(b => Convert.ToByte(b, 16)).ToArray();
			if (fLittleEndian){
				Array.Reverse(bytes);
			}
			return bytes;
		}

        [Fact]
        public void TestHeavyHashWithRefMatrix()
        {
            HeavyHash hasher = new HeavyHash();

        	byte[] input = GetBytesFromString("C1-EC-FD-FC", false);
            byte[] expectedOutput = GetBytesFromString(
                "39-38-7F-2E-64-E7-C0-8D-3C-E0-DA-8C-49-1B-4F-CF-2C-86-27-98-DE-DB-46-90-D8-19-DE-79-26-AA-4E-CB", 
                false
            );

            Assert.True(
                expectedOutput.SequenceEqual(hasher.GetHash(input, HeavyHashRef.Matrix)), 
                failureMsgBase + "HeavyHash with reference matrix."
            );
        }

        [Fact]
        public void TestFullHeavyHash()
        {
            HeavyHash hasher = new HeavyHash();
            // Using 1st block serialized version
			byte[] input = GetBytesFromString(
                "01-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-f5-fe-bb-ad-19-86-4a-69-00-b6-ce-84-28-75-11-e6-e7-46-22-9c-e6-23-9f-f3-df-74-81-65-47-78-a4-c4-d3-e1-5d-60-ff-ff-00-1c-07-47-d0-42", 
                false
            );
            // Using genesis block as a parent
            byte[] parentBlockHash = GetBytesFromString(
                "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00",
                false
            );
            // Using 1st block hash as a reference
			byte[] expectedOutput = GetBytesFromString(
                "00-00-00-00-00-11-5C-7A-7E-3F-F6-5D-77-EE-96-DE-52-79-53-CA-6E-43-E7-72-46-92-97-41-40-8F-95-C0", 
                true
            );
			
            uint256 seed = new uint256(hasher.GetSha3(parentBlockHash));
			HeavyHashMatrix heavyHashMatrix = new HeavyHashMatrix(seed);
            byte[] output = hasher.GetHash(input, heavyHashMatrix.Body);

            Assert.True(
                output.SequenceEqual(expectedOutput), 
                failureMsgBase + "Full HeavyHash."
            );
        }
    }
}