using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NBitcoin.Altcoins.Elements
{
	class ElementsRegtest : NetworkSetBase
	{
		public static ElementsRegtest Instance { get; } = new ElementsRegtest();

		public override string CryptoCode => "ELEM";

		public class ElementsStringParser : NetworkStringParser
		{
			public override bool TryParse<T>(string str, Network network, out T result)
			{
				if (str.StartsWith("CT", StringComparison.OrdinalIgnoreCase) 
									&& (typeof(BitcoinAddress).GetTypeInfo().IsAssignableFrom(typeof(T).GetTypeInfo())))
				{
					try
					{
						result = (T)(object)new BitcoinBlindedAddress(str, network);
						return true;
					}
					catch
					{
					}
				}
				return base.TryParse(str, network, out result);
			}
		}

		public class ElementsConsensusFactory : ConsensusFactory
		{
			public static ElementsConsensusFactory Instance { get; } = new ElementsConsensusFactory();

			public override bool TryCreateNew(Type type, out IBitcoinSerializable result)
			{
				if (typeof(TxIn).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
				{
					result = new ElementsTxIn(this);
					return true;
				}
				if (typeof(TxOut).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
				{
					result = new ElementsTxOut();
					return true;
				}
				return base.TryCreateNew(type, out result);
			}
			public override BlockHeader CreateBlockHeader()
			{
				return new ElementsBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new ElementsBlock(new ElementsBlockHeader(), this);
			}

			public override Transaction CreateTransaction()
			{
				return new ElementsTransaction() { ElementsConsensusFactory = this };
			}
		}

		protected override NetworkBuilder CreateMainnet()
		{
			return null;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			return null;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 144,
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(3.5 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = true,
				MinimumChainWork = uint256.Zero,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 2016,
				CoinbaseMaturity = 100,
				LitecoinWorkCalculation = true,
				ConsensusFactory = ElementsConsensusFactory.Instance
			})
			.SetNetworkStringParser(new ElementsStringParser())
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { (235) })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { (75) })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { (239) })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { (0x04), (0x35), (0x87), (0xCF) })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { (0x04), (0x35), (0x83), (0x94) })
			.SetMagic(0xdab5bffa)
			.SetPort(19444)
			.SetRPCPort(19332)
			.SetName("elem-reg")
			.AddAlias("elem-regtest")
			.AddAlias("elements-reg")
			.AddAlias("elements-regtest")
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000a1115ef053ab32201a3656f0298d74026689c6de13953126fb4b6c7c9fe17296dae5494d00000000015100010100000000010000000000000000000000000000000000000000000000000000000000000000ffffffff212078fdfddeafc3bac34abe63efee0d64f7d817cee508ded08746ba4ae6df5349cbffffffff0101000000000000000000000000000000000000000000000000000000000000000001000000000000000000016a00000000");
			return builder;
		}

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Bitcoin", new FolderName() { RegtestFolder = "elementsregtest" });
		}
	}
}
