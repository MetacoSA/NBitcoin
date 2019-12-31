#if !NOSOCKET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin.Logging;

namespace NBitcoin.Protocol
{
	public delegate void NodeServerNodeEventHandler(NodeServer sender, Node node);
	public delegate void NodeServerMessageEventHandler(NodeServer sender, IncomingMessage message);
	public class NodeServer : IDisposable
	{
		private readonly Network _Network;
		public Network Network
		{
			get
			{
				return _Network;
			}
		}

		uint _Version;
		public uint Version
		{
			get
			{
				return _Version;
			}
		}

		/// <summary>
		/// The parameters that will be cloned and applied for each node connecting to the NodeServer
		/// </summary>
		public NodeConnectionParameters InboundNodeConnectionParameters
		{
			get;
			set;
		}

		public NodeServer(Network network, uint? version = null,
			int internalPort = -1)
		{
			AllowLocalPeers = true;
			InboundNodeConnectionParameters = new NodeConnectionParameters();
			internalPort = internalPort == -1 ? network.DefaultPort : internalPort;
			_LocalEndpoint = new IPEndPoint(IPAddress.Parse("0.0.0.0").MapToIPv6Ex(), internalPort);
			MaxConnections = 125;
			_Network = network;
			_ExternalEndpoint = new IPEndPoint(_LocalEndpoint.Address, Network.DefaultPort);
			_Version = version == null ? network.MaxP2PVersion : version.Value;
			var listener = new EventLoopMessageListener<IncomingMessage>(ProcessMessage);
			_MessageProducer.AddMessageListener(listener);
			OwnResource(listener);
			_ConnectedNodes = new NodesCollection();
			_ConnectedNodes.Added += _Nodes_NodeAdded;
			_ConnectedNodes.Removed += _Nodes_NodeRemoved;
			_ConnectedNodes.MessageProducer.AddMessageListener(listener);
		}


		public event NodeServerNodeEventHandler NodeRemoved;
		public event NodeServerNodeEventHandler NodeAdded;
		public event NodeServerMessageEventHandler MessageReceived;

		void _Nodes_NodeRemoved(object sender, NodeEventArgs node)
		{
			var removed = NodeRemoved;
			if (removed != null)
				removed(this, node.Node);
		}

		void _Nodes_NodeAdded(object sender, NodeEventArgs node)
		{
			var added = NodeAdded;
			if (added != null)
				added(this, node.Node);
		}

		public bool AllowLocalPeers
		{
			get;
			set;
		}

		public int MaxConnections
		{
			get;
			set;
		}


		private IPEndPoint _LocalEndpoint;
		public IPEndPoint LocalEndpoint
		{
			get
			{
				return _LocalEndpoint;
			}
			set
			{
				_LocalEndpoint = Utils.EnsureIPv6(value);
			}
		}

		Socket socket;

		public bool IsListening
		{
			get
			{
				return socket != null;
			}
		}

		public void Listen(int maxIncoming = 8)
		{
			if (socket != null)
				throw new InvalidOperationException("Already listening");


			using (Logs.NodeServer.BeginScope("Node server listening on {listeningOn}", LocalEndpoint))

			{
				try
				{
					socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
					socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

					socket.Bind(LocalEndpoint);
					socket.Listen(maxIncoming);
					Logs.NodeServer.LogInformation("Listening...");
					BeginAccept();
				}
				catch (Exception ex)
				{
					Logs.NodeServer.LogError(default, ex, "Error while opening the Protocol server");
					throw;
				}
			}
		}

		private void BeginAccept()
		{
			if (_Cancel.IsCancellationRequested)
			{
				Logs.NodeServer.LogInformation("Stop accepting connection...");
				return;
			}
			Logs.NodeServer.LogInformation("Accepting connection...");
			var args = new SocketAsyncEventArgs();
			args.Completed += Accept_Completed;
			if (!socket.AcceptAsync(args))
				EndAccept(args);
		}

		private void Accept_Completed(object sender, SocketAsyncEventArgs e)
		{
			EndAccept(e);
		}

		private void EndAccept(SocketAsyncEventArgs args)
		{
			Socket client = null;
			try
			{
				if (args.SocketError != SocketError.Success)
					throw new SocketException((int)args.SocketError);
				client = args.AcceptSocket;
				if (_Cancel.IsCancellationRequested)
					return;
				Logs.NodeServer.LogInformation("Client connection accepted {remoteEndPoint}", client.RemoteEndPoint);
				using (var cancel = CancellationTokenSource.CreateLinkedTokenSource(_Cancel.Token))
				{
					cancel.CancelAfter(TimeSpan.FromSeconds(10));

					var stream = new NetworkStream(client, false);
					while (true)
					{
						if (ConnectedNodes.Count >= MaxConnections)
						{
							Logs.NodeServer.LogInformation("MaxConnections limit reached");
							Utils.SafeCloseSocket(client);
							break;
						}
						cancel.Token.ThrowIfCancellationRequested();
						PerformanceCounter counter;
						var message = Message.ReadNext(stream, Network, Version, cancel.Token, out counter);
						_MessageProducer.PushMessage(new IncomingMessage()
						{
							Socket = client,
							Message = message,
							Length = counter.ReadenBytes,
							Node = null,
						});
						if (message.Payload is VersionPayload)
							break;
						else
							Logs.NodeServer.LogError("The first message of the remote peer did not contained a Version payload");
					}
				}
			}
			catch (OperationCanceledException)
			{
				Utils.SafeCloseSocket(client);
				if (!_Cancel.Token.IsCancellationRequested)
				{
					Logs.NodeServer.LogError("The remote connecting failed to send a message within 10 seconds, dropping connection");
				}
			}
			catch (Exception ex)
			{
				if (_Cancel.IsCancellationRequested)
					return;
				if (client == null)
				{
					Logs.NodeServer.LogError(default, ex, "Error while accepting connection");
					Thread.Sleep(3000);
				}
				else
				{
					Utils.SafeCloseSocket(client);
					Logs.NodeServer.LogError(default, ex, "Invalid message received from the remote connecting node");
				}
			}
			BeginAccept();
		}

		internal readonly MessageProducer<IncomingMessage> _MessageProducer = new MessageProducer<IncomingMessage>();
		internal readonly MessageProducer<object> _InternalMessageProducer = new MessageProducer<object>();

		MessageProducer<IncomingMessage> _AllMessages = new MessageProducer<IncomingMessage>();
		public MessageProducer<IncomingMessage> AllMessages
		{
			get
			{
				return _AllMessages;
			}
		}

		volatile IPEndPoint _ExternalEndpoint;
		public IPEndPoint ExternalEndpoint
		{
			get
			{
				return _ExternalEndpoint;
			}
			set
			{
				_ExternalEndpoint = Utils.EnsureIPv6(value);
			}
		}


		internal void ExternalAddressDetected(IPAddress iPAddress)
		{
			if (!ExternalEndpoint.Address.IsRoutable(AllowLocalPeers) && iPAddress.IsRoutable(AllowLocalPeers))
			{
				Logs.NodeServer.LogInformation("New externalAddress detected {externalAddress}", iPAddress);
				ExternalEndpoint = new IPEndPoint(iPAddress, ExternalEndpoint.Port);
			}
		}

		void ProcessMessage(IncomingMessage message)
		{
			AllMessages.PushMessage(message);

			using (Logs.NodeServer.BeginScope("Processing inbound message {message}", message.Message))
			{
				ProcessMessageCore(message);
			}
		}

		private void ProcessMessageCore(IncomingMessage message)
		{
			if (message.Message.Payload is VersionPayload)
			{
				var version = message.AssertPayload<VersionPayload>();
				var connectedToSelf = version.Nonce == Nonce;
				if (message.Node != null && connectedToSelf)
				{
					Logs.NodeServer.LogWarning("Connection to self detected, abort connection");
					message.Node.DisconnectAsync();
					return;
				}

				if (message.Node == null)
				{
					var remoteEndpoint = version.AddressFrom;
					if (!remoteEndpoint.Address.IsRoutable(AllowLocalPeers))
					{
						//Send his own endpoint
						remoteEndpoint = new IPEndPoint(((IPEndPoint)message.Socket.RemoteEndPoint).Address, Network.DefaultPort);
					}

					var peer = new NetworkAddress()
					{
						Endpoint = remoteEndpoint,
						Time = DateTimeOffset.UtcNow
					};
					var node = new Node(peer, Network, CreateNodeConnectionParameters(), message.Socket, version);

					if (connectedToSelf)
					{
						node.SendMessage(CreateNodeConnectionParameters().CreateVersion(node.Peer.Endpoint, Network));
						Logs.NodeServer.LogWarning("Connection to self detected, abort connection");
						node.Disconnect();
						return;
					}

					CancellationTokenSource cancel = new CancellationTokenSource();
					cancel.CancelAfter(TimeSpan.FromSeconds(10.0));
					try
					{
						ConnectedNodes.Add(node);
						node.StateChanged += node_StateChanged;
						node.RespondToHandShake(cancel.Token);
					}
					catch (OperationCanceledException ex)
					{
						Logs.NodeServer.LogError(default, ex, "The remote node did not respond fast enough (10 seconds) to the handshake completion, dropping connection");
						node.DisconnectAsync();
						throw;
					}
					catch (Exception)
					{
						node.DisconnectAsync();
						throw;
					}
				}
			}

			var messageReceived = MessageReceived;
			if (messageReceived != null)
				messageReceived(this, message);
		}

		void node_StateChanged(Node node, NodeState oldState)
		{
			if (node.State == NodeState.Disconnecting ||
				node.State == NodeState.Failed ||
				node.State == NodeState.Offline)
				ConnectedNodes.Remove(node);
		}

		private readonly NodesCollection _ConnectedNodes = new NodesCollection();
		public NodesCollection ConnectedNodes
		{
			get
			{
				return _ConnectedNodes;
			}
		}


		List<IDisposable> _Resources = new List<IDisposable>();
		IDisposable OwnResource(IDisposable resource)
		{
			if (_Cancel.IsCancellationRequested)
			{
				resource.Dispose();
				return Scope.Nothing;
			}
			return new Scope(() =>
			{
				lock (_Resources)
				{
					_Resources.Add(resource);
				}
			}, () =>
			{
				lock (_Resources)
				{
					_Resources.Remove(resource);
				}
			});
		}
		#region IDisposable Members

		CancellationTokenSource _Cancel = new CancellationTokenSource();
		public void Dispose()
		{
			if (!_Cancel.IsCancellationRequested)
			{
				_Cancel.Cancel();
				Logs.NodeServer.LogInformation("Stopping node server...");
				lock (_Resources)
				{
					foreach (var resource in _Resources)
						resource.Dispose();
				}
				try
				{
					_ConnectedNodes.DisconnectAll();
				}
				finally
				{
					if (socket != null)
					{
						Utils.SafeCloseSocket(socket);
						socket = null;
					}
				}
			}
		}

		#endregion

		internal NodeConnectionParameters CreateNodeConnectionParameters()
		{
			var myExternal = Utils.EnsureIPv6(ExternalEndpoint);
			var param2 = InboundNodeConnectionParameters.Clone();
			param2.Nonce = Nonce;
			param2.Version = Version;
			param2.AddressFrom = myExternal;
			return param2;
		}

		ulong _Nonce;
		public ulong Nonce
		{
			get
			{
				if (_Nonce == 0)
				{
					_Nonce = RandomUtils.GetUInt64();
				}
				return _Nonce;
			}
			set
			{
				_Nonce = value;
			}
		}



		public bool IsConnectedTo(IPEndPoint endpoint)
		{
			return _ConnectedNodes.FindByEndpoint(endpoint) != null;
		}

		public Node FindOrConnect(IPEndPoint endpoint)
		{
			while (true)
			{
				var node = _ConnectedNodes.FindByEndpoint(endpoint);
				if (node != null)
					return node;
				node = Node.Connect(Network, endpoint, CreateNodeConnectionParameters());
				node.StateChanged += node_StateChanged;
				if (!_ConnectedNodes.Add(node))
				{
					node.DisconnectAsync();
				}
				else
					return node;
			}
		}
	}
}
#endif
