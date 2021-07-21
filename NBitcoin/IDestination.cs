namespace NBitcoin
{
	/// <summary>
	/// Represent any type which represent an underlying ScriptPubKey
	/// </summary>
	public interface IDestination
	{
		Script ScriptPubKey
		{
			get;
		}
	}

	/// <summary>
	/// Represent any type which represent an underlying ScriptPubKey which can be represented as an address
	/// </summary>
	public interface IAddressableDestination : IDestination
	{
		bool IsSupported(Network network);
		BitcoinAddress GetAddress(Network network);
	}
}
