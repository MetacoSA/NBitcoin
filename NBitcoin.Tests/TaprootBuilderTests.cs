using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
#endif
	}
}
