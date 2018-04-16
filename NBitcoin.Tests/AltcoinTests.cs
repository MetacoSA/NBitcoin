using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	[Trait("Altcoins", "Altcoins")]
	public class AltcoinTests
	{
		[Fact]
		private async Task LoadChainFromNode()
		{
			var _Network = NBitcoin.Altcoins.BitcoinGold.Mainnet;
			var _Chain = new ConcurrentChain(_Network);
			CancellationToken cancellation = new CancellationTokenSource().Token;
			var userAgent = "NBXplorer-" + RandomUtils.GetInt64();
			bool handshaked = false;
			using (var handshakeTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
			{
				try
				{
					handshakeTimeout.CancelAfter(TimeSpan.FromSeconds(10));
					using (var node = Node.Connect(_Network, new IPEndPoint(IPAddress.Parse("52.169.220.127"), 8081), new NodeConnectionParameters()
					{
						UserAgent = userAgent,
						ConnectCancellation = handshakeTimeout.Token,
						IsRelay = false
					}))
					{
						node.VersionHandshake(handshakeTimeout.Token);
						handshaked = true;
						var	loadChainTimeout = TimeSpan.FromDays(7); // unlimited

						var synchronizeOptions = new SynchronizeChainOptions()
						{
							SkipPoWCheck = true,
							StripHeaders = true
						};

						try
						{
							using (var cts1 = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
							{
								cts1.CancelAfter(loadChainTimeout);
								node.SynchronizeChain(_Chain, synchronizeOptions, cancellationToken: cts1.Token);
							}
						}
						catch // Timeout happens with SynchronizeChain, if so, throw away the cached chain
						{
							_Chain.SetTip(_Chain.Genesis);
							node.SynchronizeChain(_Chain, synchronizeOptions, cancellationToken: cancellation);
						}

					}
				}
				catch (OperationCanceledException) when (!handshaked && handshakeTimeout.IsCancellationRequested)
				{
					throw;
				}
			}
		}


		[Fact]
		public void HasCorrectGenesisBlock()
		{
			using(var builder = NodeBuilderEx.Create())
			{
				var rpc = builder.CreateNode().CreateRPCClient();
				builder.StartAll();
				var actual = (rpc.GetBlock(0)).GetHash();
				Assert.Equal(builder.Network.GetGenesis().GetHash(), actual);
			}
		}

		[Fact]
		public void CanParseBlock()
		{
			using(var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				var rpc = node.CreateRPCClient();
				rpc.Generate(100);
				var hash = rpc.GetBestBlockHash();
				Assert.NotNull(rpc.GetBlock(hash));
			}
		}

		[Fact]
		public void CanParseAddress()
		{
			using(var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				var addr = node.CreateRPCClient().SendCommand(RPC.RPCOperations.getnewaddress).Result.ToString();
				var addr2 = BitcoinAddress.Create(addr, builder.Network).ToString();
				Assert.Equal(addr, addr2);

				var address = new Key().PubKey.GetAddress(builder.Network);
				var isValid = ((JObject)node.CreateRPCClient().SendCommand("validateaddress", address.ToString()).Result)["isvalid"].Value<bool>();
				Assert.True(isValid);
			}
		}

		[Fact]
		public void CanSyncWithPoW()
		{
			using(var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				node.Generate(100);

				var nodeClient = node.CreateNodeClient();
				nodeClient.VersionHandshake();
				ConcurrentChain chain = new ConcurrentChain(builder.Network);
				nodeClient.SynchronizeChain(chain, new Protocol.SynchronizeChainOptions() { SkipPoWCheck = false });
				Assert.Equal(100, chain.Height);
			}
		}

		[Fact]
		public void CanSyncWithoutPoW()
		{
			using(var builder = NodeBuilderEx.Create())
			{
				var node = builder.CreateNode();
				builder.StartAll();
				node.Generate(100);
				var nodeClient = node.CreateNodeClient();
				nodeClient.VersionHandshake();
				ConcurrentChain chain = new ConcurrentChain(builder.Network);
				nodeClient.SynchronizeChain(chain, new Protocol.SynchronizeChainOptions() { SkipPoWCheck = true });
				Assert.Equal(100, chain.Height);

				// If it fails, override Block.GetConsensusFactory()
				var b = node.CreateRPCClient().GetBlock(50);
				Assert.Equal(b.WithOptions(TransactionOptions.Witness).Header.GetType(), chain.GetBlock(50).Header.GetType());

				var b2 = nodeClient.GetBlocks().ToArray()[50];
				Assert.Equal(b2.Header.GetType(), chain.GetBlock(50).Header.GetType());
			}
		}
	}
}
