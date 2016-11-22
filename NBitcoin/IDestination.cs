namespace nStratis
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
}
