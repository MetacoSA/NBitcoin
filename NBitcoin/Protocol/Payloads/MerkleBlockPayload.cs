namespace nStratis.Protocol.Payloads
{
	/// <summary>
	/// A merkle block received after being asked with a getdata message
	/// </summary>
	[Payload("merkleblock")]
	public class MerkleBlockPayload : BitcoinSerializablePayload<MerkleBlock>
	{
		public MerkleBlockPayload()
		{

		}
		public MerkleBlockPayload(MerkleBlock block)
			: base(block)
		{

		}
	}
}
