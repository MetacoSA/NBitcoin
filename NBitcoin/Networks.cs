using System;
using System.Collections.Generic;
using System.Net;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;

namespace NBitcoin
{

	public partial class Consensus
	{
		/// <summary>
		/// A class to enable additional 
		/// options to the consensus class
		/// </summary>
		public class ConsensusOptions
		{
		}

		public ConsensusOptions Options { get; set; }

		private Consensus OnClone(Consensus target)
		{
			target.LastPOWBlock = this.LastPOWBlock;
			target.ProofOfStakeLimit = this.ProofOfStakeLimit;
			target.ProofOfStakeLimitV2 = this.ProofOfStakeLimitV2;

			return target;
		}


		BigInteger proofOfStakeLimit;
		public BigInteger ProofOfStakeLimit
		{
			get
			{
				return proofOfStakeLimit;
			}
			set
			{
				EnsureNotFrozen();
				proofOfStakeLimit = value;
			}
		}

		BigInteger proofOfStakeLimitV2;
		public BigInteger ProofOfStakeLimitV2
		{
			get
			{
				return proofOfStakeLimitV2;
			}
			set
			{
				EnsureNotFrozen();
				proofOfStakeLimitV2 = value;
			}
		}

		int lastPOWBlock;
		public int LastPOWBlock
		{
			get
			{
				return lastPOWBlock;
			}
			set
			{
				EnsureNotFrozen();
				lastPOWBlock = value;
			}
		}
	}

	public partial class Network
	{
		static Network _StratisMain;
		public static Network StratisMain
		{
			get
			{
				if (_StratisMain == null)
					_StratisMain = InitStratisMain();
				return _StratisMain;
			}
		}

		static Network _StratisTest;
		public static Network StratisTest
		{
			get
			{
				if (_StratisTest == null)
					_StratisTest = InitStratisTest();
				return _StratisTest;
			}
		}

		static Network _StratisRegTest;
		public static Network StratisRegTest
		{
			get
			{
				if (_StratisRegTest == null)
					_StratisRegTest = InitStratisRegTest();
				return _StratisRegTest;
			}
		}

		private static Network InitStratisMain()
		{			
			Block.BlockSignature = true;
			Transaction.TimeStamp = true;

			var consensus = new Consensus();

            consensus.SubsidyHalvingInterval = 210000;
            consensus.MajorityEnforceBlockUpgrade = 750;
            consensus.MajorityRejectBlockOutdated = 950;
            consensus.MajorityWindow = 1000;
            consensus.BuriedDeployments[BuriedDeployments.BIP34] = 227931;
            consensus.BuriedDeployments[BuriedDeployments.BIP65] = 388381;
            consensus.BuriedDeployments[BuriedDeployments.BIP66] = 363725;
            consensus.BIP34Hash = new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8");
            consensus.PowLimit = new Target(new uint256("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
            consensus.PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
            consensus.PowTargetSpacing = TimeSpan.FromSeconds(10 * 60);
            consensus.PowAllowMinDifficultyBlocks = false;
            consensus.PowNoRetargeting = false;
            consensus.RuleChangeActivationThreshold = 1916; // 95% of 2016
            consensus.MinerConfirmationWindow = 2016; // nPowTargetTimespan / nPowTargetSpacing

            consensus.BIP9Deployments[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, 1199145601, 1230767999);
            consensus.BIP9Deployments[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, 1462060800, 1493596800);
            consensus.BIP9Deployments[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, 0, 0);

            consensus.LastPOWBlock = 12500;

			consensus.ProofOfStakeLimit =   new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));
			consensus.ProofOfStakeLimitV2 = new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false));

			consensus.CoinType = 105;

			var genesis = CreateStratisGenesisBlock(1470467000, 1831645, 0x1e0fffff, 1, Money.Zero);
			consensus.HashGenesisBlock = genesis.GetHash();

			// The message start string is designed to be unlikely to occur in normal data.
			// The characters are rarely used upper ASCII, not valid as UTF-8, and produce
			// a large 4-byte int at any alignment.
			var pchMessageStart = new byte[4];
            pchMessageStart[0] = 0x70;
            pchMessageStart[1] = 0x35;
            pchMessageStart[2] = 0x22;
            pchMessageStart[3] = 0x05;
			var magic = BitConverter.ToUInt32(pchMessageStart, 0); //0x5223570; 

			assert(consensus.HashGenesisBlock == uint256.Parse("0x0000066e91e46e5a264d42c89e1204963b2ee6be230b443e9159020539d972af"));
			assert(genesis.Header.HashMerkleRoot == uint256.Parse("0x65a26bc20b0351aebf05829daefa8f7db2f800623439f3c114257c91447f1518"));

			var builder = new NetworkBuilder()
				.SetName("StratisMain")
				.SetConsensus(consensus)
				.SetMagic(magic)
				.SetGenesis(genesis)
				.SetPort(16178)
				.SetRPCPort(16174)
				.SetTxFees(10000, 60000, 10000)
#if !NOSOCKET

				.AddDNSSeeds(new[]
				{
					new DNSSeedData("seednode1.stratisplatform.com", "seednode1.stratisplatform.com"),
					new DNSSeedData("seednode2.stratis.cloud", "seednode2.stratis.cloud"),
					new DNSSeedData("seednode3.stratisplatform.com", "seednode3.stratisplatform.com"),
					new DNSSeedData("seednode4.stratis.cloud", "seednode4.stratis.cloud")
				})
#endif

				//vAlertPubKey = Encoders.Hex.DecodeData("0486bce1bac0d543f104cbff2bd23680056a3b9ea05e1137d2ff90eeb5e08472eb500322593a2cb06fbf8297d7beb6cd30cb90f98153b5b7cce1493749e41e0284");

				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] {(63)})
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] {(125)})
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] {(63 + 128)})
				.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] {0x01, 0x42})
				.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] {0x01, 0x43})
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] {(0x04), (0x88), (0xB2), (0x1E)})
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] {(0x04), (0x88), (0xAD), (0xE4)})
				.SetBase58Bytes(Base58Type.PASSPHRASE_CODE, new byte[] {0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2})
				.SetBase58Bytes(Base58Type.CONFIRMATION_CODE, new byte[] {0x64, 0x3B, 0xF6, 0xA8, 0x9A})
				.SetBase58Bytes(Base58Type.STEALTH_ADDRESS, new byte[] {0x2a})
				.SetBase58Bytes(Base58Type.ASSET_ID, new byte[] {23})
				.SetBase58Bytes(Base58Type.COLORED_ADDRESS, new byte[] {0x13})
				.SetBech32(Bech32Type.WITNESS_PUBKEY_ADDRESS, "bc")
				.SetBech32(Bech32Type.WITNESS_SCRIPT_ADDRESS, "bc");

#if !NOSOCKET
			var seed = new[] { "101.200.198.155", "103.24.76.21", "104.172.24.79" };
			var vFixedSeeds = new List<NetworkAddress>();
			// Convert the pnSeeds array into usable address objects.
			Random rand = new Random();
            TimeSpan nOneWeek = TimeSpan.FromDays(7);
            for (int i = 0; i < seed.Length; i++)
            {
                // It'll only connect to one or two seed nodes because once it connects,
                // it'll get a pile of addresses with newer timestamps.				
                NetworkAddress addr = new NetworkAddress();
                // Seed nodes are given a random 'last seen time' of between one and two
                // weeks ago.
                addr.Time = DateTime.UtcNow - (TimeSpan.FromSeconds(rand.NextDouble() * nOneWeek.TotalSeconds)) - nOneWeek;
                addr.Endpoint = Utils.ParseIpEndpoint(seed[i], builder._Port);
                vFixedSeeds.Add(addr);
            }

			builder.AddSeeds(vFixedSeeds);
#endif
			return builder.BuildAndRegister();
		}

		private static Network InitStratisTest()
		{
			Block.BlockSignature = true;
			Transaction.TimeStamp = true;

			var consensus = Network.StratisMain.Consensus.Clone();
			consensus.PowLimit = new Target(uint256.Parse("0000ffff00000000000000000000000000000000000000000000000000000000"));

			// The message start string is designed to be unlikely to occur in normal data.
			// The characters are rarely used upper ASCII, not valid as UTF-8, and produce
			// a large 4-byte int at any alignment.
			var pchMessageStart = new byte[4];
			pchMessageStart[0] = 0x71;
			pchMessageStart[1] = 0x31;
			pchMessageStart[2] = 0x21;
			pchMessageStart[3] = 0x11;
			var magic = BitConverter.ToUInt32(pchMessageStart, 0); //0x5223570; 

			var genesis = Network.StratisMain.GetGenesis().Clone();
			genesis.Header.Time = 1493909211;
			genesis.Header.Nonce = 2433759;
			genesis.Header.Bits = consensus.PowLimit;
			consensus.HashGenesisBlock = genesis.GetHash();

			assert(consensus.HashGenesisBlock == uint256.Parse("0x00000e246d7b73b88c9ab55f2e5e94d9e22d471def3df5ea448f5576b1d156b9"));

			var builder = new NetworkBuilder()
				.SetName("StratisTest")
				.SetConsensus(consensus)
				.SetMagic(magic)
				.SetGenesis(genesis)
				.SetPort(26178)
				.SetRPCPort(26174)
				.SetTxFees(10000, 60000, 10000)
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { (65) })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { (196) })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { (65 + 128) })
				.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] { 0x01, 0x42 })
				.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] { 0x01, 0x43 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { (0x04), (0x88), (0xB2), (0x1E) })
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { (0x04), (0x88), (0xAD), (0xE4) })

#if !NOSOCKET
				.AddDNSSeeds(new[]
				{
					new DNSSeedData("testnode1.stratisplatform.com", "testnode1.stratisplatform.com"),
					new DNSSeedData("testnode2.stratis.cloud", "testnode2.stratis.cloud"),
					new DNSSeedData("testnode3.stratisplatform.com", "testnode3.stratisplatform.com")
				});

				builder.AddSeeds(new[] { new NetworkAddress(IPAddress.Parse("51.141.28.47"), builder._Port) }); // the c# testnet node
#endif

			return builder.BuildAndRegister();
		}

		private static Network InitStratisRegTest()
		{
			// TODO: move this to Networks
			var net = Network.GetNetwork("StratisRegTest");
			if (net != null)
				return net;

			Block.BlockSignature = true;
			Transaction.TimeStamp = true;

			var consensus = Network.StratisTest.Consensus.Clone();
			consensus.PowLimit = new Target(uint256.Parse("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));

			consensus.PowAllowMinDifficultyBlocks = true;
			consensus.PowNoRetargeting = true;

			var pchMessageStart = new byte[4];
			pchMessageStart[0] = 0xcd;
			pchMessageStart[1] = 0xf2;
			pchMessageStart[2] = 0xc0;
			pchMessageStart[3] = 0xef;
			var magic = BitConverter.ToUInt32(pchMessageStart, 0); 

			var genesis = Network.StratisMain.GetGenesis().Clone();
			genesis.Header.Time = 1494909211;
			genesis.Header.Nonce = 2433759;
			genesis.Header.Bits = consensus.PowLimit;
			consensus.HashGenesisBlock = genesis.GetHash();

			assert(consensus.HashGenesisBlock == uint256.Parse("0x93925104d664314f581bc7ecb7b4bad07bcfabd1cfce4256dbd2faddcf53bd1f"));

			var builder = new NetworkBuilder()
				.SetName("StratisRegTest")
				.SetConsensus(consensus)
				.SetMagic(magic)
				.SetGenesis(genesis)
				.SetPort(18444)
				.SetRPCPort(18442)
				.SetBase58Bytes(Base58Type.PUBKEY_ADDRESS, new byte[] { (65) })
				.SetBase58Bytes(Base58Type.SCRIPT_ADDRESS, new byte[] { (196) })
				.SetBase58Bytes(Base58Type.SECRET_KEY, new byte[] { (65 + 128) })
				.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_NO_EC, new byte[] { 0x01, 0x42 })
				.SetBase58Bytes(Base58Type.ENCRYPTED_SECRET_KEY_EC, new byte[] { 0x01, 0x43 })
				.SetBase58Bytes(Base58Type.EXT_PUBLIC_KEY, new byte[] { (0x04), (0x88), (0xB2), (0x1E) })
				.SetBase58Bytes(Base58Type.EXT_SECRET_KEY, new byte[] { (0x04), (0x88), (0xAD), (0xE4) });

			return builder.BuildAndRegister();
		}

		private static Block CreateStratisGenesisBlock(uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
            string pszTimestamp = "http://www.theonion.com/article/olympics-head-priestess-slits-throat-official-rio--53466";
            return CreateStratisGenesisBlock(pszTimestamp, nTime, nNonce, nBits, nVersion, genesisReward);
        }

        private static Block CreateStratisGenesisBlock(string pszTimestamp, uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
        {
            Transaction txNew = new Transaction();
            txNew.Version = 1;
            txNew.Time = nTime;
            txNew.AddInput(new TxIn()
            {
                ScriptSig = new Script(Op.GetPushOp(0), new Op()
                {
                    Code = (OpcodeType)0x1,
                    PushData = new[] { (byte)42 }
                }, Op.GetPushOp(Encoders.ASCII.DecodeData(pszTimestamp)))
            });
            txNew.AddOutput(new TxOut()
            {
                Value = genesisReward,
            });
            Block genesis = new Block();
            genesis.Header.BlockTime = Utils.UnixTimeToDateTime(nTime);
            genesis.Header.Bits = nBits;
            genesis.Header.Nonce = nNonce;
            genesis.Header.Version = nVersion;
            genesis.Transactions.Add(txNew);
            genesis.Header.HashPrevBlock = uint256.Zero;
            genesis.UpdateMerkleRoot();
            return genesis;
        }
	}
}
