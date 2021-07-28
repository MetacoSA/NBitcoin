using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using NBitcoin.Scripting;

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
		public ScanTxoutSetParameters(OutputDescriptor descriptor, int? begin = null, int? end = null)
		{
			Descriptors = new ScanTxoutDescriptor[1] { new ScanTxoutDescriptor(descriptor) { Begin = begin, End = end } };
		}
		public ScanTxoutSetParameters(IEnumerable<OutputDescriptor> descriptors, int? begin = null, int? end = null)
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
		public ScanTxoutDescriptor(OutputDescriptor desc)
		{
			Descriptor = desc;
		}
		public ScanTxoutDescriptor()
		{

		}
		public OutputDescriptor Descriptor { get; set; }
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
