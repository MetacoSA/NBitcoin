using Microsoft.Extensions.Logging;
using NBitcoin.Altcoins.HashX11;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Logging;
using NBitcoin.Protocol;
using NBitcoin.Protocol.Behaviors;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NBitcoin.Altcoins
{
	/// <summary>
	/// <see cref="http://www.stratisplatform.com">Stratis</see> Altcoin definition 
	/// </summary>
	public class Stratis : NetworkSetBase
	{
		public static Stratis Instance { get; } = new Stratis();

		public override string CryptoCode => "STRAT";

		private Stratis()
		{
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class StratisConsensusFactory : ConsensusFactory
		{
			public static StratisConsensusFactory Instance { get; } = new StratisConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new StratisBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new StratisBlock(new StratisBlockHeader());
			}

			public override Transaction CreateTransaction()
			{
				return new StratisTransaction();
			}

			protected bool IsHeadersPayload(Type type)
			{
				var baseType = typeof(HeadersPayload).GetTypeInfo();
				return baseType.IsAssignableFrom(type.GetTypeInfo());
			}

			public override bool TryCreateNew(Type type, out IBitcoinSerializable result)
			{
				result = null;
				if (IsHeadersPayload(type))
				{
					result = CreateHeadersPayload();
					return true;
				}

				return base.TryCreateNew(type, out result);
			}

			public HeadersPayload CreateHeadersPayload()
			{
				return new StratisHeadersPayload();
			}

			public override IEnumerable<NodeBehavior> GetCustomBehaviors()
			{
				PayloadAttribute.Add("provhdr", typeof(StratisProvenHeadersPayload));

				StratisProvenHeaderBehavior[] customBehaviors = new[] { new StratisProvenHeaderBehavior() };
				return customBehaviors;
			}
		}

		public class StratisProvenBlockHeader : StratisBlockHeader
		{
			/// <summary>
			/// Coinstake transaction.
			/// </summary>
			private StratisTransaction coinstake;

			/// <summary>
			/// Gets coinstake transaction.
			/// </summary>
			public StratisTransaction Coinstake => this.coinstake;

			/// <summary>
			/// Merkle proof that proves the coinstake tx is included in a block that is being represented by the provided header.
			/// </summary>
			private PartialMerkleTree merkleProof;

			/// <summary>
			/// Gets merkle proof that proves the coinstake tx is included in a block that is being represented by the provided header.
			/// </summary>
			public PartialMerkleTree MerkleProof => this.merkleProof;

			/// <summary>
			/// Block header signature which is signed with the private key which corresponds to
			/// coinstake's second output's public key.
			/// </summary>
			private StratisBlockSignature signature;

			/// <summary>
			/// Gets block header signature which is signed with the private key which corresponds to
			/// coinstake's second output's public key.
			/// </summary>
			public StratisBlockSignature Signature => this.signature;

			/// <summary>Gets the size of the merkle proof in bytes, the header must be serialized or deserialized for this property to be set.</summary>
			public long MerkleProofSize { get; protected set; }

			/// <summary>Gets the size of the signature in bytes, the header must be serialized or deserialized for this property to be set.</summary>
			public long SignatureSize { get; protected set; }

			/// <summary>Gets the size of the coinstake in bytes, the header must be serialized or deserialized for this property to be set.</summary>
			public long CoinstakeSize { get; protected set; }

			/// <summary>Gets the total header size - including the <see cref="BlockHeader.Size"/> - in bytes. <see cref="StratisProvenBlockHeader"/> must be serialized or deserialized for this property to be set.</summary>
			public long HeaderSize => Size + this.MerkleProofSize + this.SignatureSize + this.CoinstakeSize;

			/// <summary>
			/// Gets or sets the stake modifier v2.
			/// </summary>
			/// <value>
			/// The stake modifier v2.
			/// </value>
			/// <remarks>This property is used only in memory, it is not persisted to disk not sent over any Payloads.</remarks>
			public uint256 StakeModifierV2 { get; set; }

			public StratisProvenBlockHeader()
			{
			}

			public StratisProvenBlockHeader(StratisBlock block)
			{
				if (block == null) throw new ArgumentNullException(nameof(block));

				// Copy block header properties.
				this.HashPrevBlock = block.Header.HashPrevBlock;
				this.HashMerkleRoot = block.Header.HashMerkleRoot;
				this.nTime = block.Header.Time;
				this.Bits = block.Header.Bits;
				this.Nonce = block.Header.Nonce;
				this.Version = block.Header.Version;

				this.signature = block.BlockSignature;
				this.coinstake = block.GetProtocolTransaction();
				this.merkleProof = new MerkleBlock(block, new[] { this.coinstake.GetHash() }).PartialMerkleTree;
			}

			/// <inheritdoc />
			public override void ReadWrite(BitcoinStream stream)
			{
				long processedBytes = stream.Serializing ? stream.Counter.WrittenBytes : stream.Counter.ReadenBytes;

				base.ReadWrite(stream);
				long prev = processedBytes;

				stream.ReadWrite(ref this.merkleProof);
				this.MerkleProofSize = processedBytes - prev;

				prev = processedBytes;
				stream.ReadWrite(ref this.signature);
				this.SignatureSize = processedBytes - prev;

				prev = processedBytes;
				stream.ReadWrite(ref this.coinstake);
				this.CoinstakeSize = processedBytes - prev;
			}

			/// <inheritdoc />
			public override string ToString()
			{
				return this.GetHash().ToString();
			}
		}

		public class StratisProvenHeaderConsensusFactory : StratisConsensusFactory
		{
			public StratisProvenBlockHeader CreateProvenBlockHeader()
			{
				return new StratisProvenBlockHeader();
			}

			public override BlockHeader CreateBlockHeader()
			{
				return this.CreateProvenBlockHeader();
			}
		}

		[Payload("provhdr")]
		public class StratisProvenHeadersPayload : Payload
		{
			/// <summary>
			/// <see cref="Headers"/>
			/// </summary>
			private List<StratisProvenBlockHeader> headers = new List<StratisProvenBlockHeader>();

			/// <summary>
			/// Gets a list of up to 2,000 proven headers.
			/// </summary>
			public List<StratisProvenBlockHeader> Headers => this.headers;

			public StratisProvenHeadersPayload()
			{
			}

			public StratisProvenHeadersPayload(params StratisProvenBlockHeader[] headers)
			{
				this.Headers.AddRange(headers);
			}

			/// <inheritdoc />
			public override void ReadWriteCore(BitcoinStream stream)
			{
				stream.ConsensusFactory = new StratisProvenHeaderConsensusFactory();
				stream.ReadWrite(ref this.headers);
			}
		}

		public class StratisHeadersPayload : HeadersPayload
		{
			internal class BlockHeaderWithTxCount : IBitcoinSerializable
			{
				public BlockHeaderWithTxCount()
				{

				}
				public BlockHeaderWithTxCount(BlockHeader header)
				{
					_Header = header;
				}
				internal BlockHeader _Header;
				#region IBitcoinSerializable Members

				public void ReadWrite(BitcoinStream stream)
				{
					stream.ReadWrite(ref _Header);
					VarInt txCount = new VarInt(0);
					stream.ReadWrite(ref txCount);

					// Stratis specific addition - unknown usage see Stratis source.
					stream.ReadWrite(ref txCount);
				}

				#endregion
			}

			public StratisHeadersPayload(params StratisBlockHeader[] headers) : base(headers)
			{
			}

			public override void ReadWriteCore(BitcoinStream stream)
			{
				if (stream.Serializing)
				{
					var headersOff = Headers.Select(h => new BlockHeaderWithTxCount(h)).ToList();
					stream.ReadWrite(ref headersOff);
				}
				else
				{
					Headers.Clear();
					List<BlockHeaderWithTxCount> headersOff = new List<BlockHeaderWithTxCount>();
					stream.ReadWrite(ref headersOff);
					Headers.AddRange(headersOff.Select(h => h._Header));
				}
			}
		}

		public class StratisBlockSignature : IBitcoinSerializable
		{
			protected bool Equals(StratisBlockSignature other)
			{
				return Equals(signature, other.signature);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((StratisBlockSignature)obj);
			}

			public override int GetHashCode()
			{
				return (this.signature?.GetHashCode() ?? 0);
			}

			public StratisBlockSignature()
			{
				this.signature = new byte[0];
			}

			private byte[] signature;

			public byte[] Signature
			{
				get
				{
					return signature;
				}
				set
				{
					signature = value;
				}
			}

			internal void SetNull()
			{
				signature = new byte[0];
			}

			public bool IsEmpty()
			{
				return !this.signature?.Any() ?? true;
			}

			public static bool operator ==(StratisBlockSignature a, StratisBlockSignature b)
			{
				if (System.Object.ReferenceEquals(a, b))
					return true;

				return Utils.ArrayEqual(a.signature, b.signature);
			}

			public static bool operator !=(StratisBlockSignature a, StratisBlockSignature b)
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

		/// <summary>
		/// A POS block header, this will create a work hash based on the X13 hash algos.
		/// </summary>
		public class StratisBlockHeader : BlockHeader
		{
			const int CurrentVersion = 7;

			private readonly X13 x13;

			public StratisBlockHeader() : base()
			{
				this.x13 = new X13();
			}

			protected internal override void SetNull()
			{
				nVersion = CurrentVersion;
				hashPrevBlock = 0;
				hashMerkleRoot = 0;
				nTime = 0;
				nBits = 0;
				nNonce = 0;
			}

			public uint Time { get { return this.nTime; } set { this.nTime = value; } }

			private byte[] CalculateHash(byte[] data, int offset, int count)
			{
				byte[] hashData = data.SafeSubarray(offset, count);
				uint256 hash = null;
				if (this.nVersion > 6)
					hash = Hashes.Hash256(hashData);
				else
				{
					var x13 = new X13();
					hash = new uint256(this.x13.ComputeBytes(hashData));
				}
				return hash.ToBytes();
			}

			protected override HashStreamBase CreateHashStream()
			{
				return BufferedHashStream.CreateFrom(CalculateHash);
			}

			public override uint256 GetPoWHash()
			{
				
				return new uint256(this.x13.ComputeBytes(this.ToBytes()));
			}
		}

		/// <summary>
		/// A POS block that contains the additional block signature serialization.
		/// </summary>
		public class StratisBlock : Block
		{
			/// <summary>
			/// A block signature - signed by one of the coin base txout[N]'s owner.
			/// </summary>
			private StratisBlockSignature blockSignature = new StratisBlockSignature();

			public StratisBlock(StratisBlockHeader blockHeader) : base(blockHeader)
			{
			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return StratisConsensusFactory.Instance;
			}

			/// <summary>
			/// The block signature type.
			/// </summary>
			public StratisBlockSignature BlockSignature
			{
				get { return this.blockSignature; }
				set { this.blockSignature = value; }
			}

			/// <summary>
			/// The additional serialization of the block POS block.
			/// </summary>
			public override void ReadWrite(BitcoinStream stream)
			{
				base.ReadWrite(stream);
				stream.ReadWrite(ref this.blockSignature);
			}

			public new StratisBlockHeader Header => base.Header as StratisBlockHeader;

			/// <summary>
			/// Gets the block's coinstake transaction or returns the coinbase transaction if there is no coinstake.
			/// </summary>
			/// <returns>Coinstake transaction or coinbase transaction.</returns>
			/// <remarks>
			/// <para>In PoS blocks, coinstake transaction is the second transaction in the block.</para>
			/// <para>In PoW there isn't a coinstake transaction, return coinbase instead to be able to compute stake modifier for the next eventual PoS block.</para>
			/// </remarks>
			public StratisTransaction GetProtocolTransaction()
			{
				return (this.Transactions.Count > 1 && (this.Transactions[1] as StratisTransaction).IsCoinStake) ? this.Transactions[1] as StratisTransaction : this.Transactions[0] as StratisTransaction;
			}

		}

		private class StratisWitness
		{
			private TxInList _Inputs;

			public StratisWitness(TxInList inputs)
			{
				_Inputs = inputs;
			}

			internal bool IsNull()
			{
				return _Inputs.All(i => i.WitScript.PushCount == 0);
			}

			internal void ReadWrite(BitcoinStream stream)
			{
				for (int i = 0; i < _Inputs.Count; i++)
				{
					if (stream.Serializing)
					{
						var bytes = (_Inputs[i].WitScript ?? WitScript.Empty).ToBytes();
						stream.ReadWrite(ref bytes);
					}
					else
					{
						_Inputs[i].WitScript = WitScript.Load(stream);
					}
				}

				if (IsNull())
					throw new FormatException("Superfluous witness record");
			}
		}

		public class StratisTransaction : Transaction
		{
			public override ConsensusFactory GetConsensusFactory()
			{
				return StratisConsensusFactory.Instance;
			}

			/// <summary>
			/// POS Timestamp
			/// </summary>
			public uint Time { get; set; } = Utils.DateTimeToUnixTime(DateTime.UtcNow);

			public override void ReadWrite(BitcoinStream stream)
			{		
				var witSupported = stream.TransactionOptions.HasFlag(TransactionOptions.Witness) && stream.ProtocolCapabilities.SupportWitness;

				if (stream.Serializing)
					SerializeTxn(stream, witSupported);
				else
					DeserializeTxn(stream, witSupported);
			}

			private void DeserializeTxn(BitcoinStream stream, bool witSupported)
			{
				byte flags = 0;

				UInt32 nVersionTemp = 0;
				stream.ReadWrite(ref nVersionTemp);

				// POS time stamp
				uint nTimeTemp = 0;
				stream.ReadWrite(ref nTimeTemp);

				TxInList vinTemp = new TxInList();
				TxOutList voutTemp = new TxOutList();

				// Try to read the vin. In case the dummy is there, this will be read as an empty vector.
				stream.ReadWrite<TxInList, TxIn>(ref vinTemp);

				var hasNoDummy = (nVersionTemp & NoDummyInput) != 0 && vinTemp.Count == 0;
				if (witSupported && hasNoDummy)
					nVersionTemp = nVersionTemp & ~NoDummyInput;

				if (vinTemp.Count == 0 && witSupported && !hasNoDummy)
				{
					// We read a dummy or an empty vin.
					stream.ReadWrite(ref flags);
					if (flags != 0)
					{
						// Assume we read a dummy and a flag.
						stream.ReadWrite<TxInList, TxIn>(ref vinTemp);
						vinTemp.Transaction = this;
						stream.ReadWrite<TxOutList, TxOut>(ref voutTemp);
						voutTemp.Transaction = this;
					}
					else
					{
						// Assume read a transaction without output.
						voutTemp = new TxOutList();
						voutTemp.Transaction = this;
					}
				}
				else
				{
					// We read a non-empty vin. Assume a normal vout follows.
					stream.ReadWrite<TxOutList, TxOut>(ref voutTemp);
					voutTemp.Transaction = this;
				}
				if (((flags & 1) != 0) && witSupported)
				{
					// The witness flag is present, and we support witnesses.
					flags ^= 1;
					StratisWitness wit = new StratisWitness(vinTemp);
					wit.ReadWrite(stream);
				}
				if (flags != 0)
				{
					// Unknown flag in the serialization
					throw new FormatException("Unknown transaction optional data");
				}
				LockTime lockTimeTemp = LockTime.Zero;
				stream.ReadWriteStruct(ref lockTimeTemp);

				this.Version = nVersionTemp;
				this.Time = nTimeTemp; // POS Timestamp
				vinTemp.ForEach(i => this.AddInput(i));
				voutTemp.ForEach(i => this.AddOutput(i));
				this.LockTime = lockTimeTemp;
			}

			private void SerializeTxn(BitcoinStream stream, bool witSupported)
			{
				byte flags = 0;
				var version = (witSupported && (this.Inputs.Count == 0 && this.Outputs.Count > 0)) ? this.Version | NoDummyInput : this.Version;
				stream.ReadWrite(ref version);

				// POS Timestamp
				var time = this.Time;
				stream.ReadWrite(ref time);

				if (witSupported)
				{
					// Check whether witnesses need to be serialized.
					if (HasWitness)
					{
						flags |= 1;
					}
				}
				if (flags != 0)
				{
					// Use extended format in case witnesses are to be serialized.
					TxInList vinDummy = new TxInList();
					stream.ReadWrite<TxInList, TxIn>(ref vinDummy);
					stream.ReadWrite(ref flags);
				}
				TxInList vin = this.Inputs;
				stream.ReadWrite<TxInList, TxIn>(ref vin);
				vin.Transaction = this;
				TxOutList vout = this.Outputs;
				stream.ReadWrite<TxOutList, TxOut>(ref vout);
				vout.Transaction = this;
				if ((flags & 1) != 0)
				{
					StratisWitness wit = new StratisWitness(this.Inputs);
					wit.ReadWrite(stream);
				}
				LockTime lockTime = this.LockTime;
				stream.ReadWriteStruct(ref lockTime);
			}

			public static StratisTransaction ParseJson(string tx)
			{
				JObject obj = JObject.Parse(tx);
				StratisTransaction stratTx = new StratisTransaction();
				DeserializeFromJson(obj, ref stratTx);

				return stratTx;
			}

			private static void DeserializeFromJson(JObject json, ref StratisTransaction tx)
			{
				tx.Version = json.Value<uint>("version");
				tx.Time = json.Value<uint>("time");
				tx.LockTime = json.Value<uint>("locktime");

				var vin = json.Value<JArray>("vin");
				for (int i = 0; i < vin.Count; i++)
				{
					var jsonIn = (JObject)vin[i];
					var txin = new TxIn();
					var script = jsonIn.Value<JObject>("scriptSig");
					if (script != null)
					{
						txin.ScriptSig = new Script(Encoders.Hex.DecodeData(script.Value<string>("hex")));
						txin.PrevOut.Hash = uint256.Parse(jsonIn.Value<string>("txid"));
						txin.PrevOut.N = jsonIn.Value<uint>("vout");
					}
					else
					{
						txin.ScriptSig = new Script(Encoders.Hex.DecodeData(jsonIn.Value<string>("coinbase")));
					}
					txin.Sequence = jsonIn.Value<uint>("sequence");

					tx.Inputs.Add(txin);
				}

				var vout = json.Value<JArray>("vout");
				for (int i = 0; i < vout.Count; i++)
				{
					var jsonOut = (JObject)vout[i];
					var txout = new TxOut()
					{
						Value = Money.Coins(jsonOut.Value<decimal>("value"))
					};
					tx.Outputs.Add(txout);

					var script = jsonOut.Value<JObject>("scriptPubKey");
					txout.ScriptPubKey = new Script(Encoders.Hex.DecodeData(script.Value<string>("hex")));
				}
			}

			public bool IsCoinStake
			{
				get
				{
					// ppcoin: the coin stake transaction is marked with the first output empty
					return this.Inputs.Any()
						&& !this.Inputs.First().PrevOut.IsNull
						&& this.Outputs.Count() >= 2
						// First().IsEmpty
						&& this.Outputs.First().Value == 0 
						&& this.Outputs.First().ScriptPubKey.Length == 0;
				}
			}
		}

		public class StratisProvenHeaderBehavior : NodeBehavior
		{
			public override object Clone()
			{
				return new StratisProvenHeaderBehavior();
			}

			protected override void AttachCore()
			{
				AttachedNode.MessageReceived += AttachedNode_MessageReceived;
			}

			void AttachedNode_MessageReceived(Node node, IncomingMessage message)
			{
				var provHeadersPayload = message.Message.Payload as StratisProvenHeadersPayload;
				if (provHeadersPayload != null)
				{
					// When get "provhdr" ask node for normal headers instead.
					var headersPayload = new GetHeadersPayload();
					var prevHash = provHeadersPayload?.Headers?.FirstOrDefault()?.HashPrevBlock;
					if (prevHash != null)
					{
						headersPayload.BlockLocators = new BlockLocator
						{
							Blocks = new List<uint256> { prevHash }
						};
					}

					Logs.NodeServer.LogTrace("Received message {command} ({payload}) => sending {command} {payload})", provHeadersPayload.Command, provHeadersPayload, headersPayload.Command, headersPayload);
					node.SendMessageAsync(headersPayload);
				}
			}

			protected override void DetachCore()
			{
				AttachedNode.MessageReceived -= AttachedNode_MessageReceived;
			}
		}

		protected override NetworkBuilder CreateMainnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			var magic = BitConverter.ToUInt32(new byte[] { 0x70, 0x35, 0x22, 0x05 }, 0); //0x5223570;

			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				BIP34Hash = new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
				PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				CoinType = 105,
				CoinbaseMaturity = 50,
				ConsensusFactory = StratisConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 63 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 125 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 63 + 128 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, "bc")
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, "bc")
			.SetMagic(magic)
			.SetPort(16178)
			.SetRPCPort(16174)
			.SetName("StratisMain")
			.AddDNSSeeds(new[]
			{
					new DNSSeedData("seednode1.stratisplatform.com", "seednode1.stratisplatform.com"),
					new DNSSeedData("seednode2.stratis.cloud", "seednode2.stratis.cloud"),
					new DNSSeedData("seednode3.stratisplatform.com", "seednode3.stratisplatform.com"),
					new DNSSeedData("seednode4.stratis.cloud", "seednode4.stratis.cloud")
			})
			.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000018157f44917c2514c1f339346200f8b27d8ffaae9d8205bfae51030bc26ba265b88ba557ffff0f1eddf21b000101000000b88ba557010000000000000000000000000000000000000000000000000000000000000000ffffffff5d00012a4c58687474703a2f2f7777772e7468656f6e696f6e2e636f6d2f61727469636c652f6f6c796d706963732d686561642d7072696573746573732d736c6974732d7468726f61742d6f6666696369616c2d72696f2d2d3533343636ffffffff010000000000000000000000000000");

			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			NetworkBuilder builder = new NetworkBuilder();
			var magic = BitConverter.ToUInt32(new byte[] { 0x71, 0x31, 0x21, 0x11 }, 0); //0x5223570; 
			builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(uint256.Parse("0000ffff00000000000000000000000000000000000000000000000000000000")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = false,
				PowNoRetargeting = false,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				CoinType = 105,
				CoinbaseMaturity = 10,
				ConsensusFactory = StratisConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 65 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 65 + 128 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetMagic(magic)
			.SetPort(26178)
			.SetRPCPort(26174)
			.SetName("StratisTest")
			.AddDNSSeeds(new[]
			{
					new DNSSeedData("testnet1.stratisplatform.com", "testnet1.stratisplatform.com"),
					new DNSSeedData("testnet2.stratisplatform.com", "testnet2.stratisplatform.com"),
					new DNSSeedData("testnet3.stratisplatform.com", "testnet3.stratisplatform.com"),
					new DNSSeedData("testnet4.stratisplatform.com", "testnet4.stratisplatform.com")
			})
			.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000018157f44917c2514c1f339346200f8b27d8ffaae9d8205bfae51030bc26ba265db3e0b59ffff001fdf2225000101000000b88ba557010000000000000000000000000000000000000000000000000000000000000000ffffffff5d00012a4c58687474703a2f2f7777772e7468656f6e696f6e2e636f6d2f61727469636c652f6f6c796d706963732d686561642d7072696573746573732d736c6974732d7468726f61742d6f6666696369616c2d72696f2d2d3533343636ffffffff010000000000000000000000000000");

			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			NetworkBuilder builder = new NetworkBuilder();
			var magic = BitConverter.ToUInt32(new byte[] { 0xcd, 0xf2, 0xc0, 0xef }, 0);
			builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus()
			{
				SubsidyHalvingInterval = 210000,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(uint256.Parse("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
				PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(10 * 60),
				PowAllowMinDifficultyBlocks = true,
				PowNoRetargeting = true,
				RuleChangeActivationThreshold = 1916,
				MinerConfirmationWindow = 2016,
				CoinType = 105,
				CoinbaseMaturity = 10,
				ConsensusFactory = StratisConsensusFactory.Instance
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 65 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 65 + 128 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
			.SetMagic(magic)
			.SetPort(18444)
			.SetRPCPort(18442)
			.SetName("StratisRegTest")
			.SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000018157f44917c2514c1f339346200f8b27d8ffaae9d8205bfae51030bc26ba2651b811a59ffff7f20df2225000101000000b88ba557010000000000000000000000000000000000000000000000000000000000000000ffffffff5d00012a4c58687474703a2f2f7777772e7468656f6e696f6e2e636f6d2f61727469636c652f6f6c796d706963732d686561642d7072696573746573732d736c6974732d7468726f61742d6f6666696369616c2d72696f2d2d3533343636ffffffff010000000000000000000000000000");

			return builder;
		}
	}
}
