using NBitcoin.Crypto;

namespace NBitcoin.Altcoins
{
    public class HeavyHashStream : HashStream
    {
        private BlockHeader blockHeader;
        private HeavyHashMatrix heavyHashMatrix;
        private HeavyHash heavyHash = new HeavyHash();
        
        public HeavyHashStream(BlockHeader blockHeader) => InitHasher(blockHeader);
        
        private void InitHasher(BlockHeader blockHeader) 
        {
            this.blockHeader = blockHeader;
            
            byte[] seedBytes = heavyHash.GetSha3(blockHeader.HashPrevBlock.ToBytes());
            uint256 seed = new uint256(seedBytes);

            heavyHashMatrix = new HeavyHashMatrix(seed);
        }

        public override uint256 GetHash()
        {
            	ulong[,] inputMatrix = heavyHashMatrix.Body;

				byte[] outputBytes = heavyHash.GetHash(blockHeader.ToBytes(), inputMatrix);

				return new uint256(outputBytes);
        }
    }
}