using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NBitcoin;
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Xunit;

namespace NBitcoin.Tests
{
	public class NetworkTests
	{
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanGetNetworkFromName()
		{
            Assert.Equal(Network.GetNetwork("main"), Network.Main);
			Assert.Equal(Network.GetNetwork("reg"), Network.RegTest);
			Assert.Equal(Network.GetNetwork("regtest"), Network.RegTest);
			Assert.Equal(Network.GetNetwork("testnet"), Network.TestNet);
			Assert.Equal(Network.GetNetwork("testnet3"), Network.TestNet);
			Assert.Null(Network.GetNetwork("invalid"));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ReadMagicByteWithFirstByteDuplicated()
		{
			var bytes = Network.Main.MagicBytes.ToList();
			bytes.Insert(0, bytes.First());

			using (var memstrema = new MemoryStream(bytes.ToArray()))
			{
				var found = Network.Main.ReadMagic(memstrema, new CancellationToken());
				Assert.True(found);
			}
		}
	}

	/// <summary>
	/// Some tests are Blockchain agnostice but have results that
	/// depend on the Bitcoin blockchain network parameters.
	/// This class is for such test cases.
	/// </summary>
	public class BitcoinNetwork : Network
	{
		private static Network bitcoinNetwork;
		
		public new static Network Main
		{
			get
			{
				if (bitcoinNetwork != null)
					return bitcoinNetwork;

				var network = new BitcoinNetwork();
				network.InitBitcoinMain();
				bitcoinNetwork =  network;
				return bitcoinNetwork;
			}
		}

		public override IEnumerable<Network> EnumerateNetworks()
		{
			yield return bitcoinNetwork;
		}

		private void InitBitcoinMain()
		{
			//SpendableCoinbaseDepth = 100;
			name = "Main";

			consensus.SubsidyHalvingInterval = 210000;
			consensus.MajorityEnforceBlockUpgrade = 750;
			consensus.MajorityRejectBlockOutdated = 950;
			consensus.MajorityWindow = 1000;
			consensus.BuriedDeployments[BuriedDeployments.BIP34] = 227931;
			consensus.BuriedDeployments[BuriedDeployments.BIP65] = 388381;
			consensus.BuriedDeployments[BuriedDeployments.BIP66] = 363725;
			consensus.BIP34Hash = new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8");
			consensus.PowLimit = new Target(new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff"));
			consensus.SegWitHeight = 2000000000;
			consensus.PowTargetTimespan = TimeSpan.FromSeconds(14 * 24 * 60 * 60); // two weeks
			consensus.PowTargetSpacing = TimeSpan.FromSeconds(10 * 60);
			consensus.PowAllowMinDifficultyBlocks = false;
			consensus.PowNoRetargeting = false;
			consensus.RuleChangeActivationThreshold = 1916; // 95% of 2016
			consensus.MinerConfirmationWindow = 2016; // nPowTargetTimespan / nPowTargetSpacing

			consensus.BIP9Deployments[BIP9Deployments.TestDummy] = new BIP9DeploymentsParameters(28, 1199145601, 1230767999);
			consensus.BIP9Deployments[BIP9Deployments.CSV] = new BIP9DeploymentsParameters(0, 1462060800, 1493596800);
			consensus.BIP9Deployments[BIP9Deployments.Segwit] = new BIP9DeploymentsParameters(1, 0, 0);

			// The message start string is designed to be unlikely to occur in normal data.
			// The characters are rarely used upper ASCII, not valid as UTF-8, and produce
			// a large 4-byte int at any alignment.
			magic = 0xD9B4BEF9;
			vAlertPubKey = Encoders.Hex.DecodeData("04fc9702847840aaf195de8442ebecedf5b095cdbb9bc716bda9110971b28a49e0ead8564ff0db22209e0374782c093bb899692d524e9d6a6956e7c5ecbcd68284");
			//nDefaultPort = 8333;
			//nRPCPort = 8332;
			//nSubsidyHalvingInterval = 210000;

			// the genesis block can not be generated as the Block serialization is different
			// set the the gemesis hash manually.

			//genesis = CreateGenesisBlock(1231006505, 2083236893, 0x1d00ffff, 1, Money.Coins(50m));
			consensus.HashGenesisBlock = uint256.Parse("0x000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f"); //genesis.GetHash();


			//assert(consensus.HashGenesisBlock == uint256.Parse("0x000000000019d6689c085ae165831e934ff763ae46a2a6c172b3f1b60a8ce26f"));
			//assert(genesis.Header.HashMerkleRoot == uint256.Parse("0x4a5e1e4baab89f3a32518a88c31bc87f618f76673e2cc77ab2127b7afdeda33b"));

			base58Prefixes[(int)Base58Type.PUBKEY_ADDRESS] = new byte[] { (0) };
			base58Prefixes[(int)Base58Type.SCRIPT_ADDRESS] = new byte[] { (5) };
			base58Prefixes[(int)Base58Type.SECRET_KEY] = new byte[] { (128) };
			base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_NO_EC] = new byte[] { 0x01, 0x42 };
			base58Prefixes[(int)Base58Type.ENCRYPTED_SECRET_KEY_EC] = new byte[] { 0x01, 0x43 };
			base58Prefixes[(int)Base58Type.EXT_PUBLIC_KEY] = new byte[] { (0x04), (0x88), (0xB2), (0x1E) };
			base58Prefixes[(int)Base58Type.EXT_SECRET_KEY] = new byte[] { (0x04), (0x88), (0xAD), (0xE4) };
			base58Prefixes[(int)Base58Type.PASSPHRASE_CODE] = new byte[] { 0x2C, 0xE9, 0xB3, 0xE1, 0xFF, 0x39, 0xE2 };
			base58Prefixes[(int)Base58Type.CONFIRMATION_CODE] = new byte[] { 0x64, 0x3B, 0xF6, 0xA8, 0x9A };
			base58Prefixes[(int)Base58Type.STEALTH_ADDRESS] = new byte[] { 0x2a };
			base58Prefixes[(int)Base58Type.ASSET_ID] = new byte[] { 23 };
			base58Prefixes[(int)Base58Type.COLORED_ADDRESS] = new byte[] { 0x13 };
			base58Prefixes[(int)Base58Type.WITNESS_P2WPKH] = new byte[] { 0x6 };
			base58Prefixes[(int)Base58Type.WITNESS_P2WSH] = new byte[] { (10) };
		}

		private Block CreateGenesisBlock(uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
		{
			string pszTimestamp = "The Times 03/Jan/2009 Chancellor on brink of second bailout for banks";
			Script genesisOutputScript = new Script(Op.GetPushOp(Encoders.Hex.DecodeData("04678afdb0fe5548271967f1a67130b7105cd6a828e03909a67962e0ea1f61deb649f6bc3f4cef38c4f35504e51ec112de5c384df7ba0b8d578a4c702b6bf11d5f")), OpcodeType.OP_CHECKSIG);
			return CreateGenesisBlock(pszTimestamp, genesisOutputScript, nTime, nNonce, nBits, nVersion, genesisReward);
		}

		private Block CreateGenesisBlock(string pszTimestamp, Script genesisOutputScript, uint nTime, uint nNonce, uint nBits, int nVersion, Money genesisReward)
		{
			Transaction txNew = new Transaction();
			txNew.Version = 1;
			txNew.AddInput(new TxIn()
			{
				ScriptSig = new Script(Op.GetPushOp(486604799), new Op()
				{
					Code = (OpcodeType)0x1,
					PushData = new[] { (byte)4 }
				}, Op.GetPushOp(Encoders.ASCII.DecodeData(pszTimestamp)))
			});
			txNew.AddOutput(new TxOut()
			{
				Value = genesisReward,
				ScriptPubKey = genesisOutputScript
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
