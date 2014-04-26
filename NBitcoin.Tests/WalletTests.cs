using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class WalletTests
	{
		[Fact]
		public void CanManageMoney()
		{
			WalletTester tester = new WalletTester();

			//Change C,A,U
			tester.GiveMoney("1.0", BlockType.Main);
			tester.AssertPools(null,
				"+1.00",
				"+1.00",
				"+1.00");

			//Change U
			var rcv = tester.GiveMoney("0.1", null);
			tester.AssertPools(null,
				"+1.00",
				"+1.00",
				"+1.00+0.10");

			//Change U
			tester.GiveMoney("0.2", BlockType.Side);
			tester.AssertPools(null,
				"+1.00",
				"+1.00",
				"+1.00+0.10+0.20");

			//Change C,A
			tester.RecieveTransaction(rcv, BlockType.Main);
			tester.AssertPools(null,
				"+1.00+0.10",
				"+1.00+0.10",
				"+1.00+0.10+0.20");

			//Change A,U
			var pay = tester.Pay("0.1", null);
			tester.AssertPools(null,
				"+1.00+0.10",
				"+1.00+0.10-0.10",
				"+1.00+0.10+0.20-0.10");

			//No change
			tester.RecieveTransaction(pay, BlockType.Side);
			tester.AssertPools(null,
				"+1.00+0.10",
				"+1.00+0.10-0.10",
				"+1.00+0.10+0.20-0.10");

			//Chance C
			tester.RecieveTransaction(pay, BlockType.Main);
			tester.AssertPools(null,
				"+1.00+0.10-0.10",
				"+1.00+0.10-0.10",
				"+1.00+0.10+0.20-0.10");

			//No change
			tester.RecieveTransaction(pay, BlockType.Main);
			tester.AssertPools(null,
				"+1.00+0.10-0.10",
				"+1.00+0.10-0.10",
				"+1.00+0.10+0.20-0.10");
		}

		[Fact]
		public void CanChangeBlockChain()
		{
			WalletTester tester = new WalletTester();

			var c0 = tester.StartRecordChain();
			var c1 = tester.StartRecordChain();
			//Change C,A,U
			tester.GiveMoney("1.0", BlockType.Main);
			tester.GiveMoney("2.0", BlockType.Main);

			var c2 = tester.StartRecordChain();
			tester.GiveMoney("3.0", BlockType.Main);
			tester.StopRecordChain(c1);
			tester.GiveMoney("4.0", BlockType.Main);

			tester.AssertPools(
				null,
				"+1.00+2.00+3.00+4.00",
				"+1.00+2.00+3.00+4.00",
				"+1.00+2.00+3.00+4.00");

			//4.0 canceled
			tester.AssertPools(
			c1,
			"+1.00+2.00+3.00+4.00-4.00",
			"+1.00+2.00+3.00+4.00-4.00",
			"+1.00+2.00+3.00+4.00");


			//4.0 re introduced, 1.0, 2.0 canceled
			tester.AssertPools(
			c2,
			"+1.00+2.00+3.00+4.00-4.00-1.00-2.00+4.00",
			"+1.00+2.00+3.00+4.00-4.00-1.00-2.00+4.00",
			"+1.00+2.00+3.00+4.00");


			//1.0, 2.0 re introduced
			tester.AssertPools(
			c0,
			"+1.00+2.00+3.00+4.00-4.00-1.00-2.00+4.00+1.00+2.00",
			"+1.00+2.00+3.00+4.00-4.00-1.00-2.00+4.00+1.00+2.00",
			"+1.00+2.00+3.00+4.00");


			//4.0 canceled
			tester.AssertPools(
			c1,
			"+1.00+2.00+3.00+4.00-4.00-1.00-2.00+4.00+1.00+2.00-4.00",
			"+1.00+2.00+3.00+4.00-4.00-1.00-2.00+4.00+1.00+2.00-4.00",
			"+1.00+2.00+3.00+4.00");


			//4.0 re added
			tester.AssertPools(
			c0,
			"+1.00+2.00+3.00+4.00-4.00-1.00-2.00+4.00+1.00+2.00-4.00+4.00",
			"+1.00+2.00+3.00+4.00-4.00-1.00-2.00+4.00+1.00+2.00-4.00+4.00",
			"+1.00+2.00+3.00+4.00");
		}


	}
}