using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Bench
{
	[MemoryDiagnoser]
	public class BitcoinStringParsing
	{
		[Benchmark]
		public void DeserializeBase58()
		{
			BitcoinAddress.Create("3J98t1WpEZ73CNmQviecrnyiWrnqRhWNLy", Network.Main);
		}
		[Benchmark]
		public void DeserializeBech()
		{
			BitcoinAddress.Create("bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdq", Network.Main);
		}
		[Benchmark]
		public void DeserializeXPub()
		{
			new BitcoinExtPubKey("tpubDDtSBgfc8peQPRWaSUfPC6k3QosE6QWv1P3ZBXbmCBQehxd4KdZLpsLJGe4qML2AcgbxZNHdi87929AXeFD2tENmLZD2DWFPGXBDcQzeQ3d", Network.TestNet);
		}
	}
}
