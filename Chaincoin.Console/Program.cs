using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin;
using NBitcoin.Altcoins;

namespace Chaincoin.Console
{
	class Program
	{
		static void Main(string[] args)
		{
			Network network = NBitcoin.Altcoins.Chaincoin.Instance.Mainnet;
			System.Console.WriteLine(new Key().PubKey.GetAddress(network));
			System.Console.ReadKey();

		}
	}
}
