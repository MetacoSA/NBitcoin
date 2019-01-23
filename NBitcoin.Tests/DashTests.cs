using NBitcoin.Altcoins;
using NBitcoin.DataEncoders;
using Xunit;

namespace NBitcoin.Tests
{
	/// <summary>
	/// Ensures all Dash Special Transaction types can be read
	/// https://github.com/dashpay/dips/blob/master/dip-0002-special-transactions.md
	/// Also fixes https://github.com/MetacoSA/NBitcoin/issues/607
	/// </summary>
	public class DashTests
	{
		/// <summary>
		/// https://github.com/dashevo/dashcore-lib/blob/master/test/transaction/payload/proregtxpayload.js
		/// </summary>
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReadProviderRegistrationTransaction()
		{
			var proRegTx = new Dash.ProviderRegistrationTransaction(Encoders.Hex.DecodeData("010000000000effd6116c0bbe178d2e224c0f7fed5313a0b46dd222d63c5196319e2110db1031000000000000000000000000000ffff12ca34aa88c79f25f019d640ab50b569121967fabf3a2c74539b15ddc5d0053e2f3a2e70dbb1808ec60844ac4c8ba5fac73c9b6abbab23ef09df0d2347f2a3182eabead0027a2137e59c9f25f019d640ab50b569121967fabf3a2c74539bc9001976a914de77802bd7fcd19277b5229ac44085e3b3a3687088ac991c137dc07e13f02e188b2f97725f0f4487a0cd502ac8eb3fbc88e3c2f0d4ca411fe30ecf9cc167ff85f7d73efe9506de44762447063a62fbe82f19af39d6a353734ad0cd835d6b38cbe5a965b27a2be353ebe4a6a08e50510027c484ba74c4734c"));
			Assert.Equal(1, proRegTx.Version);
			Assert.Equal(0, proRegTx.Type);
			Assert.Equal(0, proRegTx.Mode);
			Assert.Equal("03b10d11e2196319c5632d22dd460b3a31d5fef7c024e2d278e1bbc01661fdef", proRegTx.CollateralHash.ToString());
			Assert.Equal((uint)16, proRegTx.CollateralIndex);
			Assert.Equal("00000000000000000000ffff12ca34aa", Encoders.Hex.EncodeData(proRegTx.IpAddress));
			Assert.Equal(35015, proRegTx.Port);
			Assert.Equal("9b53742c3abffa67191269b550ab40d619f0259f", proRegTx.KeyIdOwner.ToString());
			Assert.Equal("15ddc5d0053e2f3a2e70dbb1808ec60844ac4c8ba5fac73c9b6abbab23ef09df0d2347f2a3182eabead0027a2137e59c", Encoders.Hex.EncodeData(proRegTx.KeyIdOperator));
			Assert.Equal("9b53742c3abffa67191269b550ab40d619f0259f", proRegTx.KeyIdVoting.ToString());
			Assert.Equal("76a914de77802bd7fcd19277b5229ac44085e3b3a3687088ac", Encoders.Hex.EncodeData(proRegTx.ScriptPayout.ToBytes()));
			Assert.Equal(201, proRegTx.OperatorReward);
			Assert.Equal("cad4f0c2e388bc3febc82a50cda087440f5f72972f8b182ef0137ec07d131c99",
				proRegTx.InputsHash.ToString());
			Assert.Equal("1fe30ecf9cc167ff85f7d73efe9506de44762447063a62fbe82f19af39d6a353734ad0cd835d6b38cbe5a965b27a2be353ebe4a6a08e50510027c484ba74c4734c", Encoders.Hex.EncodeData(proRegTx.PayloadSig));
		}

		/// <summary>
		/// Checking example from
		/// https://github.com/dashpay/docs/raw/master/binary/merchants/Integration-Resources-Dash-v0.13.0-Transaction-Types.pdf
		/// </summary>
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReadExampleProviderRegistrationTransaction()
		{
			var proRegTx = new Dash.ProviderRegistrationTransaction(Encoders.Hex.DecodeData("01000000000026d3cb36b5360a23f5f4a2ea4c98d385c0c7a80788439f52a237717d799356a60100000000000000000000000000ffffc38d008f4e1f8a94fb062049b841f716dcded8257a3632fb053c8273ec203d1ea62cbdb54e10618329e4ed93e99bc9c5ab2f4cb0055ad281f9ad0808a1dda6aedf12c41c53142828879b8a94fb062049b841f716dcded8257a3632fb053c00001976a914e4876df5735eaa10a761dca8d62a7a275349022188acbc1055e0331ea0ea63caf80e0a7f417e50df6469a97db1f4f1d81990316a5e0b412045323bca7defef188065a6b30fb3057e4978b4f914e4e8cc0324098ae60ff825693095b927cd9707fe10edbf8ef901fcbc63eb9a0e7cd6fed39d50a8cde1cdb4"));
			Assert.Equal(1, proRegTx.Version);
			Assert.Equal(0, proRegTx.Type);
			Assert.Equal(0, proRegTx.Mode);
			Assert.Equal("a65693797d7137a2529f438807a8c7c085d3984ceaa2f4f5230a36b536cbd326", proRegTx.CollateralHash.ToString());
			Assert.Equal((uint)1, proRegTx.CollateralIndex);
			Assert.Equal(19999, proRegTx.Port);
			Assert.Equal("3c05fb32367a25d8dedc16f741b8492006fb948a", proRegTx.KeyIdOwner.ToString());
			Assert.Equal("8273ec203d1ea62cbdb54e10618329e4ed93e99bc9c5ab2f4cb0055ad281f9ad0808a1dda6aedf12c41c53142828879b", Encoders.Hex.EncodeData(proRegTx.KeyIdOperator));
			Assert.Equal("3c05fb32367a25d8dedc16f741b8492006fb948a", proRegTx.KeyIdVoting.ToString());
			Assert.Equal("yh9o9kPRK1s3YsuyCBe3DEjBit2RnzhgwH", proRegTx.ScriptPayout.GetDestinationAddress(Dash.Instance.Testnet).ToString());
			Assert.Equal(0, proRegTx.OperatorReward);
			Assert.Equal("0b5e6a319019d8f1f4b17da96964df507e417f0a0ef8ca63eaa01e33e05510bc",
				proRegTx.InputsHash.ToString());
		}

		/// <summary>
		/// https://github.com/dashevo/dashcore-lib/blob/master/test/transaction/payload/proregtxpayload.js
		/// </summary>
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReadProviderUpdateServiceTransaction()
		{
			var proUpServTx = new Dash.ProviderUpdateServiceTransaction(Encoders.Hex.DecodeData(
				"01007b1100a3e33b86b1e9948a1091648b44ac2e819850e321bbbbd9a7825cf173c800000000000000000000ffffc38d8f314e1f1976a9143e1f214c329557ae3711cb173bcf04d00762f3ff88ac3f7685789f3e6480ba6ed402285da0ed9cd0558265603fa8bad0eec0572cf1eb1746f9c46d654879d9afd67a439d4bc2ef7c1b26de2e59897fa83242d9bd819ff46c71d9e3d7aa1772f4003349b777140bedebded0a42efd64baf34f59c4a79c128df711c10a45505a0c2a94a5908f1642cbb56730f16b2cc2419a45890fb8ff"));
			Assert.Equal(1, proUpServTx.Version);
			Assert.Equal("c873f15c82a7d9bbbb21e35098812eac448b6491108a94e9b1863be3a300117b", proUpServTx.ProTXHash.ToString());
			Assert.Equal("00000000000000000000ffffc38d8f31", Encoders.Hex.EncodeData(proUpServTx.IpAddress));
			Assert.Equal(19999, proUpServTx.Port);
			Assert.Equal("76a9143e1f214c329557ae3711cb173bcf04d00762f3ff88ac", Encoders.Hex.EncodeData(proUpServTx.ScriptOperatorPayout.ToBytes()));
			Assert.Equal("ebf12c57c0eed0baa83f60658255d09ceda05d2802d46eba80643e9f7885763f", proUpServTx.InputsHash.ToString());
		}

		/// <summary>
		/// https://github.com/dashevo/dashcore-lib/blob/master/test/transaction/payload/proupregtxpayload.js
		/// </summary>
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReadProviderUpdateRegistrarTransaction()
		{
			var proUpRegTx = new Dash.ProviderUpdateRegistrarTransaction(Encoders.Hex.DecodeData(
				"01004f0fd120ac35429cdc616e470c53a52e032bba22304f8d1c54cc0af2040c3362000018ece819b998a36a185e323a8749e55fd3dc2e259b741f8580fbd68cbd9f51d30f4d4da34fd5afc71859dca3cf10fbda8a94fb062049b841f716dcded8257a3632fb053c1976a914f25c59be48ee1c4fd3733ecf56f440659f1d6c5088acb309a51267451a7f52e79ef2391aa952e9a0284e8fd8db56cdcae3b49b7e6dab4120c838c08b9492c5039444cac11e466df3609c585010fab636de75c687bab9f6154d9a7c26d7b5384a147fc67ddb2e66e5f773af73dbf818109aec692ed364eafd"));
			Assert.Equal(1, proUpRegTx.Version);
			Assert.Equal("62330c04f20acc541c8d4f3022ba2b032ea5530c476e61dc9c4235ac20d10f4f", proUpRegTx.ProTXHash.ToString());
			Assert.Equal(0, proUpRegTx.Mode);
			Assert.Equal("18ece819b998a36a185e323a8749e55fd3dc2e259b741f8580fbd68cbd9f51d30f4d4da34fd5afc71859dca3cf10fbda", Encoders.Hex.EncodeData(proUpRegTx.PubKeyOperator));
			Assert.Equal("3c05fb32367a25d8dedc16f741b8492006fb948a", proUpRegTx.KeyIdVoting.ToString());
			Assert.Equal("76a914f25c59be48ee1c4fd3733ecf56f440659f1d6c5088ac", Encoders.Hex.EncodeData(proUpRegTx.ScriptPayout.ToBytes()));
			Assert.Equal("ab6d7e9bb4e3cacd56dbd88f4e28a0e952a91a39f29ee7527f1a456712a509b3", proUpRegTx.InputsHash.ToString());
			Assert.Equal("20c838c08b9492c5039444cac11e466df3609c585010fab636de75c687bab9f6154d9a7c26d7b5384a147fc67ddb2e66e5f773af73dbf818109aec692ed364eafd", Encoders.Hex.EncodeData(proUpRegTx.PayloadSig));
		}

		/// <summary>
		/// https://github.com/dashevo/dashcore-lib/blob/master/test/transaction/payload/proregtxpayload.js
		/// </summary>
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReadProviderUpdateRevocationTransaction()
		{
			var proUpRevTx = new Dash.ProviderUpdateRevocationTransaction(Encoders.Hex.DecodeData(
				"01006f8a813df204873df003d6efc44e1906eaf6180a762513b1c91252826ce05916010082cf248cf6b8ac6a3cdc826edae582ead20421659ed891f9d4953a540616fb4f05279584b3339ed2ba95711ad28b18ee2878c4a904f76ea4d103e1d739f22ff7e3b9b3db7d0c4a7e120abb4952c3574a18de34fa29828f9fe3f52bd0b1fac17acd04f7751967d782045ab655053653438f1dd1e14ba6adeb8351b78c9eb59bf4"));
			Assert.Equal(1, proUpRevTx.Version);
			Assert.Equal("1659e06c825212c9b11325760a18f6ea06194ec4efd603f03d8704f23d818a6f", proUpRevTx.ProTXHash.ToString());
			Assert.Equal(1, proUpRevTx.Reason);
			Assert.Equal("4ffb1606543a95d4f991d89e652104d2ea82e5da6e82dc3c6aacb8f68c24cf82", proUpRevTx.InputsHash.ToString());
			Assert.Equal("05279584b3339ed2ba95711ad28b18ee2878c4a904f76ea4d103e1d739f22ff7e3b9b3db7d0c4a7e120abb4952c3574a18de34fa29828f9fe3f52bd0b1fac17acd04f7751967d782045ab655053653438f1dd1e14ba6adeb8351b78c9eb59bf4", Encoders.Hex.EncodeData(proUpRevTx.PayloadSig));
		}

		/// <summary>
		/// https://github.com/dashevo/dashcore-lib/blob/master/test/transaction/payload/coinbasepayload.js
		/// </summary>
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReadCoinbaseSpecialTransaction()
		{
			var cbTx = new Dash.CoinbaseSpecialTransaction(Encoders.Hex.DecodeData("0a0014000000b90100b3ccd30f16aa6aadf553fba6be9320d0002ed01c2f54d4975706763ce8"));
			Assert.Equal(10, cbTx.Version);
			Assert.Equal((uint)20, cbTx.Height);
			Assert.Equal("e83c76065797d4542f1cd02e00d02093bea6fb53f5ad6aaa160fd3ccb30001b9", cbTx.MerkleRootMNList.ToString());
		}
	
		/// <summary>
		/// https://github.com/dashevo/dashcore-lib/blob/master/test/transaction/payload/commitmenttxpayload.js
		/// </summary>
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanReadQuorumCommitmentTransaction()
		{
			var qcTx = new Dash.QuorumCommitmentTransaction(Encoders.Hex.DecodeData(
				"01001e430400010001f2a1f356b9e086220d38754b1de1e4dcbd8b080c3fa0a62c2bd0961400000000320000000000000032000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"));
			Assert.Equal(1, qcTx.Version);
			Assert.Equal((uint)279326, qcTx.Height);
			Assert.Equal(1, qcTx.Commitment.QfcVersion);
			Assert.Equal(1, qcTx.Commitment.LlmqType);
			Assert.Equal("000000001496d02b2ca6a03f0c088bbddce4e11d4b75380d2286e0b956f3a1f2",
				qcTx.Commitment.QuorumHash.ToString());
			Assert.Equal((uint)50, qcTx.Commitment.SignersSize);
			Assert.Equal("00000000000000", Encoders.Hex.EncodeData(qcTx.Commitment.Signers));
			Assert.Equal((uint)50, qcTx.Commitment.ValidMembersSize);
			Assert.Equal("00000000000000",
				Encoders.Hex.EncodeData(qcTx.Commitment.ValidMembers));
			Assert.Equal(
				"000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
				Encoders.Hex.EncodeData(qcTx.Commitment.QuorumPublicKey));
			Assert.Equal("0000000000000000000000000000000000000000000000000000000000000000",
				qcTx.Commitment.QuorumVvecHash.ToString());
			Assert.Equal(
				"000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
				Encoders.Hex.EncodeData(qcTx.Commitment.QuorumSig));
			Assert.Equal(
				"000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000",
				Encoders.Hex.EncodeData(qcTx.Commitment.Sig));
		}
	
		/// <summary>
		/// Check with debug console in Dash Testnet (block 7000 is the first with special transactions,
		/// all blocks prior to 7000 are standard transactions in testnet):
		/// getblockhash 7000
		/// getblock 0000001e8b87719178a7b2b86def5ff918285fa9993e561f2ef04da18c6044bf 1
		/// getrawtransaction 61bc32c39a523a7705eaa52724b80d52741025547d6562d9fc588617790a06ce 1
		/// </summary>
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CheckDashBlock7000FirstTransaction()
		{
			const string Hex =
				"03000500010000000000000000000000000000000000000000000000000000000000000000ffffffff1202581b0e2f5032506f6f6c2d74444153482fffffffff044d125b96010000001976a9144f79c383bc5d3e9d4d81b98f87337cedfa78953688ac40c3609a010000001976a914bafef41416718b231d5ca0143dccbc360d06b77688acf3b00504000000001976a914badadfdebaa6d015a0299f23fbc1fcbdd72ba96f88ac00000000000000002a6a28421df280ea438199057112738c5149e4307689d1201c96a66fbe83f4aa0c4016000000000300000000000000260100581b00000000000000000000000000000000000000000000000000000000000000000000";
			Dash.DashTransaction tx = new Dash.DashTransaction();
			tx.ReadWrite(new BitcoinStream(Encoders.Hex.DecodeData(Hex)));
			//"version": 3,
			//"type": 5,
			Assert.Equal((uint)0x50003, tx.Version);
			Assert.Equal((uint)3, tx.DashVersion);
			Assert.Equal(Dash.DashTransactionType.MasternodeListMerkleProof, tx.DashType);
			//"locktime": 0,
			Assert.Equal(0, tx.LockTime.Height);
			//"vin": [.]
			Assert.Single(tx.Inputs);
			Assert.Equal(4294967295, tx.Inputs[0].Sequence.Value);
			//"vout": [.]
			Assert.Equal(4, tx.Outputs.Count);
			Assert.Equal(6817518157, tx.Outputs[0].Value.Satoshi);
			Assert.True(tx.Outputs[0].ScriptPubKey.IsValid);
			Assert.True(tx.IsCoinBase);
			//"extraPayloadSize": 38,
			Assert.Equal(38, tx.ExtraPayload.Length);
			Assert.Equal("0100581b00000000000000000000000000000000000000000000000000000000000000000000", Encoders.Hex.EncodeData(tx.ExtraPayload));
			//"cbTx": {
			//	"version": 1,
			//	"height": 7000,
			//	"merkleRootMNList": "0000000000000000000000000000000000000000000000000000000000000000"
			//},
			Assert.NotNull(tx.CbTx);
			Assert.Equal(1, tx.CbTx.Version);
			Assert.Equal((uint)7000, tx.CbTx.Height);
			Assert.Equal("0000000000000000000000000000000000000000000000000000000000000000", tx.CbTx.MerkleRootMNList.ToString());
			// Make sure block can be written too and hex stays the same
			Assert.Equal(Hex, tx.ToHex());
		}

		/*for manually testing via running Dash Testnet node
		public DashTests(ITestOutputHelper output)
		{
			this.output = output;
		}

		private readonly ITestOutputHelper output;

		[Fact(DisplayName = "Enable manually to test when having local Dash testnet running")]
		public async Task ConnectToDashTestnetAndCheckBlock7000()
		{
			var connectionString = new RPCCredentialString();
			connectionString.UserPassword = new NetworkCredential("TestDash", "TestLocal");
			connectionString.Server = "http://localhost:19998";
			var client = new RPCClient(connectionString, Dash.Instance.Testnet);
			Assert.True(await client.GetBlockCountAsync() > 7000);
			await GrabAndOutputBlock(client, 6999);
			await GrabAndOutputBlock(client, 7000);
		}
		
		private async Task GrabAndOutputBlock(RPCClient client, int blockNumber)
		{
			var block = await client.GetBlockAsync(blockNumber);
			output.WriteLine("Block " + blockNumber + " header: " +
				JsonConvert.SerializeObject(block.Header));
			if (blockNumber == 7000)
				CheckDashBlock7000((Dash.DashBlock)block);
			Assert.True(block.Check());
			foreach (var tx in block.Transactions)
				output.WriteLine("Tx: " + tx);
		}

		/// <summary>
		/// Check with debug console:
		/// getblockhash 7000
		/// getblock 0000001e8b87719178a7b2b86def5ff918285fa9993e561f2ef04da18c6044bf 1
		/// </summary>
		private void CheckDashBlock7000(Dash.DashBlock block)
		{
			//"hash": "0000001e8b87719178a7b2b86def5ff918285fa9993e561f2ef04da18c6044bf",
			Assert.Equal("0000001e8b87719178a7b2b86def5ff918285fa9993e561f2ef04da18c6044bf", block.Header.GetHash().ToString());
			//"height": 7000,
			Assert.Equal(7000, block.GetCoinbaseHeight());
			//"version": 536870913,
			Assert.Equal(536870913, block.Header.Version);
			//"versionHex": "20000001",
			Assert.Equal(0x20000001, block.Header.Version);
			//"merkleroot": "5339c3b37049fc41c1e75194d36b8958dea0134cec42c5715bdae36cde87eff2",
			Assert.Equal("5339c3b37049fc41c1e75194d36b8958dea0134cec42c5715bdae36cde87eff2",
				block.Header.HashMerkleRoot.ToString());
			//"time": 1545087335,
			Assert.Equal(1545087335, block.Header.BlockTime.ToUnixTimeSeconds());
			//"mediantime": 1545086480,
			Assert.Equal((uint)767772160, block.Header.Nonce);
			//"difficulty": 0.02184410439171994,
			Assert.Equal(0.021844104391, block.Header.Bits.Difficulty);
			Assert.True(block.Header.CheckProofOfWork());
			//"tx": [..]
			Assert.False(block.HeaderOnly);
			Assert.Equal(2, block.Transactions.Count);
			//see above: CheckDashBlock7000FirstTransaction((Dash.DashTransaction)block.Transactions[0]);
			CheckDashBlock7000SecondTransaction((Dash.DashTransaction)block.Transactions[1]);
			// Check if transaction hashes leave us with the correct merkel root
			Assert.Equal(block.Header.HashMerkleRoot, block.GetMerkleRoot().Hash);
		}
	
		private void CheckDashBlock7000SecondTransaction(Dash.DashTransaction tx)
		{
			//"version": 3,
			//"type": 6,
			Assert.Equal((uint)0x60003, tx.Version);
			Assert.Equal((ushort)3, tx.DashVersion);
			Assert.Equal(Dash.DashTransactionType.QuorumCommitment, tx.DashType);
			//"locktime": 0,
			Assert.Equal(0, tx.LockTime.Height);
			//"vin": []
			Assert.Empty(tx.Inputs);
			//"vout": []
			Assert.Empty(tx.Outputs);
			//"extraPayloadSize": 38,
			Assert.Equal(329, tx.ExtraPayload.Length);
			Assert.Equal("0100581b0000010001e3aeae4a2d013f6bdd3525318bcc579f95c3420e8897a23e8a479f1c39000000320000000000000032000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", Encoders.Hex.EncodeData(tx.ExtraPayload));
			//"qcTx": { ... }
			Assert.NotNull(tx.QcTx);
			Assert.Equal(1, tx.QcTx.Version);
			Assert.Equal((uint)7000, tx.QcTx.Height);
			Assert.Equal(1, tx.QcTx.Commitment.QfcVersion);
			Assert.Equal(1, tx.QcTx.Commitment.LlmqType);
			Assert.Equal("000000391c9f478a3ea297880e42c3959f57cc8b312535dd6b3f012d4aaeaee3", tx.QcTx.Commitment.QuorumHash.ToString());
			Assert.Equal((uint)50, tx.QcTx.Commitment.SignersSize);
			Assert.Equal((uint)50, tx.QcTx.Commitment.ValidMembersSize);
			Assert.Equal("000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", Encoders.Hex.EncodeData(tx.QcTx.Commitment.QuorumPublicKey));
			Assert.Equal("03000600000000000000fd49010100581b0000010001e3aeae4a2d013f6bdd3525318bcc579f95c3420e8897a23e8a479f1c39000000320000000000000032000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", tx.ToHex());
		}
		*/
	}
}
