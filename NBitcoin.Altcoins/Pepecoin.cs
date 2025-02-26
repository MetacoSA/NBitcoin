using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Reflection;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/pepecoinppc/pepecoin/blob/4fb5a0cd930c0df82c88292e973a7b7cfa06c4e8/src/chainparams.cpp
	public class Pepecoin : NetworkSetBase
	{
		public static Pepecoin Instance { get; } = new Pepecoin();

		public override string CryptoCode => "PEPE";

		private Pepecoin()
		{

		}
		public class PepeConsensusFactory : ConsensusFactory
		{
			private PepeConsensusFactory()
			{
			}
			public static PepeConsensusFactory Instance { get; } = new PepeConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new PepecoinBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new PepecoinBlock(new PepecoinBlockHeader());
			}
			public override Transaction CreateTransaction()
			{
				return new PepeTransaction();
			}
			public override TxOut CreateTxOut()
			{
				return new PepeTxOut();
			}
			protected override TransactionBuilder CreateTransactionBuilderCore(Network network)
			{
				// https://github.com/pepecoinppc/pepecoin/blob/4fb5a0cd930c0df82c88292e973a7b7cfa06c4e8/doc/fee-recommendation.md
				var txBuilder = base.CreateTransactionBuilderCore(network);
				txBuilder.StandardTransactionPolicy.MinRelayTxFee = new FeeRate(Money.Coins(0.001m), 1000);
				// Around 3000 USD of fee for a transaction at ~0.4 USD per pepe
				txBuilder.StandardTransactionPolicy.MaxTxFee = new FeeRate(Money.Coins(56m), 1);
				return txBuilder;
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class AuxPow : IBitcoinSerializable
		{
			Transaction tx = new Transaction();

			public Transaction Transactions
			{
				get
				{
					return tx;
				}
				set
				{
					tx = value;
				}
			}

			uint nIndex = 0;

			public uint Index
			{
				get
				{
					return nIndex;
				}
				set
				{
					nIndex = value;
				}
			}

			uint256 hashBlock = new uint256();

			public uint256 HashBlock
			{
				get
				{
					return hashBlock;
				}
				set
				{
					hashBlock = value;
				}
			}

			List<uint256> vMerkelBranch = new List<uint256>();

			public List<uint256> MerkelBranch
			{
				get
				{
					return vMerkelBranch;
				}
				set
				{
					vMerkelBranch = value;
				}
			}

			List<uint256> vChainMerkleBranch = new List<uint256>();

			public List<uint256> ChainMerkleBranch
			{
				get
				{
					return vChainMerkleBranch;
				}
				set
				{
					vChainMerkleBranch = value;
				}
			}

			uint nChainIndex = 0;

			public uint ChainIndex
			{
				get
				{
					return nChainIndex;
				}
				set
				{
					nChainIndex = value;
				}
			}

			BlockHeader parentBlock = new BlockHeader();

			public BlockHeader ParentBlock
			{
				get
				{
					return parentBlock;
				}
				set
				{
					parentBlock = value;
				}
			}

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWrite(ref tx);
				stream.ReadWrite(ref hashBlock);
				stream.ReadWrite(ref vMerkelBranch);
				stream.ReadWrite(ref nIndex);
				stream.ReadWrite(ref vChainMerkleBranch);
				stream.ReadWrite(ref nChainIndex);
				stream.ReadWrite(ref parentBlock);
			}
		}
		public class PepeTransaction : Transaction
		{
			public override ConsensusFactory GetConsensusFactory()
			{
				return Pepecoin.PepeConsensusFactory.Instance;
			}
		}
		public class PepeTxOut : TxOut
		{
			public override Money GetDustThreshold()
			{
				// https://github.com/pepecoinppc/pepecoin/blob/4fb5a0cd930c0df82c88292e973a7b7cfa06c4e8/doc/fee-recommendation.md
				return Money.Coins(0.01m);
			}
			public override ConsensusFactory GetConsensusFactory()
			{
				return Pepecoin.PepeConsensusFactory.Instance;
			}
		}
		public class PepecoinBlock : Block
		{
			public PepecoinBlock(PepecoinBlockHeader header) : base(header)
			{

			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return PepeConsensusFactory.Instance;
			}
		}
		public class PepecoinBlockHeader : BlockHeader
		{
			const int VERSION_AUXPOW = (1 << 8);

			AuxPow auxPow = new AuxPow();

			public AuxPow AuxPow
			{
				get
				{
					return auxPow;
				}
				set
				{
					auxPow = value;
				}
			}

			public override uint256 GetPoWHash()
			{
				var headerBytes = this.ToBytes();
				var h = NBitcoin.Crypto.SCrypt.ComputeDerivedKey(headerBytes, headerBytes, 1024, 1, 1, null, 32);
				return new uint256(h);
			}

			public override void ReadWrite(BitcoinStream stream)
			{
				base.ReadWrite(stream);
				if ((Version & VERSION_AUXPOW) != 0)
				{
					if (!stream.Serializing)
					{
						stream.ReadWrite(ref auxPow);
					}
				}
			}
		}

		public class PepecoinTestnetAddressStringParser : NetworkStringParser
		{
			public override bool TryParse(string str, Network network, Type targetType, out IBitcoinString result)
			{
				if (str.StartsWith("tgpv", StringComparison.OrdinalIgnoreCase) && targetType.GetTypeInfo().IsAssignableFrom(typeof(BitcoinExtKey).GetTypeInfo()))
				{
					try
					{
						var decoded = Encoders.Base58Check.DecodeData(str);
						decoded[0] = 0x04;
						decoded[1] = 0x35;
						decoded[2] = 0x83;
						decoded[3] = 0x94;
						result = new BitcoinExtKey(Encoders.Base58Check.EncodeData(decoded), network);
						return true;
					}
					catch
					{
					}
				}
				if (str.StartsWith("tgub", StringComparison.OrdinalIgnoreCase) && targetType.GetTypeInfo().IsAssignableFrom(typeof(BitcoinExtPubKey).GetTypeInfo()))
				{
					try
					{
						var decoded = Encoders.Base58Check.DecodeData(str);
						decoded[0] = 0x04;
						decoded[1] = 0x35;
						decoded[2] = 0x87;
						decoded[3] = 0xCF;
						result = new BitcoinExtPubKey(Encoders.Base58Check.EncodeData(decoded), network);
						return true;
					}
					catch
					{
					}
				}
				return base.TryParse(str, network, targetType, out result);
			}
		}

#pragma warning restore CS0618 // Type or member is obsolete

		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		//static Tuple<byte[], int>[] pnSeed6_main = null;
		//static Tuple<byte[], int>[] pnSeed6_test = null;
		// Not used in DOGE: https://github.com/dogecoin/dogecoin/blob/10a5e93a055ab5f239c5447a5fe05283af09e293/src/chainparams.cpp#L135

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 100000,
				MajorityEnforceBlockUpgrade = 1500,
				MajorityRejectBlockOutdated = 1900,
				MajorityWindow = 2000,
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 30,
				//  Not set in reference client, assuming false
				PowNoRetargeting = false,
				//RuleChangeActivationThreshold = 6048,
				//MinerConfirmationWindow = 8064,
				ConsensusFactory = PepeConsensusFactory.Instance,
				LitecoinWorkCalculation = true,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 56 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 22 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 158 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x02, 0xFA, 0xCA, 0xFD })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x02, 0xFA, 0xC3, 0x98 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("pepe"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("pepe"))
			.SetMagic(0xe0f0a0c0)
			.SetPort(33874)
			.SetRPCPort(33873)
			.SetName("pepe-main")
			.AddAlias("pepe-mainnet")
			.AddAlias("pepecoin-mainnet")
			.AddAlias("pepecoin-main")
			.SetUriScheme("pepecoin")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("pepecoin.org", "seeds.pepecoin.org"),
				new DNSSeedData("pepeblocks.com", "seeds.pepeblocks.com"),
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000001265bca4002feac94c0c06971f12aa8b2c82fb3e93244690d5cb399aa51b2ad2a01daf65f0ff0f1eb48506000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5104ffff001d01044957534a20312f32322f3234202d204665642052657669657720436c656172732043656e7472616c2042616e6b204f6666696369616c73206f662056696f6c6174696e672052756c6573ffffffff010058850c0200000043410436d04f40a76a1094ea10b14a513b62bfd0b47472dda1c25aa9cf8266e53f3c4353680146177f8a3b328ed2c6e02f2b8e051d9d5ffc61a4e6ccabd03409109a5aac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 100000,
				MajorityEnforceBlockUpgrade = 501,
				MajorityRejectBlockOutdated = 750,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				// pre-post-digishield https://github.com/dogecoin/dogecoin/blob/10a5e93a055ab5f239c5447a5fe05283af09e293/src/chainparams.cpp#L45
				PowTargetTimespan = TimeSpan.FromSeconds(60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 30,
				//  Not set in reference client, assuming false
				PowNoRetargeting = false,
				//RuleChangeActivationThreshold = 6048,
				//MinerConfirmationWindow = 8064,
				LitecoinWorkCalculation = true,
				ConsensusFactory = PepeConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 113 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 241 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetNetworkStringParser(new PepecoinTestnetAddressStringParser())
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tpepe"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tpepe"))
			.SetMagic(0xccdbc1fe)
			.SetPort(44874)
			.SetRPCPort(44873)
		   .SetName("pepe-test")
		   .AddAlias("pepe-testnet")
		   .AddAlias("pepecoin-test")
		   .AddAlias("pepecoin-testnet")
		   .SetUriScheme("pepecoin")
		   .AddDNSSeeds(new[]
		   {
			   new DNSSeedData("pepecoin.org", "seeds-testnet.pepecoin.org"),
			   new DNSSeedData("pepeblocks.com", "seeds-testnet.pepeblocks.com"),
		   })
		   .AddSeeds(new NetworkAddress[0])
		   .SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000001265bca4002feac94c0c06971f12aa8b2c82fb3e93244690d5cb399aa51b2ad2dc1daf65f0ff0f1e940d00000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5104ffff001d01044957534a20312f32322f3234202d204665642052657669657720436c656172732043656e7472616c2042616e6b204f6666696369616c73206f662056696f6c6174696e672052756c6573ffffffff010058850c0200000043410436d04f40a76a1094ea10b14a513b62bfd0b47472dda1c25aa9cf8266e53f3c4353680146177f8a3b328ed2c6e02f2b8e051d9d5ffc61a4e6ccabd03409109a5aac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(4 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 60,
				//  Not set in reference client, assuming false
				PowNoRetargeting = false,
				//RuleChangeActivationThreshold = 6048,
				//MinerConfirmationWindow = 8064,
				LitecoinWorkCalculation = true,
				ConsensusFactory = PepeConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tpepe"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tpepe"))
			.SetMagic(0xdab5bffa)
			.SetPort(18444)
			.SetRPCPort(18332) // by default this is assigned dynamically, adding port I got for testing
			.SetName("pepe-reg")
			.AddAlias("pepe-regtest")
			.AddAlias("pepecoin-regtest")
			.AddAlias("pepecoin-reg")
			.SetUriScheme("pepecoin")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000001265bca4002feac94c0c06971f12aa8b2c82fb3e93244690d5cb399aa51b2ad2dae5494dffff7f20000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff5104ffff001d01044957534a20312f32322f3234202d204665642052657669657720436c656172732043656e7472616c2042616e6b204f6666696369616c73206f662056696f6c6174696e672052756c6573ffffffff010058850c0200000043410436d04f40a76a1094ea10b14a513b62bfd0b47472dda1c25aa9cf8266e53f3c4353680146177f8a3b328ed2c6e02f2b8e051d9d5ffc61a4e6ccabd03409109a5aac00000000");
			return builder;
		}

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("Pepecoin");
		}

	}
}
