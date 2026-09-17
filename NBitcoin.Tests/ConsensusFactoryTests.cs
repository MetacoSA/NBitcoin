using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using NBitcoin.RPC;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class ConsensusFactoryTests
	{
		private const uint ExtensionValue = 0xa1b2c3d4;
		private const uint MarkerValue = 0x10203040;
		private static readonly Network TestNetwork = CreateNetwork();

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void FactoryCreatesCustomBlockTypes()
		{
			Assert.IsType<TestBlockHeader>(TestConsensusFactory.Instance.CreateBlockHeader());
			var block = Assert.IsType<TestBlock>(TestConsensusFactory.Instance.CreateBlock());
			Assert.IsType<TestBlockHeader>(block.Header);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void BlockHeaderParseUsesConsensusFactory()
		{
			var expected = CreateHeader();
			var hex = Encoders.Hex.EncodeData(expected.ToBytes());
			var network = TestNetwork;

			AssertHeaderRoundTrip(expected, BlockHeader.Parse(hex, TestConsensusFactory.Instance));
			AssertHeaderRoundTrip(expected, BlockHeader.Parse(hex, network.Consensus));
			AssertHeaderRoundTrip(expected, BlockHeader.Parse(hex, network));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void BlockParseAndLoadUseConsensusFactory()
		{
			var expected = CreateBlock();
			var bytes = expected.ToBytes();
			var hex = Encoders.Hex.EncodeData(bytes);
			var network = TestNetwork;

			AssertBlockRoundTrip(expected, Block.Parse(hex, TestConsensusFactory.Instance));
			AssertBlockRoundTrip(expected, Block.Parse(hex, network.Consensus));
			AssertBlockRoundTrip(expected, Block.Parse(hex, network));
			AssertBlockRoundTrip(expected, Block.Load(bytes, TestConsensusFactory.Instance));
			AssertBlockRoundTrip(expected, Block.Load(bytes, network.Consensus));
			AssertBlockRoundTrip(expected, Block.Load(bytes, network));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void BitcoinStreamUsesConsensusFactoryForBlocksAndHeaders()
		{
			var expectedHeader = CreateHeader();
			BlockHeader parsedHeader = null;
			var headerStream = new BitcoinStream(expectedHeader.ToBytes())
			{
				ConsensusFactory = TestConsensusFactory.Instance
			};
			headerStream.ReadWrite(ref parsedHeader);
			AssertHeaderRoundTrip(expectedHeader, parsedHeader);

			var expectedBlock = CreateBlock();
			Block parsedBlock = null;
			var blockStream = new BitcoinStream(expectedBlock.ToBytes())
			{
				ConsensusFactory = TestConsensusFactory.Instance
			};
			blockStream.ReadWrite(ref parsedBlock);
			AssertBlockRoundTrip(expectedBlock, parsedBlock);
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void HeadersPayloadUsesConsensusFactory()
		{
			var expected = CreateHeader();
			var payload = new HeadersPayload(expected);
			var bytes = Serialize(payload, TestConsensusFactory.Instance);
			var parsed = new HeadersPayload();

			Deserialize(parsed, bytes, TestConsensusFactory.Instance);

			AssertHeaderRoundTrip(expected, Assert.Single(parsed.Headers));
			Assert.Equal(bytes, Serialize(parsed, TestConsensusFactory.Instance));
		}

		[Fact]
		[Trait("Protocol", "Protocol")]
		public void BlockPayloadUsesConsensusFactory()
		{
			var expected = CreateBlock();
			var payload = new BlockPayload(expected);
			var bytes = Serialize(payload, TestConsensusFactory.Instance);
			var parsed = new BlockPayload();

			Deserialize(parsed, bytes, TestConsensusFactory.Instance);

			AssertBlockRoundTrip(expected, parsed.Object);
			Assert.Equal(bytes, Serialize(parsed, TestConsensusFactory.Instance));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public async Task RestClientUsesConsensusFactoryForBlockHeaders()
		{
			var first = CreateHeader(ExtensionValue, MarkerValue);
			var second = CreateHeader(ExtensionValue + 1, MarkerValue + 1);
			var response = first.ToBytes().Concat(second.ToBytes()).ToArray();
			var client = CreateRestClient(response);

			var headers = (await client.GetBlockHeadersAsync(uint256.Zero, 2)).ToArray();

			Assert.Collection(headers,
				header => AssertHeaderRoundTrip(first, header),
				header => AssertHeaderRoundTrip(second, header));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public async Task RestClientRejectsTruncatedBlockHeaders()
		{
			var header = CreateHeader().ToBytes();
			var truncatedHeader = header.Take(header.Length - 1).ToArray();

			Assert.Empty(await CreateRestClient(Array.Empty<byte>()).GetBlockHeadersAsync(uint256.Zero, 1));
			await Assert.ThrowsAsync<EndOfStreamException>(() =>
				CreateRestClient(truncatedHeader).GetBlockHeadersAsync(uint256.Zero, 1));
			await Assert.ThrowsAsync<EndOfStreamException>(() =>
				CreateRestClient(header.Concat(truncatedHeader).ToArray()).GetBlockHeadersAsync(uint256.Zero, 2));
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public async Task RPCClientPreservesConsensusFactoryWhenParsingBlockHeaders()
		{
			var expected = CreateHeader();
			var response = $"{{\"result\":\"{Encoders.Hex.EncodeData(expected.ToBytes())}\",\"error\":null,\"id\":1}}";
			var credentials = new RPCCredentialString
			{
				UserPassword = new NetworkCredential("user", "password")
			};
			var client = new RPCClient(credentials, new Uri("http://127.0.0.1:18443"), TestNetwork)
			{
				HttpClient = new HttpClient(new StaticResponseHandler(Encoding.UTF8.GetBytes(response), "application/json"))
			};

			var parsed = await client.GetBlockHeaderAsync(uint256.Zero);

			AssertHeaderRoundTrip(expected, parsed);
		}

		[Fact]
		[Trait("UnitTest", "UnitTest")]
		public void BitcoinBlockHeaderParseIsUnchanged()
		{
			var expected = Network.Main.GetGenesis().Header;
			var hex = Encoders.Hex.EncodeData(expected.ToBytes());

			var parsed = BlockHeader.Parse(hex, Network.Main);

			Assert.IsType<BlockHeader>(parsed);
			Assert.Equal(expected.GetHash(), parsed.GetHash());
			Assert.Equal(expected.ToBytes(), parsed.ToBytes());
		}

		private static TestBlockHeader CreateHeader(uint extension = ExtensionValue, uint marker = MarkerValue)
		{
			return new TestBlockHeader
			{
				Version = 2,
				HashPrevBlock = new uint256(1),
				HashMerkleRoot = new uint256(2),
				BlockTime = DateTimeOffset.FromUnixTimeSeconds(1234567890),
				Bits = new Target(0x1d00ffff),
				Nonce = 42,
				Extension = extension,
				Marker = new FactoryMarker { Value = marker, CreatedByFactory = true }
			};
		}

		private static TestBlock CreateBlock()
		{
			var block = (TestBlock)TestConsensusFactory.Instance.CreateBlock();
			block.Header = CreateHeader();
			var transaction = TestConsensusFactory.Instance.CreateTransaction();
			transaction.Inputs.Add(new TxIn());
			transaction.Outputs.Add(new TxOut());
			block.Transactions.Add(transaction);
			return block;
		}

		private static Network CreateNetwork()
		{
			var consensus = Network.Main.Consensus.Clone();
			consensus.ConsensusFactory = TestConsensusFactory.Instance;
			var genesis = TestConsensusFactory.Instance.CreateBlock();
			var builder = new NetworkBuilder();
			builder.CopyFrom(Network.Main);
			builder.SetConsensus(consensus);
			builder.SetGenesis(Encoders.Hex.EncodeData(genesis.ToBytes()));
			builder.SetName($"consensus-factory-tests-{Guid.NewGuid():N}");
			return builder.BuildAndRegister();
		}

		private static RestClient CreateRestClient(byte[] response)
		{
			return new RestClient(new Uri("http://127.0.0.1:18443"), TestNetwork)
			{
				HttpClient = new HttpClient(new StaticResponseHandler(response))
			};
		}

		private static byte[] Serialize(IBitcoinSerializable value, ConsensusFactory consensusFactory)
		{
			using var memory = new MemoryStream();
			value.ReadWrite(memory, true, consensusFactory);
			return memory.ToArray();
		}

		private static void Deserialize(IBitcoinSerializable value, byte[] bytes, ConsensusFactory consensusFactory)
		{
			using var memory = new MemoryStream(bytes);
			value.ReadWrite(memory, false, consensusFactory);
		}

		private static void AssertHeaderRoundTrip(TestBlockHeader expected, BlockHeader actual)
		{
			var header = Assert.IsType<TestBlockHeader>(actual);
			Assert.Equal(expected.Extension, header.Extension);
			Assert.Equal(expected.Marker.Value, header.Marker.Value);
			Assert.True(header.Marker.CreatedByFactory);
			Assert.Equal(expected.ToBytes(), header.ToBytes());
		}

		private static void AssertBlockRoundTrip(TestBlock expected, Block actual)
		{
			var block = Assert.IsType<TestBlock>(actual);
			AssertHeaderRoundTrip((TestBlockHeader)expected.Header, block.Header);
			Assert.Equal(expected.ToBytes(), block.ToBytes());
		}

		private sealed class StaticResponseHandler : HttpMessageHandler
		{
			private readonly byte[] _response;
			private readonly string _contentType;

			public StaticResponseHandler(byte[] response, string contentType = "application/octet-stream")
			{
				_response = response;
				_contentType = contentType;
			}

			protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				var response = new HttpResponseMessage(HttpStatusCode.OK)
				{
					Content = new ByteArrayContent(_response)
				};
				response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(_contentType);
				return Task.FromResult(response);
			}
		}

		private sealed class FactoryMarker : IBitcoinSerializable
		{
			public bool CreatedByFactory { get; set; }
			public uint Value { get; set; }

			public void ReadWrite(BitcoinStream stream)
			{
				var value = Value;
				stream.ReadWrite(ref value);
				Value = value;
			}
		}

		private sealed class TestBlockHeader : BlockHeader
		{
#pragma warning disable CS0618 // Type or member is obsolete
			public TestBlockHeader()
#pragma warning restore CS0618 // Type or member is obsolete
			{
			}

			public uint Extension { get; set; }
			public FactoryMarker Marker { get; set; }

			public override void ReadWrite(BitcoinStream stream)
			{
				base.ReadWrite(stream);
				var extension = Extension;
				stream.ReadWrite(ref extension);
				Extension = extension;
				var marker = Marker;
				stream.ReadWrite(ref marker);
				Marker = marker;
			}
		}

		private sealed class TestBlock : Block
		{
#pragma warning disable CS0618 // Type or member is obsolete
			public TestBlock(TestBlockHeader header) : base(header)
#pragma warning restore CS0618 // Type or member is obsolete
			{
			}

			public override ConsensusFactory GetConsensusFactory()
			{
				return TestConsensusFactory.Instance;
			}
		}

		private sealed class TestConsensusFactory : ConsensusFactory
		{
			public static TestConsensusFactory Instance { get; } = new TestConsensusFactory();

			public override bool TryCreateNew(Type type, out IBitcoinSerializable result)
			{
				if (type == typeof(FactoryMarker))
				{
					result = new FactoryMarker { CreatedByFactory = true };
					return true;
				}
				return base.TryCreateNew(type, out result);
			}

			public override BlockHeader CreateBlockHeader()
			{
				return new TestBlockHeader();
			}

			public override Block CreateBlock()
			{
				return new TestBlock(new TestBlockHeader());
			}
		}
	}
}
