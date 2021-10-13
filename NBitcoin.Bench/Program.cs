using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace NBitcoin.Bench
{
	public class Program
	{
		public static void Main(string[] args)
		{
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

			//var s = new PSBTSigningBench();
			//s.Setup();
			//s.BenchSignPSBT();

			//var s = new Serialization();
			//s.Setup();
			//s.DeserializeBigBlock();

			//var s = new GolombRiceFilters();
			//s.Setup();
			//s.Build();
		}
	}
}
