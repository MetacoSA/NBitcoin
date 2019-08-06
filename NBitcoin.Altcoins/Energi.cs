using System;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin.Altcoins
{
	public class Energi : NetworkSetBase
	{
		private Energi()
		{
		}

		public class EnergiConsensusFactory : ConsensusFactory
		{
			private EnergiConsensusFactory()
			{
			}

			public static EnergiConsensusFactory Instance { get; } = new EnergiConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new EnergiBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new EnergiBlock(new EnergiBlockHeader());
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class EnergiBlockHeader : BlockHeader
		{
			const uint _posBit = 0x10000000U;

			protected uint nHeight;

			public uint Height
			{
				get
				{
					return nHeight;
				}
				set
				{
					nHeight = value;
				}
			}

			protected uint256 hashMix;

			public uint256 HashMix
			{
				get
				{
					return hashMix;
				}
				set
				{
					hashMix = value;
				}
			}

			protected ulong newNonce;
			public ulong NewNonce
			{
				get
				{
					return newNonce;
				}
				set
				{
					newNonce = value;
				}
			}

			protected uint256 posStakeHash;
			public uint256 PosStakeHash
			{
				get
				{
					return posStakeHash;
				}
				set
				{
					posStakeHash = value;
				}
			}

			protected uint posStakeN;
			public uint PosStakeN
			{
				get
				{
					return posStakeN;
				}
				set
				{
					posStakeN = value;
				}
			}

			protected byte[] posBlockSig = Array.Empty<byte>();
			public byte[] PosBlockSig
			{
				get
				{
					return posBlockSig;
				}
				set
				{
					posBlockSig = value;
				}
			}

			protected Script posPubKey = Script.Empty;
			public Script PosPubKey
			{
				get
				{
					return posPubKey;
				}
				set
				{
					posPubKey = value;
				}
			}

			public override void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWrite(ref nVersion);
				stream.ReadWrite(ref hashPrevBlock);
				stream.ReadWrite(ref hashMerkleRoot);
				stream.ReadWrite(ref nTime);
				stream.ReadWrite(ref nBits);
				stream.ReadWrite(ref nHeight);
				stream.ReadWrite(ref hashMix);
				stream.ReadWrite(ref newNonce);

				if (IsProofOfStake())
				{
					stream.ReadWrite(ref posStakeHash);
					stream.ReadWrite(ref posStakeN);

					if (stream.Type == SerializationType.Hash)
					{
						stream.ReadWrite(ref posBlockSig);
					}

					if (!stream.Serializing)
					{
						stream.ReadWrite(posPubKey);
					}
				}
			}

			private bool IsProofOfStake()
			{
				return (nVersion & _posBit) != 0;
			}
		}

		public class EnergiBlock : Block
		{
			public EnergiBlock(EnergiBlockHeader h)
				: base(h)
			{		

			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return Instance.Mainnet.Consensus.ConsensusFactory;
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		public static Energi Instance { get; } = new Energi();

		public override string CryptoCode => "NRG";

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210240,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000a16073f8a2399fb3"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = EnergiConsensusFactory.Instance,
				SupportSegwit = false,
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 33 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 53 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 106 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x03, 0xB8, 0xC8, 0x56 })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0xD7, 0xDC, 0x6E, 0x9F })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("nrg"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("nrg"))
			.SetMagic(0xAF9A2DEC)
			.SetPort(9797)
			.SetRPCPort(9796)
			.SetMaxP2PVersion(70212)
			.SetName("nrg-main")
			.AddAlias("nrg-mainnet")
			.AddAlias("energi-mainnet")
			.AddAlias("energi-main")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("energi.network", "dnsseed.energi.network"),
			})
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000c4340eb93cfe999a121f82293f985703a30fe1494cf317bb73f57e31177573ce4a13d25af0ff0f1e000000009a209108a17fc5e9ccf98d677e7b0063f1362d515e34735e15314a219458b499b87f1202000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1304f0ff0f1e01040b576f726c6420506f776572ffffffff0100022e1b0000000043410479619b3615fc9f03aace413b9064dc97d4b6f892ad541e5a2d8a3181517443840a79517fb1a308e834ac3c53da86de69a9bcce27ae01cf77d9b2b9d7588d122aac00000000");
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
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 100,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 108,
				MinerConfirmationWindow = 144,
				ConsensusFactory = EnergiConsensusFactory.Instance,
				SupportSegwit = false,
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 127 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tnrg"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tnrg"))
			.SetMagic(0x7F6C89EF)
			.SetPort(39797)
			.SetRPCPort(8332)
			.SetMaxP2PVersion(70212)
			.SetName("nrg-reg")
			.AddAlias("nrg-regtest")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000009e66e4b752f3469f19130525455b4ffc61d3ea6140a0aef1e49166b9f377e034c0a8da5affff7f2000000000ad052da0b7df5e7766c48cab6be5790ae9b80b312e7c2cc65b4b9b802d2c567e0c000000000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1304ffff7f2001040b576f726c6420506f776572ffffffff0100022e1b0000000043410479619b3615fc9f03aace413b9064dc97d4b6f892ad541e5a2d8a3181517443840a79517fb1a308e834ac3c53da86de69a9bcce27ae01cf77d9b2b9d7588d122aac00000000");
			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210240,
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000924e924a21715"),
				PowTargetTimespan = TimeSpan.FromSeconds(24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(60),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 100,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1512,
				MinerConfirmationWindow = 2016,
				ConsensusFactory = EnergiConsensusFactory.Instance,
				SupportSegwit = false,
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 127 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 19 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tnrg"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tnrg"))
			.SetMagic(0x6EAB2AD9)
			.SetPort(19797)
			.SetRPCPort(19796)
			.SetMaxP2PVersion(70212)
			.SetName("nrg-test")
			.AddAlias("nrg-testnet")
			.AddAlias("energi-testnet")
			.AddAlias("energi-test")
			.AddDNSSeeds(new[]
			{
				new DNSSeedData("test.energi.network",  "dnsseed.test.energi.network"),
			})
		   .AddSeeds(new NetworkAddress[0])
		   .SetGenesis("0100000000000000000000000000000000000000000000000000000000000000000000009e66e4b752f3469f19130525455b4ffc61d3ea6140a0aef1e49166b9f377e034e1a7db5affff7f20000000007198a621af95399466f75017040999e3f523e6c460b60182a8d44d404ee0fc4cc2920101000000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1304ffff7f2001040b576f726c6420506f776572ffffffff0100022e1b0000000043410479619b3615fc9f03aace413b9064dc97d4b6f892ad541e5a2d8a3181517443840a79517fb1a308e834ac3c53da86de69a9bcce27ae01cf77d9b2b9d7588d122aac00000000");
			return builder;
		}
	}
}
