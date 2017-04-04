using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using NBitcoin;
using NBitcoin.BouncyCastle.Math;
using NBitcoin.DataEncoders;
using NBitcoin.OpenAsset;
using NBitcoin.Protocol;
using NBitcoin.Stealth;

namespace NBitcoin
{
	public partial class Consensus
	{
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
#if !NOSOCKET

				.AddDNSSeeds(new[]
				{
					new DNSSeedData("seed.stratisplatform.com", "seed.stratisplatform.com"),
					new DNSSeedData("seed.cloudstratis.com", "seed.cloudstratis.com")
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
				.SetBase58Bytes(Base58Type.WITNESS_P2WPKH, new byte[] {0x6})
				.SetBase58Bytes(Base58Type.WITNESS_P2WSH, new byte[] {(10)});

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

//		private void InitTest()
//		{
//			name = "TestNet";

//			Block.BlockSignature = true;
//			Transaction.TimeStamp = true;

//			consensus.SubsidyHalvingInterval = 210000;
//			consensus.MajorityEnforceBlockUpgrade = 51;
//			consensus.MajorityRejectBlockOutdated = 75;
//			consensus.MajorityWindow = 100;
//			consensus.BuriedDeployments[BuriedDeployments.BIP34] = 21111;
//			consensus.BuriedDeployments[BuriedDeployments.BIP65] = 581885;
//			consensus.BuriedDeployments[BuriedDeployments.BIP66] = 330776;
//			consensus.BIP34Hash = new uint256("0x0000000023b3a96d3484e5abb3755c413e7d41500f8e2a5c3f0dd01299cd8ef8");
//			consensus.PowLimit = new Target(new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
//			consensus.MinimumChainWork = new uint256("0x0000000000000000000000000000000000000000000000198b4def2baa9338d6");
//			consensus.PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
//			consensus.PowTargetSpacing = TimeSpan.FromSeconds(10 * 60);
//			consensus.PowAllowMinDifficultyBlocks = true;
//			consensus.PowNoRetargeting = false;
//			consensus.RuleChangeActivationThreshold = 1512; // 75% for testchains
//			consensus.MinerConfirmationWindow = 2016; // nPowTargetTimespan / nPowTargetSpacing

//			consensus.BIP9Deployments[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, 1199145601, 1230767999);
//			consensus.BIP9Deployments[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, 1456790400, 1493596800);
//			consensus.BIP9Deployments[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, 1462060800, 1493596800);

//			consensus.LastPOWBlock = 0x7fffffff;

//			var pchMessageStart = new byte[4];
//			pchMessageStart[0] = 0xcd;
//			pchMessageStart[1] = 0xf2;
//			pchMessageStart[2] = 0xc0;
//			pchMessageStart[3] = 0xef;
//			var mhash = BitConverter.ToUInt32(pchMessageStart, 0);
//			magic = mhash;

//			vAlertPubKey = DataEncoders.Encoders.Hex.DecodeData("0471dc165db490094d35cde15b1f5d755fa6ad6f2b5ed0f340e3f17f57389c3c2af113a8cbcc885bde73305a553b5640c83021128008ddf882e856336269080496");
//			nDefaultPort = 25714;
//			nRPCPort = 25715;

//			// Modify the testnet genesis block so the timestamp is valid for a later start.
//			genesis = CreateGenesisBlock(1470467000, 235708, 520159231, 1, Money.Zero);
//			consensus.HashGenesisBlock = genesis.GetHash();

//			assert(consensus.HashGenesisBlock == uint256.Parse("0x00000161ec84df354cc488b6de9c2a24ba12046e6c0286a797d6a0c8a43f0515"));

//#if !NOSOCKET
//			vFixedSeeds.Clear();
//			vSeeds.Clear();
//#endif

//			base58Prefixes = Network.Main.base58Prefixes.ToArray();
//			base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (111) };
//			base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
//			base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (239) };
//			base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x35), (0x87), (0xCF) };
//			base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x35), (0x83), (0x94) };
//			base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2b };
//			base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 115 };
//			base58Prefixes[(int)Base58Type.COLORED_ADDRESS] = new byte[] { 0x13 };
//			base58Prefixes[(int)Base58Type.WITNESS_P2WPKH] = new byte[] { (0x03) };
//			base58Prefixes[(int)Base58Type.WITNESS_P2WSH] = new byte[] { (40) };
//		}
//		private void InitReg()
//		{
//			name = "RegTest";
//			consensus.SubsidyHalvingInterval = 150;
//			consensus.MajorityEnforceBlockUpgrade = 750;
//			consensus.MajorityRejectBlockOutdated = 950;
//			consensus.MajorityWindow = 1000;
//			consensus.BuriedDeployments[BuriedDeployments.BIP34] = 100000000;
//			consensus.BuriedDeployments[BuriedDeployments.BIP65] = 100000000;
//			consensus.BuriedDeployments[BuriedDeployments.BIP66] = 100000000;
//			consensus.BIP34Hash = new uint256();
//			consensus.PowLimit = new Target(new uint256("7fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
//			consensus.MinimumChainWork = uint256.Zero;
//			consensus.PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
//			consensus.PowTargetSpacing = TimeSpan.FromSeconds(10 * 60);
//			consensus.PowAllowMinDifficultyBlocks = true;
//			consensus.PowNoRetargeting = true;
//			consensus.RuleChangeActivationThreshold = 108;
//			consensus.MinerConfirmationWindow = 144;

//			consensus.LastPOWBlock = 0x7fffffff;

//			var pchMessageStart = new byte[4];
//			pchMessageStart[0] = 0xcd;
//			pchMessageStart[1] = 0xf2;
//			pchMessageStart[2] = 0xc0;
//			pchMessageStart[3] = 0xef;
//			var mhash = BitConverter.ToUInt32(pchMessageStart, 0);
//			magic = mhash;

//			consensus.BIP9Deployments[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, 0, 999999999);
//			consensus.BIP9Deployments[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, 0, 999999999);
//			consensus.BIP9Deployments[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, 0, 999999999);

//			genesis = CreateGenesisBlock(1411111111, 1659424, 545259519, 1, Money.Zero);
//			consensus.HashGenesisBlock = genesis.GetHash();
//			nDefaultPort = 18444;

//			// TODO disable this till networks are sorted
//			//assert(consensus.HashGenesisBlock == uint256.Parse("0x00000d97ffc6d5e27e78954c5bf9022b081177756488f44780b4f3c2210b1645"));

//#if !NOSOCKET
//			vSeeds.Clear();  // Regtest mode doesn't have any DNS seeds.
//#endif
//			base58Prefixes = Network.TestNet.base58Prefixes.ToArray();
//			base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (111) };
//			base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (196) };
//			base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (239) };
//			base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x35), (0x87), (0xCF) };
//			base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x35), (0x83), (0x94) };
//			base58Prefixes[(int)Base58Type.COLORED_ADDRESS] = new byte[] { 0x13 };
//		}

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
