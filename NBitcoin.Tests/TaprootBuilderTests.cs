using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace NBitcoin.Tests
{
	public class TaprootBuilderTests
	{
		private readonly ITestOutputHelper _testOutputHelper;
		public TaprootBuilderTests(ITestOutputHelper testOutputHelper)
		{
			_testOutputHelper = testOutputHelper;
		}
#if HAS_SPAN
		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TestVectorsCore()
		{
			var internalKey = TaprootInternalPubKey.Parse("93c7378d96518a75448821c4f7c8f4bae7ce60f804d03d1f0628dd5dd0f5de51");

			var scriptWeights = new[]
			{
				(10u, Script.FromHex("51")),
				(20u, Script.FromHex("52")),
				(20u, Script.FromHex("53")),
				(30u, Script.FromHex("54")),
				(19u, Script.FromHex("55")),
			};

			var treeInfo = TaprootSpendInfo.WithHuffmanTree(internalKey, scriptWeights);
			/* The resulting tree should put the scripts into a tree similar
			 * to the following:
			 *
			 *   1      __/\__
			 *         /      \
			 *        /\     / \
			 *   2   54 52  53 /\
			 *   3            55 51
			 */
			var expected = new[] { ("51", 3), ("52", 2), ("53", 2), ("54", 2), ("55", 3) };
			foreach (var t in expected)
			{
				var (script, expectedLength) = t;
				Assert.True(
					treeInfo
						.ScriptToMerkleProofMap!
						.TryGetValue((Script.FromHex(script), (byte)TaprootConstants.TAPROOT_LEAF_TAPSCRIPT), out var scriptSet)
				);
				var actualLength = scriptSet[0];
				Assert.Equal(expectedLength, actualLength.Count);
			}

			var outputKey = treeInfo.OutputPubKey;

			foreach (var (_, script) in scriptWeights)
			{
				var ctrlBlock = treeInfo.GetControlBlock(script, (byte)TaprootConstants.TAPROOT_LEAF_TAPSCRIPT);
				Assert.True(ctrlBlock.VerifyTaprootCommitment(outputKey, script));
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void TaptreeBuilderTests()
		{
			var internalPubKey = TaprootInternalPubKey.Parse("93c7378d96518a75448821c4f7c8f4bae7ce60f804d03d1f0628dd5dd0f5de51");
			var builder = new TaprootBuilder();
			// Create a tree as shown below
			// For example, imagine this tree:
			// A, B , C are at depth 2 and D,E are at 3
			//                                       ....
			//                                     /      \
			//                                    /\      /\
			//                                   /  \    /  \
			//                                  A    B  C  / \
			//                                            D   E
			var a = Script.FromHex("51");
			var b = Script.FromHex("52");
			var c = Script.FromHex("53");
			var d = Script.FromHex("54");
			var e = Script.FromHex("55");
			builder
				.AddLeaf(2, a)
				.AddLeaf(2, b)
				.AddLeaf(2, c)
				.AddLeaf(3, d);

			// Trying to finalize an incomplete tree throws Exception(
			Assert.Throws<InvalidOperationException>(() => builder.Finalize(internalPubKey));
			builder.AddLeaf(3, e);
			var treeInfo = builder.Finalize(internalPubKey);
			var outputKey = treeInfo.OutputPubKey;
			foreach (var script in new[] { a, b, c, d, e })
			{
				var ctrlBlock = treeInfo.GetControlBlock(script, (byte)TaprootConstants.TAPROOT_LEAF_TAPSCRIPT);
				Assert.True(ctrlBlock.VerifyTaprootCommitment(outputKey, script));
			}
		}

		private TaprootBuilder ProcessScriptTrees(JToken v, TaprootBuilder builder, List<(Script, byte)> leaves,
			uint depth)
		{

			if (v is null) return builder;
			if (v is JArray arr)
			{
				foreach (var leaf in arr)
				{
					builder = ProcessScriptTrees(leaf, builder, leaves, depth + 1);
				}
			}
			else
			{
				var script = Script.FromHex(v["script"].ToString());
				var ver = (byte)(ulong)v["leafVersion"];
				leaves.Add((script, ver));
				builder = builder.AddLeaf(depth, script, ver);
			}
			return builder;
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void CanHandleMultipleProofForSameScript()
		{
			var internalKey = TaprootInternalPubKey.Parse("93c7378d96518a75448821c4f7c8f4bae7ce60f804d03d1f0628dd5dd0f5de51");
			var data = new byte[] { 0x01 };
			var sc = new Script(OpcodeType.OP_RETURN, Op.GetPushOp(data));
			var version = (byte)TaprootConstants.TAPROOT_LEAF_TAPSCRIPT;
			var nodeInfoA = NodeInfo.NewLeafWithVersion(sc, version);
			var nodeInfoB = NodeInfo.NewLeafWithVersion(sc, version);
			var rootNode = nodeInfoA + nodeInfoB;
			var info = TaprootSpendInfo.FromNodeInfo(internalKey, rootNode);
			Assert.Single(info.ScriptToMerkleProofMap);
			Assert.True(info.ScriptToMerkleProofMap.TryGetValue((sc, version), out var proofs));
			Assert.Equal(2, proofs.Count);
		}

		[Fact]
		[Trait("Core", "Core")]
		public void BIP341Tests()
		{
			var data = JObject.Parse(File.ReadAllText("data/bip-0341/wallet-test-vectors.json"));

			foreach (var arr in data["scriptPubKey"]!.ToArray())
			{
				var internalKey = TaprootInternalPubKey.Parse(arr["given"]!["internalPubkey"]!.ToString());
				var scriptTree = arr["given"]["scriptTree"];
				if (!scriptTree.HasValues)
				{
					Assert.False(arr["intermediary"]!["merkleRoot"]!.HasValues);
				}
				else
				{
					var merkleRootHex = arr["intermediary"]!["merkleRoot"]!.ToString();
					Assert.NotNull(merkleRootHex);
					_testOutputHelper.WriteLine($"scriptTree: {scriptTree}, merkleRoot {merkleRootHex}");
					var merkleRoot =
						new uint256(Encoders.Hex.DecodeData( merkleRootHex));
					var leafHashes = arr["intermediary"]!["leafHashes"]!.ToArray();
					var ctrlBlocks = arr["expected"]!["scriptPathControlBlocks"]!.ToArray();
					var builder = new TaprootBuilder();
					var leaves = new List<(Script, byte)>();
					builder = ProcessScriptTrees(scriptTree, builder, leaves, 0);
					var spendInfo = builder.Finalize(internalKey);
					foreach (var (i, (script, version)) in leaves.Select((v, i) => (i, v)))
					{
						var expectedLeafHash = new uint256(Encoders.Hex.DecodeData(leafHashes[i].ToString()));
						var expectedControlBlockStr = ctrlBlocks[i].ToString();
						var expectedControlBlock = ControlBlock.FromHex(expectedControlBlockStr);
						var leafHash = script.TaprootLeafHash(version);
						var ctrlBlock = spendInfo.GetControlBlock(script, version);
						Assert.Equal(expectedLeafHash, leafHash);
						var ctrlStr = Encoders.Hex.EncodeData(ctrlBlock.ToBytes());
						_testOutputHelper.WriteLine($"Control block str {ctrlStr}. expected {expectedControlBlockStr}");
						Assert.Equal(expectedControlBlock, ctrlBlock);
						Assert.Equal(expectedControlBlockStr, ctrlStr);
					}

					var expectedOutputKey = TaprootPubKey.Parse(arr["intermediary"]!["tweakedPubkey"]!.ToString());
					var expectedTweak = arr["intermediary"]!["tweak"]!.ToString();
					var expectedSpk = Script.FromHex(arr["expected"]!["scriptPubKey"]!.ToString());
					var expectedAddr = arr["expected"]!["bip350Address"]!.ToString();

					var tweak = new byte [32];
					TaprootFullPubKey.ComputeTapTweak(internalKey, merkleRoot, tweak);
					var tweakHex = DataEncoders.Encoders.Hex.EncodeData(tweak);
					var outputKey = internalKey.GetTaprootFullPubKey(merkleRoot);
					var addr = new TaprootAddress(outputKey, Network.Main);
					var spk = addr.ScriptPubKey;
					Assert.Equal(outputKey, expectedOutputKey);
					Assert.Equal(tweakHex, expectedTweak);
					Assert.Equal(addr.ToString(), expectedAddr);
					Assert.Equal(spk, expectedSpk);
				}
			}
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void ScriptPathSpendUnitTest1()
		{
			var builder = new TaprootBuilder();
			builder
				.AddLeaf(1, Script.FromHex("210203a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5ac"))
				.AddLeaf(2, Script.FromHex("2102a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5bdac"))
				.AddLeaf(2, Script.FromHex("210203a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5ac"));

			var internalKey = TaprootInternalPubKey.Parse("03a34b99f22c790c4e36b2b3c2c35a36db06226e41c692fc82b8b56ac1c540c5");
			var info = builder.Finalize(internalKey);
			var expectedMerkleRoot =
				new uint256(
					Encoders.Hex.DecodeData("e5e302a2955fdd97403d9cfd15b86a4e7d4e17e0ff0a86baa2e02f5afdbad1b5"));
			Assert.Equal(info.MerkleRoot!, expectedMerkleRoot);

			var output = internalKey.GetTaprootFullPubKey(info.MerkleRoot);
			Assert.Equal(info.OutputPubKey.OutputKey.ToString(), "003cdb72825a12ea62f5834f3c47f9bf48d58d27f5ad1e6576ac613b093125f3");
			var spk = output.ScriptPubKey;
			var expectedSpk = ("51201497ae16f30dacb88523ed9301bff17773b609e8a90518a3f96ea328a47d1500");
			Assert.Equal( expectedSpk, spk.ToHex());
		}
	}
#endif
}
