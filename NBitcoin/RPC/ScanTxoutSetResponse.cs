using System;
using System.Linq;
using System.Collections.Generic;

namespace NBitcoin.RPC
{
	public class ScanTxoutSetResponse
	{
		public int SearchedItems { get; internal set; }
		public bool Success { get; internal set; }
		public ScanTxoutOutput[] Outputs { get; set; }
		public Money TotalAmount { get; set; }
	}


	public class ScanTxoutSetParameters
	{
		public ScanTxoutSetParameters()
		{

		}
		public ScanTxoutSetParameters(string descriptor, int? begin = null, int? end = null)
		{
			Descriptors = new[] { new ScanTxoutDescriptor(descriptor) { Begin = begin, End = end } };
		}
		public ScanTxoutSetParameters(BitcoinAddress address, int? begin = null, int? end = null)
		: this(ScanTxoutDescriptor.ToDescriptor(address), begin, end)
		{
		}
		public ScanTxoutSetParameters(Script script, int? begin = null, int? end = null)
			: this(ScanTxoutDescriptor.ToDescriptor(script), begin, end)
		{
		}



		public ScanTxoutSetParameters(IEnumerable<string> descriptors, int? begin = null, int? end = null)
		{
			Descriptors = descriptors.Select(descriptor => new ScanTxoutDescriptor(descriptor) { Begin = begin, End = end } ).ToArray();
		}
		public ScanTxoutSetParameters(ScanTxoutDescriptor[] descriptors)
		{
			Descriptors = descriptors;
		}
		public ScanTxoutDescriptor[] Descriptors { get; set; }
	}
	public class ScanTxoutDescriptor
	{
		internal static string ToDescriptor(BitcoinAddress address)
		=> address is null ? throw new ArgumentNullException(nameof(address)) : $"addr({address})";
		internal static string ToDescriptor(Script script)
			=> script is null ? throw new ArgumentNullException(nameof(script)) : $"raw({script})";
		public ScanTxoutDescriptor(string descriptor)
		{
			Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
		}

		public ScanTxoutDescriptor(BitcoinAddress address) : this(ToDescriptor(address))
		{
		}
		public ScanTxoutDescriptor(Script script) : this(ToDescriptor(script))
		{
		}

		public ScanTxoutDescriptor()
		{

		}
		public string Descriptor { get; set; }
		/// <summary>
		/// The range of HD chain indexes to explore
		/// </summary>
		public int? Begin { get; set; }
		/// <summary>
		/// The range of HD chain indexes to explore
		/// </summary>
		public int? End { get; set; }
	}
	public class ScanTxoutOutput
	{
		public Coin Coin { get; set; }
		public int Height { get; set; }
	}
}
