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

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/terracoin/terracoin/blob/master/src/chainparams.cpp
	public class Terracoin : NetworkSetBase
	{
		public static Terracoin Instance { get; } = new Terracoin();

		public override string CryptoCode => "TRC";

		private Terracoin()
		{

		}
		//Format visual studio
		//{({.*?}), (.*?)}
		//Tuple.Create(new byte[]$1, $2)
		//static Tuple<byte[], int>[] pnSeed6_main = null;
		//static Tuple<byte[], int>[] pnSeed6_test = null;

#pragma warning disable CS0618 // Type or member is obsolete
		public class TerracoinConsensusFactory : ConsensusFactory
		{
			private TerracoinConsensusFactory()
			{
			}

			public static TerracoinConsensusFactory Instance { get; } = new TerracoinConsensusFactory();

			public override ProtocolCapabilities GetProtocolCapabilities(uint protocolVersion)
			{
				var capabilities = base.GetProtocolCapabilities(protocolVersion);
				capabilities.SupportWitness = false;
				return capabilities;
			}
			public override BlockHeader CreateBlockHeader()
			{
				return new PureBlockHeader();
			}
			public override Block CreateBlock()
			{
				return new TerracoinBlock(new PureBlockHeader());
			}
			protected override TransactionBuilder CreateTransactionBuilderCore(Network network)
			{
				var txBuilder = base.CreateTransactionBuilderCore(network);
				txBuilder.StandardTransactionPolicy.MinFee = Money.Coins(0.0001m);
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
				//stream.TransactionOptions = TransactionOptions.Witness;

				stream.ReadWrite(ref tx);
				stream.ReadWrite(ref hashBlock);
				stream.ReadWrite(ref vMerkelBranch);
				stream.ReadWrite(ref nIndex);
				stream.ReadWrite(ref vChainMerkleBranch);
				stream.ReadWrite(ref nChainIndex);
				stream.ReadWrite(ref parentBlock);
			}
		}

		public class TerracoinBlock : Block
		{
			public TerracoinBlock(PureBlockHeader h) : base(h)
			{

			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return TerracoinConsensusFactory.Instance;
			}
		}

		public class PureBlockHeader : BlockHeader
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

			public bool IsAuxpow()
			{
				return ((Version & VERSION_AUXPOW) != 0);
			}

			public override uint256 GetPoWHash()
			{
				if(IsAuxpow())
				{
					// It's AuxPow we assume PoW is correct
					return uint256.Zero;
				}
				else
				{
					return base.GetPoWHash();
				}
			}

			public override void ReadWrite(BitcoinStream stream)
			{
				//stream.TransactionOptions = TransactionOptions.None;

				base.ReadWrite(stream);
				if(IsAuxpow() && !stream.Serializing)
				{
					stream.ReadWrite(ref auxPow);
				}
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("TerracoinCore");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 1050000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("0x0000000000006a908847f2d6b7ac98e8ac9ce54c544aca63c66473e637f4741e"),
				PowLimit = new Target(new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x00000000000000000000000000000000000000000013c31ec956b02ded0e535c"),
				PowTargetTimespan = TimeSpan.FromSeconds(60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 90,
				MinerConfirmationWindow = 30,
				ConsensusFactory = TerracoinConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 5 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 128 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("trc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("trc"))
			.SetMagic(0x56BEBA42)
			.SetPort(13333)
			.SetRPCPort(13332)
			.SetMaxP2PVersion(70208)
			.SetName("trc-main")
			.AddAlias("trc-mainnet")
			.AddAlias("terracoin-main")
			.AddAlias("terracoin-mainnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("terracoin.io", "seed.terracoin.io"),
				new DNSSeedData("southofheaven.ca", "dnsseed.southofheaven.ca")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000096f12836f9a4d8029fea2c89ad06be01a9aaa6f3c3160c5867b00338f9098b0fbb538a50ffff001d2a841ba80101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4204ffff001d01043a4a756e65203474682031393738202d204d61726368203674682032303039203b205265737420496e2050656163652c205374657068616e69652effffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 1050000,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				BIP34Hash = new uint256("0x57e446ce39f87a0949e7400db06b1e2e1680fe4bc4621db0af04b5ecabb92abd"),
				PowLimit = new Target(new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000000001a9c85200164b"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = TerracoinConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("ttrc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("ttrc"))
			.SetMagic(0x0709110B)
			.SetPort(18321)
			.SetRPCPort(18322)
			.SetMaxP2PVersion(70208)
			.SetName("trc-test")
			.AddAlias("trc-testnet")
			.AddAlias("terracoin-test")
			.AddAlias("terracoin-testnet")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("terracoin.io",  "testnetseed.terracoin.io")
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000096f12836f9a4d8029fea2c89ad06be01a9aaa6f3c3160c5867b00338f9098b0f332f9359ffff001db80aa3cf0101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4204ffff001d01043a4a756e65203474682031393738202d204d61726368203674682032303039203b205265737420496e2050656163652c205374657068616e69652effffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			NetworkBuilder builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256(),
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = uint256.Zero,
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(2 * 60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				ConsensusFactory = TerracoinConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("ttrc"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("ttrc"))
			.SetMagic(0xDAB5BFFA)
			.SetPort(18444)
			.SetRPCPort(18332)
			.SetMaxP2PVersion(70208)
			.SetName("trc-reg")
			.AddAlias("trc-regtest")
			.AddAlias("terracoin-reg")
			.AddAlias("terracoin-regtest")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000096f12836f9a4d8029fea2c89ad06be01a9aaa6f3c3160c5867b00338f9098b0fdae5494dffff7f20020000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff4204ffff001d01043a4a756e65203474682031393738202d204d61726368203674682032303039203b205265737420496e2050656163652c205374657068616e69652effffffff0100f2052a01000000434104678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5fac00000000");
			return builder;
		}
	}
}
