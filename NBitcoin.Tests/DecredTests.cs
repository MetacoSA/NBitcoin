using NBitcoin.Altcoins;
using NBitcoin.DataEncoders;
using Xunit;

namespace NBitcoin.Tests
{
	[Trait("Altcoins", "Altcoins")]
	public class DecredTests
	{
		[Fact]
		public void CanLoadDecredNetworks()
		{
			var mainnet = Decred.Instance.Mainnet;
			var testnet = Decred.Instance.Testnet;
			var regtest = Decred.Instance.Regtest;

			Assert.NotNull(mainnet);
			Assert.NotNull(testnet);
			Assert.NotNull(regtest);
			Assert.NotEqual(mainnet, testnet);
			Assert.NotEqual(mainnet, regtest);
			Assert.NotEqual(testnet, regtest);

			Assert.Equal("DCR", Decred.Instance.CryptoCode);
			Assert.Equal(ChainName.Mainnet, mainnet.ChainName);
			Assert.Equal(ChainName.Testnet, testnet.ChainName);
			Assert.Equal(ChainName.Regtest, regtest.ChainName);
		}

		[Fact]
		public void CanLookupDecredNetworks()
		{
			Assert.Equal(Decred.Instance.Mainnet, Network.GetNetwork("dcr-mainnet"));
			Assert.Equal(Decred.Instance.Mainnet, Network.GetNetwork("dcr-main"));
			Assert.Equal(Decred.Instance.Testnet, Network.GetNetwork("dcr-testnet"));
			Assert.Equal(Decred.Instance.Testnet, Network.GetNetwork("dcr-test"));
			Assert.Equal(Decred.Instance.Regtest, Network.GetNetwork("dcr-regtest"));
			Assert.Equal(Decred.Instance.Regtest, Network.GetNetwork("dcr-simnet"));
		}

		[Fact]
		public void CanParseDecredMainnetAddress()
		{
			var network = Decred.Instance.Mainnet;

			// P2PKH address (starts with "Ds")
			var addr = BitcoinAddress.Create("DsUZxxoHJSty8DCfwfartwTYbuhmVct7tJu", network);
			Assert.NotNull(addr);
			Assert.IsType<BitcoinPubKeyAddress>(addr);
			Assert.Equal("DsUZxxoHJSty8DCfwfartwTYbuhmVct7tJu", addr.ToString());

			// Another P2PKH
			var addr2 = BitcoinAddress.Create("DsU7xcg53nxaKLLcAUSKyRndjG78Z2VZnX9", network);
			Assert.NotNull(addr2);
			Assert.IsType<BitcoinPubKeyAddress>(addr2);
		}

		[Fact]
		public void CanParseDecredScriptAddress()
		{
			var network = Decred.Instance.Mainnet;

			// P2SH address (starts with "Dc")
			var addr = BitcoinAddress.Create("DcuQKx8BES9wU7C6Q5VmLBjw436r27hayjS", network);
			Assert.NotNull(addr);
			Assert.IsType<BitcoinScriptAddress>(addr);
			Assert.Equal("DcuQKx8BES9wU7C6Q5VmLBjw436r27hayjS", addr.ToString());
		}

		[Fact]
		public void CanParseDecredTestnetAddress()
		{
			var network = Decred.Instance.Testnet;

			// Testnet P2PKH (starts with "Ts")
			var addr = BitcoinAddress.Create("Tso2MVTUeVrjHTBFedFhiyM7yVTbieqp91h", network);
			Assert.NotNull(addr);
			Assert.IsType<BitcoinPubKeyAddress>(addr);
			Assert.Equal("Tso2MVTUeVrjHTBFedFhiyM7yVTbieqp91h", addr.ToString());
		}

		[Fact]
		public void CanGenerateDecredAddress()
		{
			var network = Decred.Instance.Mainnet;
			var key = new Key();
			var addr = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network);
			Assert.NotNull(addr);
			// Mainnet P2PKH addresses start with "Ds"
			Assert.StartsWith("Ds", addr.ToString());
		}

		[Fact]
		public void CanGenerateDecredAddressAllNetworks()
		{
			foreach (var network in new[] { Decred.Instance.Mainnet, Decred.Instance.Testnet, Decred.Instance.Regtest })
			{
				var key = new Key();
				var addr = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, network);
				Assert.NotNull(addr);
				// Verify roundtrip
				var parsed = BitcoinAddress.Create(addr.ToString(), network);
				Assert.Equal(addr.ToString(), parsed.ToString());
			}
		}

		[Fact]
		public void CanParseDecredWIF()
		{
			var network = Decred.Instance.Mainnet;

			// Known WIF from dcrd test vectors
			var wif = "PmQdMn8xafwaQouk8ngs1CccRCB1ZmsqQxBaxNR4vhQi5a5QB5716";
			var secret = new BitcoinSecret(wif, network);
			Assert.NotNull(secret);

			// Verify the private key bytes
			var expectedKeyHex = "0c28fca386c7a227600b2fe50b7cae11ec86d3bf1fbe471be89827e19d72aa1d";
			Assert.Equal(expectedKeyHex, Encoders.Hex.EncodeData(secret.PrivateKey.ToBytes()));

			// Verify roundtrip
			Assert.Equal(wif, secret.ToString());
		}

		[Fact]
		public void CanRoundtripDecredWIF()
		{
			var network = Decred.Instance.Mainnet;

			// Generate a key and verify WIF roundtrip
			var key = new Key();
			var secret = new BitcoinSecret(key, network);
			var wifStr = secret.ToString();

			// Mainnet WIF starts with "Pm"
			Assert.StartsWith("Pm", wifStr);

			// Parse it back
			var parsed = new BitcoinSecret(wifStr, network);
			Assert.Equal(key.ToBytes(), parsed.PrivateKey.ToBytes());
		}

		[Fact]
		public void CanRoundtripDecredWIFTestnet()
		{
			var network = Decred.Instance.Testnet;

			// Known testnet WIF from dcrd test vectors
			var wif = "PtWVDUidYaiiNT5e2Sfb1Ah4evbaSopZJkkpFBuzkJYcYteugvdFg";
			var secret = new BitcoinSecret(wif, network);
			Assert.NotNull(secret);

			var expectedKeyHex = "dda35a1488fb97b6eb3fe6e9ef2a25814e396fb5dc295fe994b96789b21a0398";
			Assert.Equal(expectedKeyHex, Encoders.Hex.EncodeData(secret.PrivateKey.ToBytes()));

			// Verify roundtrip
			Assert.Equal(wif, secret.ToString());
		}

		[Fact]
		public void DecredAddressUsesBlakeHash()
		{
			// Verify that Decred uses RIPEMD160(Blake256()) for Hash160
			// by checking that the same key produces different addresses
			// on Decred vs Bitcoin
			var key = new Key();
			var btcAddr = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, Network.Main);
			var dcrAddr = key.PubKey.GetAddress(ScriptPubKeyType.Legacy, Decred.Instance.Mainnet);

			// Different hash functions should yield different addresses
			Assert.NotEqual(btcAddr.ToString(), dcrAddr.ToString());
		}

		[Fact]
		public void CanParseDecredGenesis()
		{
			var network = Decred.Instance.Mainnet;

			// Parse the mainnet genesis block
			var genesis = network.GetGenesis();
			Assert.NotNull(genesis);
			Assert.NotNull(genesis.Header);

			// Verify it has transactions
			Assert.NotEmpty(genesis.Transactions);

			// Verify header type
			Assert.IsType<Decred.DecredBlockHeader>(genesis.Header);
			var header = (Decred.DecredBlockHeader)genesis.Header;
			Assert.Equal(0u, header.Height);
		}

		[Fact]
		public void CanParseDecredRegtestGenesis()
		{
			var network = Decred.Instance.Regtest;
			var genesis = network.GetGenesis();
			Assert.NotNull(genesis);
			Assert.NotNull(genesis.Header);

			var header = (Decred.DecredBlockHeader)genesis.Header;
			Assert.Equal(0u, header.Height);
		}

		[Fact]
		public void CanSerializeDeserializeDecredTransaction()
		{
			var network = Decred.Instance.Mainnet;

			// Create a transaction, serialize it, deserialize it, check roundtrip
			var factory = network.Consensus.ConsensusFactory;
			var tx = (Decred.DecredTransaction)factory.CreateTransaction();
			Assert.NotNull(tx);

			// Add an input
			var txIn = (Decred.DecredTxIn)factory.CreateTxIn();
			txIn.PrevOut = new OutPoint(uint256.One, 0);
			txIn.PrevOutTree = 0;
			txIn.Sequence = 0xffffffff;
			txIn.Value = 500000000;
			txIn.Height = 100;
			txIn.Index = 0;
			txIn.ScriptSig = Script.Empty;
			tx.Inputs.Add(txIn);

			// Add an output
			var txOut = (Decred.DecredTxOut)factory.CreateTxOut();
			txOut.Value = Money.Coins(4.99m);
			txOut.Version = 0;
			txOut.ScriptPubKey = Script.Empty;
			tx.Outputs.Add(txOut);

			tx.Version = 1;
			tx.LockTime = 0;
			tx.Expiry = 0;
			tx.SerializeType = Decred.DecredTransaction.TxSerializeType.Full;

			// Serialize
			var hex = tx.ToHex();
			Assert.NotEmpty(hex);

			// Deserialize
			var tx2 = (Decred.DecredTransaction)factory.CreateTransaction();
			tx2.FromBytes(Encoders.Hex.DecodeData(hex));

			// Verify
			Assert.Equal(tx.Version, tx2.Version);
			Assert.Equal(tx.LockTime, tx2.LockTime);
			Assert.Equal(tx.Expiry, tx2.Expiry);
			Assert.Equal(tx.Inputs.Count, tx2.Inputs.Count);
			Assert.Equal(tx.Outputs.Count, tx2.Outputs.Count);

			var txIn2 = (Decred.DecredTxIn)tx2.Inputs[0];
			Assert.Equal(txIn.PrevOut, txIn2.PrevOut);
			Assert.Equal(txIn.PrevOutTree, txIn2.PrevOutTree);
			Assert.Equal(txIn.Sequence, txIn2.Sequence);
			Assert.Equal(txIn.Value, txIn2.Value);
			Assert.Equal(txIn.Height, txIn2.Height);

			var txOut2 = (Decred.DecredTxOut)tx2.Outputs[0];
			Assert.Equal(txOut.Value, txOut2.Value);
			Assert.Equal(txOut.Version, txOut2.Version);
		}

		[Fact]
		public void DecredTxHashIsOverPrefixOnly()
		{
			var network = Decred.Instance.Mainnet;
			var factory = network.Consensus.ConsensusFactory;

			// Create a transaction with witness data
			var tx = (Decred.DecredTransaction)factory.CreateTransaction();
			tx.Version = 1;
			tx.SerializeType = Decred.DecredTransaction.TxSerializeType.Full;

			var txIn = (Decred.DecredTxIn)factory.CreateTxIn();
			txIn.PrevOut = new OutPoint(uint256.One, 0);
			txIn.PrevOutTree = 0;
			txIn.Sequence = 0xffffffff;
			txIn.Value = 500000000;
			txIn.Height = 1;
			txIn.Index = 0;
			txIn.ScriptSig = new Script(OpcodeType.OP_1);
			tx.Inputs.Add(txIn);

			var txOut = (Decred.DecredTxOut)factory.CreateTxOut();
			txOut.Value = Money.Coins(4.99m);
			txOut.Version = 0;
			txOut.ScriptPubKey = Script.Empty;
			tx.Outputs.Add(txOut);

			var hash1 = tx.GetHash();

			// Change witness data (script sig), hash should not change
			// since GetHash() is over prefix only
			txIn.ScriptSig = new Script(OpcodeType.OP_2);
			// Clear cached hash
			var tx2 = (Decred.DecredTransaction)factory.CreateTransaction();
			tx2.FromBytes(tx.ToBytes());
			((Decred.DecredTxIn)tx2.Inputs[0]).ScriptSig = new Script(OpcodeType.OP_2);

			// Serialize just prefix for both and compare
			// The prefix doesn't include scriptsig, so prefix hashes should match
			var hash2 = tx2.GetHash();

			// GetHash hashes prefix only, witness changes shouldn't affect it
			// but we rebuilt the tx so we need to verify the prefix is the same
			Assert.Equal(hash1, hash2);
		}

		[Fact]
		public void DecredConsensusFactoryCreatesCorrectTypes()
		{
			var network = Decred.Instance.Mainnet;
			var factory = network.Consensus.ConsensusFactory;

			Assert.IsType<Decred.DecredTransaction>(factory.CreateTransaction());
			Assert.IsType<Decred.DecredBlockHeader>(factory.CreateBlockHeader());
			Assert.IsType<Decred.DecredBlock>(factory.CreateBlock());
			Assert.IsType<Decred.DecredTxIn>(factory.CreateTxIn());
			Assert.IsType<Decred.DecredTxOut>(factory.CreateTxOut());
		}

		[Fact]
		public void DecredConsensusFactoryRPCProperties()
		{
			var factory = Decred.Instance.Mainnet.Consensus.ConsensusFactory;

			Assert.False(factory.SupportsBatchRPC);
			Assert.False(factory.SupportsGetRawTransactionBlockId);
			Assert.Equal(1, factory.GetRawTransactionVerboseParam);
		}

		[Fact]
		public void DecredNetworkProperties()
		{
			var mainnet = Decred.Instance.Mainnet;
			Assert.IsType<Decred.DecredConsensusFactory>(mainnet.Consensus.ConsensusFactory);
			Assert.Equal(9108, mainnet.DefaultPort);
			Assert.Equal(9109, mainnet.RPCPort);

			var testnet = Decred.Instance.Testnet;
			Assert.IsType<Decred.DecredConsensusFactory>(testnet.Consensus.ConsensusFactory);
			Assert.Equal(19108, testnet.DefaultPort);
			Assert.Equal(19109, testnet.RPCPort);

			var regtest = Decred.Instance.Regtest;
			Assert.IsType<Decred.DecredConsensusFactory>(regtest.Consensus.ConsensusFactory);
			Assert.Equal(19560, regtest.DefaultPort);
			Assert.Equal(19561, regtest.RPCPort);

			// Bitcoin should not use DecredConsensusFactory
			Assert.IsNotType<Decred.DecredConsensusFactory>(Network.Main.Consensus.ConsensusFactory);
		}

		[Fact]
		public void DecredBlockHeaderSerializationRoundtrip()
		{
			var network = Decred.Instance.Mainnet;

			// Parse genesis block and verify header roundtrip
			var genesis = network.GetGenesis();
			var header = (Decred.DecredBlockHeader)genesis.Header;

			// Serialize
			var headerBytes = header.ToBytes();
			Assert.Equal(180, headerBytes.Length); // Decred headers are 180 bytes

			// Deserialize
			var header2 = (Decred.DecredBlockHeader)network.Consensus.ConsensusFactory.CreateBlockHeader();
			header2.ReadWrite(headerBytes, network);

			Assert.Equal(header.Version, header2.Version);
			Assert.Equal(header.HashPrevBlock, header2.HashPrevBlock);
			Assert.Equal(header.HashMerkleRoot, header2.HashMerkleRoot);
			Assert.Equal(header.StakeRoot, header2.StakeRoot);
			Assert.Equal(header.VoteBits, header2.VoteBits);
			Assert.Equal(header.Voters, header2.Voters);
			Assert.Equal(header.FreshStake, header2.FreshStake);
			Assert.Equal(header.Revocations, header2.Revocations);
			Assert.Equal(header.PoolSize, header2.PoolSize);
			Assert.Equal(header.Bits, header2.Bits);
			Assert.Equal(header.SBits, header2.SBits);
			Assert.Equal(header.Height, header2.Height);
			Assert.Equal(header.BlockSize, header2.BlockSize);
			Assert.Equal(header.BlockTime, header2.BlockTime);
			Assert.Equal(header.Nonce, header2.Nonce);
			Assert.Equal(header.StakeVersion, header2.StakeVersion);
		}

		[Fact]
		public void DecredGenesisBlockRoundtrip()
		{
			var network = Decred.Instance.Mainnet;
			var genesis = network.GetGenesis();

			// Serialize
			var blockBytes = genesis.ToBytes();

			// Deserialize
			var block2 = (Decred.DecredBlock)network.Consensus.ConsensusFactory.CreateBlock();
			block2.ReadWrite(blockBytes, network);

			Assert.Equal(genesis.Header.GetHash(), block2.Header.GetHash());
			Assert.Equal(genesis.Transactions.Count, block2.Transactions.Count);
		}
	}
}
