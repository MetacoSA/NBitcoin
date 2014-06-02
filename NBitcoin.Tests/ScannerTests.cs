using NBitcoin.Scanning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class ScannerTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanScanPubKeyHash()
		{
			var dest = new Key().PubKey.ID;
			var dest2 = new Key().PubKey.ID;
			Transaction tx = new Transaction();
			tx.Outputs.Add(new TxOut(Money.Parse("1.5"), dest));
			tx.Outputs.Add(new TxOut(Money.Parse("2.0"), dest2));
			PubKeyHashScanner scanner = new PubKeyHashScanner(dest);
			var coins = scanner.ScanCoins(tx, 0);
			Assert.NotNull(coins);
			Assert.Equal(Money.Parse("1.5"), coins.Value);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanScanScriptHash()
		{
			var dest = new Script(OpcodeType.OP_RETURN).ID;
			var dest2 = new Key().PubKey.ID;
			Transaction tx = new Transaction();
			tx.Outputs.Add(new TxOut(Money.Parse("1.5"), dest));
			tx.Outputs.Add(new TxOut(Money.Parse("2.0"), dest2));
			var scanner = new ScriptHashScanner(dest);
			var coins = scanner.ScanCoins(tx, 0);
			Assert.NotNull(coins);
			Assert.Equal(Money.Parse("1.5"), coins.Value);
		}

		
	}
}
