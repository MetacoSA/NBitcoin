using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin.Altcoins
{
	/// <summary>
	/// XDS is a Proof-of-Work/Proof-of-Stake-v4 coin with SegWit and ColdStaking.
	/// Bitcointalk: https://bitcointalk.org/index.php?topic=5218979.0
	/// </summary>
	public class XDS : NetworkSetBase
	{
		public static XDS Instance { get; } = new XDS();

		public override string CryptoCode => "XDS";

		public const int MaxReorgLength = 125;

		XDS()
		{
		}

		protected override NetworkBuilder CreateMainnet()
		{
			NetworkBuilder builder = new NetworkBuilder();

			var networkName = "XDSMain";
			var magic = 0x58445331u;
			int defaultPort = 38333;

			builder.SetConsensus(new Consensus
			{
				SubsidyHalvingInterval = 210000, // ok
				MajorityEnforceBlockUpgrade = 750, // ok
				MajorityRejectBlockOutdated = 950, // ok
				MajorityWindow = 1000, // ok
				BIP34Hash = new uint256("0x0000000e13c5bf36c155c7cb1681053d607c191fc44b863d0c5aef6d27b8eb8f"), // ok
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")), // ok
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60), // ok
				PowTargetSpacing = TimeSpan.FromSeconds(256), // ok
				PowAllowMinDifficultyBlocks = false, // ok
				PowNoRetargeting = false, // ok
				RuleChangeActivationThreshold = 1916, // ?
				MinerConfirmationWindow = 2016, // ok
				CoinType = 15118976, // Genesis nonce
				CoinbaseMaturity = 50, // ok
				ConsensusFactory = XDSConsensusFactory.FactoryInstance,
				SupportSegwit = true,
				BuriedDeployments = {
					[BuriedDeployments.BIP34] = 0,
					[BuriedDeployments.BIP65] = 0,
					[BuriedDeployments.BIP66] = 0},
				BIP9Deployments =
				{
					[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, BIP9DeploymentsParameters.AlwaysActive, 999999999),
					[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, BIP9DeploymentsParameters.AlwaysActive, 999999999),
					[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, BIP9DeploymentsParameters.AlwaysActive,999999999),
				},
				MinimumChainWork = null
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0 })    // same as Bitcoin but unsupported - bech32/P2WPKH must be used instead
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 5 })    // same as Bitcoin but unsupported - bech32/P2WSH must be used instead
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 128 })      // same as Bitcoin
			.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] { 0x01, 0x42 }) // same as Bitcoin
			.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] { 0x01, 0x43 }) // same as Bitcoin
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBase58Bytes(Base58Type.PASSPHRASE_CODE, new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 })
			.SetBase58Bytes(Base58Type.CONFIRMATION_CODE, new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A })
			.SetBase58Bytes(Base58Type.ASSET_ID, new byte[] { 23 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, "xds")
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, "xds")
			.SetMagic(magic)
			.SetPort(defaultPort)
			.SetRPCPort(48333)
			.SetName(networkName)
			.AddSeeds(new List<NetworkAddress>
			{
				new NetworkAddress(IPAddress.Parse("178.62.62.160"), defaultPort),
				new NetworkAddress(IPAddress.Parse("206.189.33.114"), defaultPort),
				new NetworkAddress(IPAddress.Parse("159.65.148.135"), defaultPort),
			})
			.AddDNSSeeds(new DNSSeedData[0]);

			var genesisTime = Utils.DateTimeToUnixTime(new DateTime(2020, 1, 2, 23, 56, 00, DateTimeKind.Utc));
			var genesisNonce = 15118976u;
			var genesisBits = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
			var genesisVersion = 1;
			var genesisReward = Money.Zero;

			var genesis = ComputeGenesisBlock(genesisTime, genesisNonce, genesisBits, genesisVersion, genesisReward);
			builder.SetGenesis(Encoders.Hex.EncodeData(genesis.ToBytes()));

			if (genesis.GetHash() != uint256.Parse("0000000e13c5bf36c155c7cb1681053d607c191fc44b863d0c5aef6d27b8eb8f") ||
				genesis.Header.HashMerkleRoot != uint256.Parse("e3c549956232f0878414d765e83c3f9b1b084b0fa35643ddee62857220ea02b0"))
				throw new InvalidOperationException($"Invalid network {networkName}.");

			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			NetworkBuilder builder = new NetworkBuilder();

			var networkName = "XDSTest";
			const int testNetMagicNumberOffset = 1;
			var magic = 0x58445331u + testNetMagicNumberOffset;
			int defaultPort = 38333 + testNetMagicNumberOffset;

			builder.SetConsensus(new Consensus
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("00000d2ff9f3620b5487ed8ec154ce1947fec525e91e6973d1aeae93c53db7a3"),
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(256),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				CoinType = 24270024,
				CoinbaseMaturity = 50,
				ConsensusFactory = XDSConsensusFactory.FactoryInstance,
				SupportSegwit = true,
				BuriedDeployments = {
					[BuriedDeployments.BIP34] = 0,
					[BuriedDeployments.BIP65] = 0,
					[BuriedDeployments.BIP66] = 0},
				BIP9Deployments =
				{
					[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, BIP9DeploymentsParameters.AlwaysActive, 999999999),
					[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, BIP9DeploymentsParameters.AlwaysActive, 999999999),
					[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, BIP9DeploymentsParameters.AlwaysActive,999999999),
				},
				MinimumChainWork = null
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 5 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 128 })
			.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] { 0x01, 0x42 })
			.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] { 0x01, 0x43 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBase58Bytes(Base58Type.PASSPHRASE_CODE, new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 })
			.SetBase58Bytes(Base58Type.CONFIRMATION_CODE, new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A })
			.SetBase58Bytes(Base58Type.ASSET_ID, new byte[] { 23 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, "xdt")
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, "xdt")
			.SetMagic(magic)
			.SetPort(defaultPort)
			.SetRPCPort(48333 + testNetMagicNumberOffset)
			.SetName(networkName)
			.AddSeeds(new List<NetworkAddress>())
			.AddDNSSeeds(new DNSSeedData[0]);

			var genesisTime = Utils.DateTimeToUnixTime(new DateTime(2020, 11, 29, 23, 36, 00, DateTimeKind.Utc));
			var genesisNonce = 24270024u;
			var genesisBits = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
			var genesisVersion = 1;
			var genesisReward = Money.Zero;

			var genesis = ComputeGenesisBlock(genesisTime, genesisNonce, genesisBits, genesisVersion, genesisReward);
			builder.SetGenesis(Encoders.Hex.EncodeData(genesis.ToBytes()));

			if (genesis.GetHash() != uint256.Parse("00000d2ff9f3620b5487ed8ec154ce1947fec525e91e6973d1aeae93c53db7a3") ||
				genesis.Header.HashMerkleRoot != uint256.Parse("e3c549956232f0878414d765e83c3f9b1b084b0fa35643ddee62857220ea02b0"))
				throw new InvalidOperationException($"Invalid network {networkName}.");

			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			NetworkBuilder builder = new NetworkBuilder();

			var networkName = "XDSRegTest";
			const int regTestMagicNumberOffset = 2;
			var magic = 0x58445331u + regTestMagicNumberOffset;
			int defaultPort = 38333 + regTestMagicNumberOffset;

			builder.SetConsensus(new Consensus
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("00000e48aeeedabface6d45c0de52c7d0edaec14662ab4f56401361f70d12cc6"),
				PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(256),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 144,
				CoinType = 11687224,
				CoinbaseMaturity = 50,
				ConsensusFactory = XDSConsensusFactory.FactoryInstance,
				SupportSegwit = true,
				BuriedDeployments = {
					[BuriedDeployments.BIP34] = 0,
					[BuriedDeployments.BIP65] = 0,
					[BuriedDeployments.BIP66] = 0},
				BIP9Deployments =
				{
					[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, BIP9DeploymentsParameters.AlwaysActive, 999999999),
					[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, BIP9DeploymentsParameters.AlwaysActive, 999999999),
					[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, BIP9DeploymentsParameters.AlwaysActive,999999999),
				},
				MinimumChainWork = null
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 0 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 5 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 128 })
			.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] { 0x01, 0x42 })
			.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] { 0x01, 0x43 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBase58Bytes(Base58Type.PASSPHRASE_CODE, new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 })
			.SetBase58Bytes(Base58Type.CONFIRMATION_CODE, new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A })
			.SetBase58Bytes(Base58Type.ASSET_ID, new byte[] { 23 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, "xdr")
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, "xdr")
			.SetMagic(magic)
			.SetPort(defaultPort)
			.SetRPCPort(48333 + regTestMagicNumberOffset)
			.SetName(networkName)
			.AddSeeds(new List<NetworkAddress>())
			.AddDNSSeeds(new DNSSeedData[0]);

			var genesisTime = Utils.DateTimeToUnixTime(new DateTime(2020, 11, 29, 22, 50, 00, DateTimeKind.Utc));
			var genesisNonce = 11687224u;
			var genesisBits = new Target(new uint256("0000ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
			var genesisVersion = 1;
			var genesisReward = Money.Zero;

			var genesis = ComputeGenesisBlock(genesisTime, genesisNonce, genesisBits, genesisVersion, genesisReward);
			builder.SetGenesis(Encoders.Hex.EncodeData(genesis.ToBytes()));

			if (genesis.GetHash() != uint256.Parse("00000e48aeeedabface6d45c0de52c7d0edaec14662ab4f56401361f70d12cc6") ||
				genesis.Header.HashMerkleRoot != uint256.Parse("e3c549956232f0878414d765e83c3f9b1b084b0fa35643ddee62857220ea02b0"))
				throw new InvalidOperationException($"Invalid network {networkName}.");

			return builder;
		}

		public class XDSConsensusFactory : ConsensusFactory
		{
			XDSConsensusFactory()
			{
			}

			public static XDSConsensusFactory FactoryInstance { get; } = new XDSConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new XDSBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new XDSBlock(new XDSBlockHeader());
			}

			public override Transaction CreateTransaction()
			{
				return new Transaction();
			}

			protected bool IsHeadersPayload(Type type)
			{
				var baseType = typeof(HeadersPayload).GetTypeInfo();
				return baseType.IsAssignableFrom(type.GetTypeInfo());
			}

			public override bool TryCreateNew(Type type, out IBitcoinSerializable result)
			{
				if (IsHeadersPayload(type))
				{
					result = CreateHeadersPayload();
					return true;
				}

				return base.TryCreateNew(type, out result);
			}

			public HeadersPayload CreateHeadersPayload()
			{
				return new XDSHeadersPayload();
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class XDSBlockHeader : BlockHeader
		{
			public int CurrentVersion => 7;

			public ProvenBlockHeader ProvenBlockHeader { get; set; }

			protected internal override void SetNull()
			{
				nVersion = CurrentVersion;
				hashPrevBlock = 0;
				hashMerkleRoot = 0;
				nTime = 0;
				nBits = 0;
				nNonce = 0;
			}

			protected override HashStreamBase CreateHashStream()
			{
				if (this.Version == 1)
					return BufferedHashStream.CreateFrom(Sha512T.GetHash);
				return BufferedHashStream.CreateFrom(Hashes.DoubleSHA256RawBytes);
			}

			public override uint256 GetPoWHash()
			{
				var bytes = this.ToBytes();
				return new uint256(Sha512T.GetHash(this.ToBytes(), 0, bytes.Length));
			}
		}
#pragma warning restore CS0618 // Type or member is obsolete

		public class XDSBlockSignature : IBitcoinSerializable
		{
			protected bool Equals(XDSBlockSignature other)
			{
				return Equals(signature, other.signature);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((XDSBlockSignature)obj);
			}

			[SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
			public override int GetHashCode()
			{
				return this.signature != null ? this.signature.Sum(x => (float)x).GetHashCode() : 0;
			}

			public XDSBlockSignature()
			{
				this.signature = new byte[0];
			}

			private byte[] signature;

			public byte[] Signature
			{
				get => signature;
				set => signature = value;
			}

			public void SetNull()
			{
				signature = new byte[0];
			}

			public bool IsEmpty()
			{
				return !this.signature?.Any() ?? true;
			}

			public static bool operator ==(XDSBlockSignature a, XDSBlockSignature b)
			{
				if (ReferenceEquals(a, null))
				{
					if (ReferenceEquals(b, null))
					{
						return true;
					}

					return false;
				}
				return a.Equals(b);
			}

			public static bool operator !=(XDSBlockSignature a, XDSBlockSignature b)
			{
				return !(a == b);
			}

			#region IBitcoinSerializable Members

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWriteAsVarString(ref signature);
			}

			#endregion

			public override string ToString()
			{
				return this.signature != null ? Encoders.Hex.EncodeData(this.signature) : null;
			}
		}

		public class XDSBlock : Block
		{
			XDSBlockSignature blockSignature = new XDSBlockSignature();

#pragma warning disable CS0618 // Type or member is obsolete
			public XDSBlock() { }

			public XDSBlock(BlockHeader blockHeader) : base(blockHeader)
#pragma warning restore CS0618 // Type or member is obsolete
			{
			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return XDSConsensusFactory.FactoryInstance;
			}

			public XDSBlockSignature BlockSignature
			{
				get => this.blockSignature;
				set => this.blockSignature = value;
			}

			public override void ReadWrite(BitcoinStream stream)
			{
				base.ReadWrite(stream);
				stream.ReadWrite(ref this.blockSignature);
			}

			public Transaction GetProtocolTransaction()
			{
				return this.Transactions.Count > 1 && IsCoinstake(Transactions[1]) ? this.Transactions[1] : this.Transactions[0];
			}
		}

		public class ProvenBlockHeader : IBitcoinSerializable
		{
			XDSBlockHeader xdsBlockHeader;
			Transaction coinstake;
			PartialMerkleTree merkleProof;
			XDSBlockSignature signature;

			public XDSBlockHeader XDSBlockHeader => this.xdsBlockHeader;
			public Transaction Coinstake => this.coinstake;
			public PartialMerkleTree MerkleProof => this.merkleProof;
			public XDSBlockSignature Signature => this.signature;

			public long PosHeaderSize { get; protected set; }

			public long MerkleProofSize { get; protected set; }

			public long SignatureSize { get; protected set; }

			public long CoinstakeSize { get; protected set; }

			public long HeaderSize => this.PosHeaderSize + this.MerkleProofSize + this.SignatureSize + this.CoinstakeSize;

			public uint256 StakeModifierV2 { get; set; }

			public ProvenBlockHeader()
			{
			}

			public ProvenBlockHeader(XDSBlock block, XDSBlockHeader xdsBlockHeader)
			{
				if (block == null) throw new ArgumentNullException(nameof(block));

				this.xdsBlockHeader = xdsBlockHeader;
				this.xdsBlockHeader.HashPrevBlock = block.Header.HashPrevBlock;
				this.xdsBlockHeader.HashMerkleRoot = block.Header.HashMerkleRoot;
				this.xdsBlockHeader.BlockTime = block.Header.BlockTime;
				this.xdsBlockHeader.Bits = block.Header.Bits;
				this.xdsBlockHeader.Nonce = block.Header.Nonce;
				this.xdsBlockHeader.Version = block.Header.Version;
				this.xdsBlockHeader.ProvenBlockHeader = this;

				this.signature = block.BlockSignature;
				this.coinstake = block.GetProtocolTransaction();
				this.merkleProof = new MerkleBlock(block, new[] { this.coinstake.GetHash() }).PartialMerkleTree;
			}

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWrite(ref this.xdsBlockHeader);
				long prev = ProcessedBytes(stream);
				if (!stream.Serializing)
					this.xdsBlockHeader.ProvenBlockHeader = this;

				stream.ReadWrite(ref this.merkleProof);
				this.MerkleProofSize = ProcessedBytes(stream) - prev;

				prev = ProcessedBytes(stream);
				stream.ReadWrite(ref this.signature);
				this.SignatureSize = ProcessedBytes(stream) - prev;

				prev = ProcessedBytes(stream);
				stream.ReadWrite(ref this.coinstake);
				this.CoinstakeSize = ProcessedBytes(stream) - prev;
			}

			public override string ToString()
			{
				return this.xdsBlockHeader.GetHash().ToString();
			}

			public static long ProcessedBytes(BitcoinStream bitcoinStream)
			{
				return bitcoinStream.Serializing ? bitcoinStream.Counter.WrittenBytes : bitcoinStream.Counter.ReadenBytes;
			}
		}

		public class XDSHeadersPayload : HeadersPayload
		{
			public class BlockHeaderWithTxCount : IBitcoinSerializable
			{
				public BlockHeaderWithTxCount()
				{

				}

				public BlockHeaderWithTxCount(BlockHeader header)
				{
					Header = header;
				}

				public BlockHeader Header;
				#region IBitcoinSerializable Members

				public void ReadWrite(BitcoinStream stream)
				{
					stream.ReadWrite(ref Header);
					VarInt txCount = new VarInt(0);
					stream.ReadWrite(ref txCount);

					// Inherited Stratis-specific addition.
					stream.ReadWrite(ref txCount);
				}

				#endregion
			}

			public override void ReadWriteCore(BitcoinStream stream)
			{
				if (stream.Serializing)
				{
					var heardersOff = Headers.Select(h => new BlockHeaderWithTxCount(h)).ToList();
					stream.ReadWrite(ref heardersOff);
				}
				else
				{
					Headers.Clear();
					List<BlockHeaderWithTxCount> headersOff = new List<BlockHeaderWithTxCount>();
					stream.ReadWrite(ref headersOff);
					Headers.AddRange(headersOff.Select(h => h.Header));
				}
			}
		}

		public static bool IsCoinstake(Transaction transaction)
		{
			return transaction.Inputs.Any()
				   && !transaction.Inputs.First().PrevOut.IsNull
				   && transaction.Outputs.Count == 3
				   && IsEmpty(transaction.Outputs.First());

			bool IsEmpty(TxOut txOut)
			{
				return txOut.Value == Money.Zero && txOut.ScriptPubKey.Length == 0;
			}
		}

		public static class Sha512T
		{
			/// <summary>
			/// Truncated double-SHA512 hash. Used are the first 32 bytes of the second hash output.
			/// https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.180-4.pdf
			/// </summary>
			/// <param name="src">bytes to hash</param>
			/// <returns>hash</returns>
			public static byte[] GetHash(byte[] src, int offset, int count)
			{
				byte[] buffer32 = new byte[32];
				using (var sha512 = SHA512.Create())
				{
					var buffer64 = sha512.ComputeHash(src, offset, count);
					buffer64 = sha512.ComputeHash(buffer64);
					Buffer.BlockCopy(buffer64, 0, buffer32, 0, 32);
				}

				return buffer32;
			}
		}

		static Block ComputeGenesisBlock(uint genesisTime, uint genesisNonce, uint genesisBits, int genesisVersion, Money genesisReward)
		{
			string pszTimestamp = "https://www.blockchain.com/btc/block/611000";

			Transaction txNew = new Transaction();

			txNew.Version = 1;
			txNew.Inputs.Add(new TxIn
			{
				ScriptSig = new Script(Op.GetPushOp(0), new Op()
				{
					Code = (OpcodeType)0x1,
					PushData = new[] { (byte)42 }
				}, Op.GetPushOp(Encoding.UTF8.GetBytes(pszTimestamp)))
			});
			txNew.Outputs.Add(new TxOut
			{
				Value = genesisReward,
			});
			var genesis = XDSConsensusFactory.FactoryInstance.CreateBlock();
			genesis.Header.BlockTime = Utils.UnixTimeToDateTime(genesisTime);
			genesis.Header.Bits = genesisBits;
			genesis.Header.Nonce = genesisNonce;
			genesis.Header.Version = genesisVersion;
			genesis.Transactions.Add(txNew);
			genesis.Header.HashPrevBlock = uint256.Zero;
			genesis.UpdateMerkleRoot();
			
			return genesis;
		}
	}
}
