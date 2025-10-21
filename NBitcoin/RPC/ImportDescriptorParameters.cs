using System;
using Newtonsoft.Json;

namespace NBitcoin.RPC;

public class BlockchainTimestamp
{
	public class Now : BlockchainTimestamp;

	public class Date(DateTimeOffset timestamp) : BlockchainTimestamp
	{
		public DateTimeOffset Timestamp { get; } = timestamp;
	}
}

public class DescriptorRange(long? begin, long end)
{
	public DescriptorRange(long end) : this(null, end)
	{
	}
	public long? Begin { get; set; } = begin;
	public long End { get; set; } = end;
}

public class ImportDescriptorParameters(string desc)
{
	/// <summary>
	/// Descriptor to import
	/// </summary>
	public string Desc { get; set; } = desc;

	/// <summary>
	/// Set this descriptor to be the active descriptor for the corresponding output type/externality
	/// </summary>
	public bool? Active { get; set; }

	/// <summary>
	/// If a ranged descriptor is used, this specifies the end or the range (in the form [begin,end]) to import
	/// </summary>
	public DescriptorRange Range { get; set; }

	/// <summary>
	/// If a ranged descriptor is set to active, this specifies the next index to generate addresses from
	/// </summary>
	public int? NextIndex { get; set; }

	/// <summary>
	/// Time from which to start rescanning the blockchain for this descriptor, in UNIX epoch time
	/// Use the string "now" to substitute the current synced blockchain time.
	/// </summary>
	public BlockchainTimestamp Timestamp { get; set; } = new BlockchainTimestamp.Now();

	/// <summary>
	/// Whether matching outputs should be treated as not incoming payments (e.g. change)
	/// </summary>
	public bool? Internal { get; set; }

	/// <summary>
	/// Label to assign to the address, only allowed with internal=false
	/// </summary>
	public string Label { get; set; }
}

public class ImportDescriptorResult
{
	/// <summary>
	/// Indicates if the import was successful
	/// </summary>
	[JsonProperty("success")]
	public bool Success { get; set; }

	/// <summary>
	/// Optional warnings that occurred during import
	/// </summary>
	[JsonProperty("warnings")]
	public string[] Warnings { get; set; }

	[JsonProperty("error")]
	[JsonConverter(typeof(JsonConverters.RPCErrorJsonConverter))]
	public RPCError Error { get; set; }
}
