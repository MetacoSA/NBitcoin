#nullable enable

namespace NBitcoin.RPC
{
	public class CreateWalletOptions
	{
		public bool? DisablePrivateKeys { get; set; } 
		public bool? Blank { get; set; } 
		public string? Passphrase { get; set; } 
		public bool? AvoidReuse { get; set; }
		public bool? Descriptors { get; set; }
		public bool? LoadOnStartup { get; set; }
		// A decred wallet cannot be created from a node and should be
		// already started and synced.
		public int? Port { get; set; }
	}
}

#nullable restore
