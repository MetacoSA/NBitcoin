using NBitcoin;
using NBitcoin.Altcoins.HashX11;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;



namespace NBitcoin.Altcoins
{
    // Reference: https://github.com/Bitcoinpluspay/Bitcoinplus/blob/master/src/chainparams.cpp
    public class Bitcoinplus : NetworkSetBase
    {
        public static Bitcoinplus Instance { get; } = new Bitcoinplus();

        public override string CryptoCode => "XBC";

        private Bitcoinplus()
        {

        }

#pragma warning disable CS0618 // Type or member is obsolete
        public class BitcoinplusConsensusFactory : ConsensusFactory
        {
            private BitcoinplusConsensusFactory()
            {
            }

            public static BitcoinplusConsensusFactory Instance { get; } = new BitcoinplusConsensusFactory();

            public override BlockHeader CreateBlockHeader()
            {
                return new BitcoinplusBlockHeader();
            }

            public override Block CreateBlock()
            {
                return new BitcoinplusBlock(new BitcoinplusBlockHeader());
            }

            public override Transaction CreateTransaction()
            {
                return new BitcoinplusTransaction(this);
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
                return new BitcoinplusHeadersPayload();
            }
        }

        public class BitcoinplusHeadersPayload : HeadersPayload
        {
            class BlockHeaderWithTxCount : IBitcoinSerializable
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

                    // Bitcoinplus specific addition - unknown usage see Bitcoinplus source.
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
                    Headers.AddRange(headersOff.Select(h => h._Header));
                }
            }
        }

        public class BitcoinplusBlockSignature : IBitcoinSerializable
        {
            protected bool Equals(BitcoinplusBlockSignature other)
            {
                return Equals(signature, other.signature);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((BitcoinplusBlockSignature)obj);
            }

            public override int GetHashCode()
            {
                return (signature?.GetHashCode() ?? 0);
            }

            public BitcoinplusBlockSignature()
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
                return !this.signature.Any();
            }

            public static bool operator ==(BitcoinplusBlockSignature a, BitcoinplusBlockSignature b)
            {
                if (System.Object.ReferenceEquals(a, b))
                    return true;

                if (((object)a == null) || ((object)b == null))
                    return false;

                return a.signature.SequenceEqual(b.signature);
            }

            public static bool operator !=(BitcoinplusBlockSignature a, BitcoinplusBlockSignature b)
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
                return Encoders.Hex.EncodeData(this.signature);
            }
        }

        /// <summary>
        /// A POS block header, this will create a work hash based on the X13 hash algos.
        /// </summary>
        public class BitcoinplusBlockHeader : BlockHeader
        {
            static byte[] CalculateHash(byte[] data, int offset, int count)
            {
                return new HashX11.X13().ComputeBytes(data.Skip(offset).Take(count).ToArray());
            }

            protected override HashStreamBase CreateHashStream()
            {
                return BufferedHashStream.CreateFrom(CalculateHash);
            }
        }

        /// <summary>
        /// A POS block that contains the additional block signature serialization.
        /// </summary>
        public class BitcoinplusBlock : Block
        {
            /// <summary>
            /// A block signature - signed by one of the coin base txout[N]'s owner.
            /// </summary>
            private BitcoinplusBlockSignature blockSignature = new BitcoinplusBlockSignature();

            public BitcoinplusBlock(BitcoinplusBlockHeader blockHeader) : base(blockHeader)
            {
            }

            public override ConsensusFactory GetConsensusFactory()
            {
                return BitcoinplusConsensusFactory.Instance;
            }

            /// <summary>
            /// The block signature type.
            /// </summary>
            public BitcoinplusBlockSignature BlockSignature
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

                //this.BlockSize = stream.Serializing ? stream.Counter.WrittenBytes : stream.Counter.ReadBytes;
            }
        }

        private class BitcoinplusWitness
        {
            private TxInList _Inputs;

            public BitcoinplusWitness(TxInList inputs)
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

        public class BitcoinplusTransaction : Transaction
        {
            public BitcoinplusTransaction(ConsensusFactory consensusFactory)
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

			public override void ReadWrite(BitcoinStream stream)
            {
                var witSupported = (((uint)stream.TransactionOptions & (uint)TransactionOptions.Witness) != 0) &&
                                        stream.ProtocolCapabilities.SupportWitness;

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

                TxInList vinTemp = null;
                TxOutList voutTemp = null;

                /* Try to read the vin. In case the dummy is there, this will be read as an empty vector. */
                stream.ReadWrite(ref vinTemp);
				vinTemp.Transaction = this;

				var hasNoDummy = (nVersionTemp & NoDummyInput) != 0 && vinTemp.Count == 0;
                if (witSupported && hasNoDummy)
                    nVersionTemp = nVersionTemp & ~NoDummyInput;

                if (vinTemp.Count == 0 && witSupported && !hasNoDummy)
                {
                    /* We read a dummy or an empty vin. */
                    stream.ReadWrite(ref flags);
                    if (flags != 0)
                    {
                        /* Assume we read a dummy and a flag. */
                        stream.ReadWrite(ref vinTemp);
                        vinTemp.Transaction = this;
                        stream.ReadWrite(ref voutTemp);
                        voutTemp.Transaction = this;
                    }
                    else
                    {
                        /* Assume read a transaction without output. */
                        voutTemp = new TxOutList();
                        voutTemp.Transaction = this;
                    }
                }
                else
                {
                    /* We read a non-empty vin. Assume a normal vout follows. */
                    stream.ReadWrite(ref voutTemp);
                    voutTemp.Transaction = this;
                }
                if (((flags & 1) != 0) && witSupported)
                {
                    /* The witness flag is present, and we support witnesses. */
                    flags ^= 1;
                    BitcoinplusWitness wit = new BitcoinplusWitness(vinTemp);
                    wit.ReadWrite(stream);
                }
                if (flags != 0)
                {
                    /* Unknown flag in the serialization */
                    throw new FormatException("Unknown transaction optional data");
                }
                LockTime lockTimeTemp = 0;
                stream.ReadWriteStruct(ref lockTimeTemp);

                this.Version = nVersionTemp;
                this.Time = nTimeTemp; // POS Timestamp
                vinTemp.ForEach(i => this.Inputs.Add(i));
                voutTemp.ForEach(i => this.Outputs.Add(i));
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
                    /* Check whether witnesses need to be serialized. */
                    if (HasWitness)
                    {
                        flags |= 1;
                    }
                }
                if (flags != 0)
                {
                    /* Use extended format in case witnesses are to be serialized. */
                    TxInList vinDummy = null;
                    stream.ReadWrite(ref vinDummy);
                    stream.ReadWrite(ref flags);
                }
                TxInList vin = this.Inputs;
                stream.ReadWrite(ref vin);
                vin.Transaction = this;
                TxOutList vout = this.Outputs;
                stream.ReadWrite(ref vout);
                vout.Transaction = this;
                if ((flags & 1) != 0)
                {
                    BitcoinplusWitness wit = new BitcoinplusWitness(this.Inputs);
                    wit.ReadWrite(stream);
                }
                LockTime lockTime = this.LockTime;
                stream.ReadWriteStruct(ref lockTime);
            }

            public static BitcoinplusTransaction ParseJson(string tx)
            {
                JObject obj = JObject.Parse(tx);
                BitcoinplusTransaction bxcTx = new BitcoinplusTransaction(Bitcoinplus.BitcoinplusConsensusFactory.Instance);
                DeserializeFromJson(obj, ref bxcTx);

                return bxcTx;
            }

            private static void DeserializeFromJson(JObject json, ref BitcoinplusTransaction tx)
            {
                tx.Version = (uint)json.GetValue("version");
                tx.Time = (uint)json.GetValue("time");
                tx.LockTime = (uint)json.GetValue("locktime");

                var vin = (JArray)json.GetValue("vin");
                for (int i = 0; i < vin.Count; i++)
                {
                    var jsonIn = (JObject)vin[i];
                    var txin = new TxIn();
                    tx.Inputs.Add(txin);

                    var script = (JObject)jsonIn.GetValue("scriptSig");
                    if (script != null)
                    {
                        txin.ScriptSig = new Script(Encoders.Hex.DecodeData((string)script.GetValue("hex")));
                        txin.PrevOut.Hash = uint256.Parse((string)jsonIn.GetValue("txid"));
                        txin.PrevOut.N = (uint)jsonIn.GetValue("vout");
                    }
                    else
                    {
                        var coinbase = (string)jsonIn.GetValue("coinbase");
                        txin.ScriptSig = new Script(Encoders.Hex.DecodeData(coinbase));
                    }

                    txin.Sequence = (uint)jsonIn.GetValue("sequence");

                }

                var vout = (JArray)json.GetValue("vout");
                for (int i = 0; i < vout.Count; i++)
                {
                    var jsonOut = (JObject)vout[i];
                    var txout = new TxOut();
                    tx.Outputs.Add(txout);

                    var btc = (decimal)jsonOut.GetValue("value");
                    var satoshis = btc * Money.COIN;
                    txout.Value = new Money((long)(satoshis));

                    var script = (JObject)jsonOut.GetValue("scriptPubKey");
                    txout.ScriptPubKey = new Script(Encoders.Hex.DecodeData((string)script.GetValue("hex")));
                }
            }

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
					// PoS Time
					sss.ReadWrite(this.Time);
					// Input prevouts/nSequence (none/all, depending on flags)
					sss.ReadWrite(hashPrevouts);
					sss.ReadWrite(hashSequence);
					// The input being signed (replacing the scriptSig with scriptCode + amount)
					// The prevout may already be contained in hashPrevout, and the nSequence
					// may already be contain in hashSequence.
					sss.ReadWrite(Inputs[nIn].PrevOut);
					sss.ReadWrite(scriptCode);
					sss.ReadWrite(spentOutput.Value.Satoshi);
					sss.ReadWrite(Inputs[nIn].Sequence);
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
				stream.ReadWrite(Time);
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
						stream.ReadWrite(Inputs[nInput].Sequence);
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
		}

        protected override void PostInit()
        {
            RegisterDefaultCookiePath("Bitcoinplus");
        }

        protected override NetworkBuilder CreateMainnet()
        {
            var builder = new NetworkBuilder();
            builder.SetConsensus(new Consensus()
            {
                MajorityEnforceBlockUpgrade = 750,
                MajorityRejectBlockOutdated = 950,
                MajorityWindow = 1000,
                PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(16 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(60),
                PowAllowMinDifficultyBlocks = false,
                PowNoRetargeting = false,
                RuleChangeActivationThreshold = 15120,
                MinerConfirmationWindow = 20160,
                CoinbaseMaturity = 100,
                LitecoinWorkCalculation = true,
                ConsensusFactory = BitcoinplusConsensusFactory.Instance,
				SupportSegwit = true
			})
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 25 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 85 })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 153 })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x88, 0xB2, 0x1E })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x88, 0xAD, 0xE4 })
            .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("xbc"))
            .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("xbc"))
            .SetMagic(0xf5a23f2d)
            .SetPort(8884)
            .SetRPCPort(8885)
            .SetName("xbc-main")
            .AddAlias("xbc-mainnet")
            .AddAlias("Bitcoinplus-mainnet")
            .AddAlias("Bitcoinplus-main")
            .AddDNSSeeds(new[]
            {
                new DNSSeedData("ns1.xbcBitcoinplus.co.uk", "seed1.xbcBitcoinplus.co.uk"),
                new DNSSeedData("ns2.xbcBitcoinplus.co.uk", "seed2.xbcBitcoinplus.co.uk"),
            })
            .SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000039c00f41f16be7dc366589ed8c38e6a259316095f113138b99e771c8ae05d281e36b1955ffff0f1e8aea19000101000000e36b1955010000000000000000000000000000000000000000000000000000000000000000ffffffff3100012a2d424243204e6577733a2043616d65726f6e206c61756e6368657320656c656374696f6e2063616d706169676e2effffffff010000000000000000000000000000");
            return builder;
        }


        protected override NetworkBuilder CreateTestnet()
        {
            var builder = new NetworkBuilder();
            builder.SetConsensus(new Consensus()
            {
                MajorityEnforceBlockUpgrade = 51,
                MajorityRejectBlockOutdated = 75,
                MajorityWindow = 100,
                PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(16 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(60),
                PowAllowMinDifficultyBlocks = true,
                PowNoRetargeting = false,
                RuleChangeActivationThreshold = 1512,
                MinerConfirmationWindow = 2016,
                CoinbaseMaturity = 100,
                LitecoinWorkCalculation = true,
                ConsensusFactory = BitcoinplusConsensusFactory.Instance,
				SupportSegwit = true
			})
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
            .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("txbc"))
            .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("txbc"))
            .SetMagic(0x05223570)
            .SetPort(18884)
            .SetRPCPort(18885)
            .SetName("xbc-test")
            .AddAlias("xbc-testnet")
            .AddAlias("Bitcoinplus-test")
            .AddAlias("Bitcoinplus-testnet")
            .AddDNSSeeds(new[]
            {
                new DNSSeedData("Bitcoinplus.net","testnet-seed1.Bitcoinplus.net"),
            })
            .SetGenesis("01000000000000000000000000000000000000000000000000000000000000000000000039c00f41f16be7dc366589ed8c38e6a259316095f113138b99e771c8ae05d281e36b1955ffff0f1e8aea19000101000000e36b1955010000000000000000000000000000000000000000000000000000000000000000ffffffff3100012a2d424243204e6577733a2043616d65726f6e206c61756e6368657320656c656374696f6e2063616d706169676e2effffffff010000000000000000000000000000");
            return builder;
        }

        protected override NetworkBuilder CreateRegtest()
        {
            var builder = new NetworkBuilder();
            builder.SetConsensus(new Consensus()
            {
                MajorityEnforceBlockUpgrade = 750,
                MajorityRejectBlockOutdated = 950,
                MajorityWindow = 1000,
                PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")),
                PowTargetTimespan = TimeSpan.FromSeconds(16 * 60),
                PowTargetSpacing = TimeSpan.FromSeconds(60),
                PowAllowMinDifficultyBlocks = true,
                MinimumChainWork = uint256.Zero,
                PowNoRetargeting = true,
                RuleChangeActivationThreshold = 108,
                MinerConfirmationWindow = 144,
                CoinbaseMaturity = 110,
                LitecoinWorkCalculation = true,
                ConsensusFactory = BitcoinplusConsensusFactory.Instance,
				SupportSegwit = true
			})
            .SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
            .SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
            .SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
            .SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
            .SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
            .SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("txbc"))
            .SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("txbc"))
            .SetMagic(0xdab5bffa)
            .SetPort(18444)
            .SetRPCPort(18445)
            .SetName("xbc-reg")
            .AddAlias("xbc-regtest")
            .AddAlias("Bitcoinplus-reg")
            .AddAlias("Bitcoinplus-regtest")
            .SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000da64b72ffb5a9f067e39f5d0d67efe353c5f051b30f213f3b7e3a4ffe847c348dae5494dffff7f20000000000101000000dae5494d010000000000000000000000000000000000000000000000000000000000000000ffffffff3100012a2d424243204e6577733a2043616d65726f6e206c61756e6368657320656c656374696f6e2063616d706169676e2effffffff010000000000000000000000000000");
            return builder;
        }
    }
}
