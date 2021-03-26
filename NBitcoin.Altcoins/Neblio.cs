using System;
using System.Linq;
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

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
				uint nTimeTemp = 0;
				stream.ReadWrite(ref nTimeTemp);

				TxInList vinTemp = new TxInList();
				TxOutList voutTemp = new TxOutList();

				// Try to read the vin.
				stream.ReadWrite<TxInList, TxIn>(ref vinTemp);

				// Assume a normal vout follows.
				stream.ReadWrite<TxOutList, TxOut>(ref voutTemp);
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
				stream.ReadWrite<TxInList, TxIn>(ref vin);
				vin.Transaction = this;

				TxOutList vout = this.Outputs;
				stream.ReadWrite<TxOutList, TxOut>(ref vout);
				vout.Transaction = this;

				LockTime lockTime = this.LockTime;
				stream.ReadWriteStruct(ref lockTime);
			}

			public static NeblioTransaction ParseJson(string tx)
			{
				JObject obj = JObject.Parse(tx);
				NeblioTransaction neblioTx = new NeblioTransaction(Neblio.NeblioConsensusFactory.Instance);
				DeserializeFromJson(obj, ref neblioTx);

				return neblioTx;
			}

			private static void DeserializeFromJson(JObject json, ref NeblioTransaction tx)
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
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 112 }) //0x70
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
				SubsidyHalvingInterval = unchecked(1000000000),
				MajorityEnforceBlockUpgrade = 51,
				MajorityRejectBlockOutdated = 75,
				MajorityWindow = 100,
				PowLimit = new Target(0 >> 1),
				MinimumChainWork = new uint256("0x000000000000000000000000000000000000000000000000000a0c3931735170"),
				PowTargetTimespan = TimeSpan.FromSeconds(3 * 50),
				PowTargetSpacing = TimeSpan.FromSeconds(3 * 50),
				PowAllowMinDifficultyBlocks = true,
				CoinbaseMaturity = 15,
				PowNoRetargeting = false,
				ConsensusFactory = NeblioConsensusFactory.Instance,
				SupportSegwit = false,
				CoinType = 1
			})
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("tNEBL"))
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("tNEBL"))
				.SetMagic(0xfae4aae1)
				.SetPort(16325)
				.SetRPCPort(16326)
				.SetMaxP2PVersion(70800)
				.SetName("Neblio-test")
				.AddAlias("Neblio-testnet")
				.AddSeeds(new NetworkAddress[0])
				//testnet down for now
				.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000cc59e59ff97ac092b55e423aa5495151ed6fb80570a5bb78cd5bd1c3821c21b8010000000000000000000000000000000000000000000000000000000000000033193156ffff001f070500000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1f04ffff001d010417696e736572742074696d657374616d7020737472696e67ffffffff01000004bfc91b8e001976a914345991dbf57bfb014b87006acdfafbfc5fe8292f88ac0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

			return builder;
		}

		protected override NetworkBuilder CreateRegtest()
		{
			var builder = new NetworkBuilder();
			var res = builder.SetConsensus(new Consensus
			{
				SubsidyHalvingInterval = 150,
				MajorityEnforceBlockUpgrade = 750,
				MajorityRejectBlockOutdated = 950,
				MajorityWindow = 1000,
				PowLimit = new Target(0 >> 1),
				MinimumChainWork = new uint256("0000000000000000000000000000000000000000000000000000000000000000"),
				PowTargetTimespan = TimeSpan.FromSeconds(1),
				PowTargetSpacing = TimeSpan.FromSeconds(1),
				PowAllowMinDifficultyBlocks = false,
				CoinbaseMaturity = 0,
				PowNoRetargeting = true,
				ConsensusFactory = NeblioConsensusFactory.Instance,
				SupportSegwit = false
			})
			.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { 111 })
			.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { 196 })
			.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { 239 })
			.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { 0x04, 0x35, 0x87, 0xCF })
			.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { 0x04, 0x35, 0x83, 0x94 })
			.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, Encoders.Bech32("rtNEBL"))
			.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, Encoders.Bech32("rtNEBL"))
			.SetMagic(0xfae4aad1)
			.SetPort(16325)
			.SetRPCPort(16326)
			.SetMaxP2PVersion(70800)
			.SetName("Neblio-reg")
			.AddAlias("Neblio-regtest")
			.AddDNSSeeds(new DNSSeedData[0])
			.AddSeeds(new NetworkAddress[0])
			.SetGenesis("010000000000000000000000000000000000000000000000000000000000000000000000cc59e59ff97ac092b55e423aa5495151ed6fb80570a5bb78cd5bd1c3821c21b8010000000000000000000000000000000000000000000000000000000000000033193156ffff7f20010000000101000000010000000000000000000000000000000000000000000000000000000000000000ffffffff1f04ffff001d010417696e736572742074696d657374616d7020737472696e67ffffffff01000004bfc91b8e001976a914345991dbf57bfb014b87006acdfafbfc5fe8292f88ac0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000");

			return builder;
		}
	}
}
