using System;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Newtonsoft.Json.Linq;

namespace NBitcoin.Altcoins
{
	// Reference: https://github.com/NeblioTeam/neblio/blob/master/wallet/chainparams.cpp
	public class Neblio : NetworkSetBase
	{
		public static Neblio Instance { get; } = new Neblio();

		public override string CryptoCode => "NEBL";

		private Neblio()
		{
		}

		public class NeblioConsensusFactory : ConsensusFactory
		{
			private NeblioConsensusFactory()
			{
			}

			public static NeblioConsensusFactory Instance { get; } = new NeblioConsensusFactory();

			public override BlockHeader CreateBlockHeader()
			{
				return new NeblioBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new NeblioBlock(new NeblioBlockHeader());
			}

			public override Transaction CreateTransaction()
			{
				return new NeblioTransaction(this);
			}
		}

#pragma warning disable CS0618 // Type or member is obsolete
		public class NeblioBlockHeader : BlockHeader
		{

			public override uint256 GetPoWHash()
			{
				throw new NotSupportedException("PoW for NEBL is not supported");
			}
		}

		public class NeblioBlock : Block
		{
			public NeblioBlock(NeblioBlockHeader h) : base(h)
			{
			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return Instance.Mainnet.Consensus.ConsensusFactory;
			}
		}

		public class NeblioTransaction : Transaction
		{
			public NeblioTransaction(ConsensusFactory consensusFactory)
			{
				_Factory = consensusFactory;
			}

			ConsensusFactory _Factory;
			public override ConsensusFactory GetConsensusFactory()
			{
				return _Factory;
			}

			/// <summary>
			/// POS Timestamp
			/// </summary>
			public uint Time { get; set; } = Utils.DateTimeToUnixTime(DateTime.UtcNow);

			public override uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, TxOut spentOutput, HashVersion sigversion, PrecomputedTransactionData precomputedTransactionData)
			{
				if (sigversion == HashVersion.WitnessV0)
				{
					if (spentOutput?.Value == null || spentOutput.Value == TxOut.NullMoney)
						throw new ArgumentException("The output being signed with the amount must be provided", nameof(spentOutput));
					uint256 hashPrevouts = uint256.Zero;
					uint256 hashSequence = uint256.Zero;
					uint256 hashOutputs = uint256.Zero;

					if ((nHashType & SigHash.AnyoneCanPay) == 0)
					{
						hashPrevouts = precomputedTransactionData == null ?
									   GetHashPrevouts() : precomputedTransactionData.HashPrevouts;
					}

					if ((nHashType & SigHash.AnyoneCanPay) == 0 && ((uint)nHashType & 0x1f) != (uint)SigHash.Single && ((uint)nHashType & 0x1f) != (uint)SigHash.None)
					{
						hashSequence = precomputedTransactionData == null ?
									   GetHashSequence() : precomputedTransactionData.HashSequence;
					}

					if (((uint)nHashType & 0x1f) != (uint)SigHash.Single && ((uint)nHashType & 0x1f) != (uint)SigHash.None)
					{
						hashOutputs = precomputedTransactionData == null ?
										GetHashOutputs() : precomputedTransactionData.HashOutputs;
					}
					else if (((uint)nHashType & 0x1f) == (uint)SigHash.Single && nIn < this.Outputs.Count)
					{
						BitcoinStream ss = CreateHashWriter(sigversion);
						ss.ReadWrite(this.Outputs[nIn]);
						hashOutputs = GetHash(ss);
					}

					BitcoinStream sss = CreateHashWriter(sigversion);
					// Version
					sss.ReadWrite(this.Version);
					sss.ReadWrite(this.Time); // Neblio Timestamp!!!
					// Input prevouts/nSequence (none/all, depending on flags)
					sss.ReadWrite(hashPrevouts);
					sss.ReadWrite(hashSequence);
					// The input being signed (replacing the scriptSig with scriptCode + amount)
					// The prevout may already be contained in hashPrevout, and the nSequence
					// may already be contain in hashSequence.
					sss.ReadWrite(Inputs[nIn].PrevOut);
					sss.ReadWrite(scriptCode);
					sss.ReadWrite(spentOutput.Value.Satoshi);
					sss.ReadWrite((uint)Inputs[nIn].Sequence);
					// Outputs (none/one/all, depending on flags)
					sss.ReadWrite(hashOutputs);
					// Locktime
					sss.ReadWriteStruct(LockTime);
					// Sighash type
					sss.ReadWrite((uint)nHashType);

					return GetHash(sss);
				}

				bool fAnyoneCanPay = (nHashType & SigHash.AnyoneCanPay) != 0;
				bool fHashSingle = ((byte)nHashType & 0x1f) == (byte)SigHash.Single;
				bool fHashNone = ((byte)nHashType & 0x1f) == (byte)SigHash.None;

				if (nIn >= Inputs.Count)
				{
					return uint256.One;
				}
				if (fHashSingle)
				{
					if (nIn >= Outputs.Count)
					{
						return uint256.One;
					}
				}

				var stream = CreateHashWriter(sigversion);
				stream.ReadWrite(Version);
				stream.ReadWrite(this.Time); // Neblio Timestamp!!!
				uint nInputs = (uint)(fAnyoneCanPay ? 1 : Inputs.Count);
				stream.ReadWriteAsVarInt(ref nInputs);
				for (int nInput = 0; nInput < nInputs; nInput++)
				{
					if (fAnyoneCanPay)
						nInput = nIn;
					stream.ReadWrite(Inputs[nInput].PrevOut);
					if (nInput != nIn)
					{
						stream.ReadWrite(Script.Empty);
					}
					else
					{
						WriteScriptCode(stream, scriptCode);
					}

					if (nInput != nIn && (fHashSingle || fHashNone))
						stream.ReadWrite((uint)0);
					else
						stream.ReadWrite((uint)Inputs[nInput].Sequence);
				}

				uint nOutputs = (uint)(fHashNone ? 0 : (fHashSingle ? nIn + 1 : Outputs.Count));
				stream.ReadWriteAsVarInt(ref nOutputs);
				for (int nOutput = 0; nOutput < nOutputs; nOutput++)
				{
					if (fHashSingle && nOutput != nIn)
					{
						this.Outputs.CreateNewTxOut().ReadWrite(stream);
					}
					else
					{
						Outputs[nOutput].ReadWrite(stream);
					}
				}

				stream.ReadWriteStruct(LockTime);
				stream.ReadWrite((uint)nHashType);
				return GetHash(stream);
			}

			private static uint256 GetHash(BitcoinStream stream)
			{
				var preimage = ((HashStreamBase)stream.Inner).GetHash();
				stream.Inner.Dispose();
				return preimage;
			}

			private static void WriteScriptCode(BitcoinStream stream, Script scriptCode)
			{
				int nCodeSeparators = 0;
				var reader = scriptCode.CreateReader();
				OpcodeType opcode;
				while (reader.TryReadOpCode(out opcode))
				{
					if (opcode == OpcodeType.OP_CODESEPARATOR)
						nCodeSeparators++;
				}

				uint n = (uint)(scriptCode.Length - nCodeSeparators);
				stream.ReadWriteAsVarInt(ref n);

				reader = scriptCode.CreateReader();
				int itBegin = 0;
				while (reader.TryReadOpCode(out opcode))
				{
					if (opcode == OpcodeType.OP_CODESEPARATOR)
					{
						stream.Inner.Write(scriptCode.ToBytes(true), itBegin, (int)(reader.Inner.Position - itBegin - 1));
						itBegin = (int)reader.Inner.Position;
					}
				}

				if (itBegin != scriptCode.Length)
					stream.Inner.Write(scriptCode.ToBytes(true), itBegin, (int)(reader.Inner.Position - itBegin));
			}

			public override void ReadWrite(BitcoinStream stream)
			{
				if (stream.Serializing)
					SerializeTxn(stream);
				else
					DeserializeTxn(stream);
			}

			private void DeserializeTxn(BitcoinStream stream)
			{

				UInt32 nVersionTemp = 0;
				stream.ReadWrite(ref nVersionTemp);

				// POS time stamp
				UInt32 nTimeTemp = 0;
				stream.ReadWrite(ref nTimeTemp);

				TxInList vinTemp = null;
				TxOutList voutTemp = null;

				// Try to read the vin.
				stream.ReadWrite(ref vinTemp);
				vinTemp.Transaction = this;
				// Assume a normal vout follows.
				stream.ReadWrite(ref voutTemp);
				voutTemp.Transaction = this;

				LockTime lockTimeTemp = LockTime.Zero;
				stream.ReadWriteStruct(ref lockTimeTemp);

				this.Version = nVersionTemp;
				this.Time = nTimeTemp; // POS Timestamp
				vinTemp.ForEach(i => this.Inputs.Add(i));
				voutTemp.ForEach(i => this.Outputs.Add(i));
				this.LockTime = lockTimeTemp;
			}

			private void SerializeTxn(BitcoinStream stream)
			{
				var version = this.Version;
				stream.ReadWrite(ref version);

				// POS Timestamp
				var time = this.Time;
				stream.ReadWrite(ref time);

				TxInList vin = this.Inputs;
				stream.ReadWrite(ref vin);
				vin.Transaction = this;

				TxOutList vout = this.Outputs;
				stream.ReadWrite(ref vout);
				vout.Transaction = this;

				LockTime lockTime = this.LockTime;
				stream.ReadWriteStruct(ref lockTime);
			}

			public string Hex { get; set; } = string.Empty;
			private static void DeserializeFromJson(JObject json, ref NeblioTransaction tx)
			{
				tx.Version = json.Value<uint>("version");
				//tx.Time = json.Value<uint>("time");
				var tm = json.Value<double>("time");
				tx.Time = Convert.ToUInt32(tm/1000);
				tx.LockTime = json.Value<uint>("locktime");
				tx.Hex = json.Value<string>("hex");
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
						Value = Money.Coins(jsonOut.Value<decimal>("value")/100000000)
					};
					tx.Outputs.Add(txout);

					var script = jsonOut.Value<JObject>("scriptPubKey");
					txout.ScriptPubKey = new Script(Encoders.Hex.DecodeData(script.Value<string>("hex")));
				}
			}
		}

#pragma warning restore CS0618 // Type or member is obsolete

		protected override void PostInit()
		{
			RegisterDefaultCookiePath("NEBL");
		}

		protected override NetworkBuilder CreateMainnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus
			{
				PowLimit = new Target(0 >> 1),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000a0c3931735170"),
				PowTargetTimespan = TimeSpan.FromSeconds(2 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(30),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 120,
				PowNoRetargeting = false,
				ConsensusFactory = NeblioConsensusFactory.Instance,
				SupportSegwit = false,
				CoinType = 146 // BIP44 CoinType
			})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 53 }) // 0x35
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 112 }) // 0x70
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 181 }) // 0xb5
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
				.SetMagic(0x325e6f86)
				.SetPort(6325)
				.SetRPCPort(6326)
				.SetMaxP2PVersion(60320)
				.SetName("Neblio-main")
				.AddAlias("Neblio-mainnet")
				.AddDNSSeeds(new[]
				{
					new DNSSeedData("seed.nebl.io", "seed.nebl.io"),
					new DNSSeedData("seed2.nebl.io", "seed2.nebl.io"),
				})
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000e7ae9132c789d33c38b735ae562ef57c9780c7328b0d1cb0121a321432d13f20137a7259ffff7f20252100000101000000137a7259010000000000000000000000000000000000000000000000000000000000000000ffffffff2900012a2532316a756c32303137202d204e65626c696f204669727374204e6574204c61756e63686573ffffffff010000000000000000000000000000");

			return builder;
		}

		protected override NetworkBuilder CreateTestnet()
		{
			var builder = new NetworkBuilder();
			builder.SetConsensus(new Consensus
			{
				PowLimit = new Target(0 >> 1),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000a0c3931735170"),
				PowTargetTimespan = TimeSpan.FromSeconds(2 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(30),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 120,
				PowNoRetargeting = false,
				ConsensusFactory = NeblioConsensusFactory.Instance,
				SupportSegwit = false,
				CoinType = 146
			})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 65 }) // 0x41
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 127 }) // 0x7f
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 193 }) // 0xc1
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
				.SetMagic(0x1bba63c5)
				.SetPort(16325)
				.SetRPCPort(16326)
				.SetMaxP2PVersion(60320)
				.SetName("Neblio-test")
				.AddAlias("Neblio-testnet")
				.AddDNSSeeds(new[]
				{
					new DNSSeedData("testnet-seed.nebl.io", "testnet-seed.nebl.io"),
				})
				.AddSeeds(new NetworkAddress[0])
				.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000e7ae9132c789d33c38b735ae562ef57c9780c7328b0d1cb0121a321432d13f20137a7259ffff7f20252100000101000000137a7259010000000000000000000000000000000000000000000000000000000000000000ffffffff2900012a2532316a756c32303137202d204e65626c696f204669727374204e6574204c61756e63686573ffffffff010000000000000000000000000000");

			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			var res = builder.SetConsensus(new Consensus
			{
				PowLimit = new Target(0 >> 1),
				MinimumChainWork = new uint256("0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(2 * 60 * 60),
				PowTargetSpacing = TimeSpan.FromSeconds(30),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 10,
				PowNoRetargeting = false,
				ConsensusFactory = NeblioConsensusFactory.Instance,
				SupportSegwit = false,
				CoinType = 146
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 65 }) // 0x41
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 127 }) // 0x7f
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 193 }) // 0xc1
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetMagic(0xcdf3e0ee)
			.SetPort(26325)
			.SetRPCPort(26326)
			.SetMaxP2PVersion(60320)
			.SetName("Neblio-reg")
			.AddAlias("Neblio-regtest")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000e7ae9132c789d33c38b735ae562ef57c9780c7328b0d1cb0121a321432d13f20137a7259ffff7f20252100000101000000137a7259010000000000000000000000000000000000000000000000000000000000000000ffffffff2900012a2532316a756c32303137202d204e65626c696f204669727374204e6574204c61756e63686573ffffffff010000000000000000000000000000");

			return builder;
		}
	}
}
