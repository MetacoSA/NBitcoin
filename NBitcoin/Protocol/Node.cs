using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public enum NodeState : int
	{
		Failed,
		Offline,
		Disconnecting,
		Connected,
		HandShaked
	}

	public class Node
	{
		private NodeState _State = NodeState.Offline;
		public NodeState State
		{
			get
			{
				return _State;
			}
			private set
			{
				TraceCorrelation.LogInside(() => NodeServerTrace.Information("State changed from " + _State + " to " + value));
				_State = value;
				if(value == NodeState.Failed)
					NodeServer.PeerTable.RemovePeer(Peer);
				if(value == NodeState.Failed || value == NodeState.Offline || value == NodeState.Disconnecting)
				{
					NodeServer.RemoveNode(this);
				}
			}
		}

		private readonly NodeServer _NodeServer;
		public NodeServer NodeServer
		{
			get
			{
				return _NodeServer;
			}
		}

		volatile Socket _Socket;
		public Socket EnsureConnected()
		{
			if(_Socket == null || !_Socket.Connected)
			{
				if(_Socket != null)
				{
					_Socket.Dispose();
					_Socket = null;
				}
				_Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
				using(TraceCorrelation.Open())
				{

					try
					{
						_Socket.Connect(Peer.NetworkAddress.Endpoint);
						State = NodeState.Connected;
						_Connected = new CancellationTokenSource();
						NodeServerTrace.Information("Outbound connection successfull");
					}
					catch(Exception ex)
					{
						State = NodeState.Failed;
						NodeServerTrace.Error("Error connecting to the remote endpoint ", ex);
						throw;
					}
					BeginListen();
				}
			}
			return _Socket;
		}


		private void BeginListen()
		{
			if(_Socket == null)
				throw new InvalidOperationException("Socket should not be null at this point");
			var socket = _Socket;
			Task.Run(() =>
			{
				using(TraceCorrelation.Open(false))
				{
					NodeServerTrace.Information("Listening");
					try
					{
						while(!_Connected.IsCancellationRequested)
						{
							var message = Message.ReadNext(socket, NodeServer.Network, Version, _Connected.Token);
							NodeServerTrace.Information("Message recieved " + message);

							LastSeen = DateTimeOffset.UtcNow;
							MessageProducer.PushMessage(new IncomingMessage()
							{
								Message = message,
								Socket = socket,
								Node = this
							});
						}

					}
					catch(OperationCanceledException)
					{
					}
					catch(Exception ex)
					{
						if(State != NodeState.Disconnecting)
						{
							State = NodeState.Failed;
							NodeServerTrace.Error("Connection to server stopped unexpectedly", ex);
						}
					}
					NodeServerTrace.Information("Stop listening");
					if(State != NodeState.Failed)
						State = NodeState.Offline;
					Utils.SafeCloseSocket(socket);
					socket = null;
					if(_Disconnected != null)
						_Disconnected.Set();
				}
			});
		}





		internal Node(Peer peer, NodeServer rpcServer)
		{
			Version = rpcServer.Version;
			_NodeServer = rpcServer;
			_Peer = peer;
			LastSeen = peer.NetworkAddress.Time;
		}
		internal Node(Peer peer, NodeServer rpcServer, Socket socket)
			: this(peer, rpcServer)
		{
			_Socket = socket;
			TraceCorrelation.LogInside(() => NodeServerTrace.Information("Connected to advertised node " + _Peer.NetworkAddress.Endpoint));
			BeginListen();
		}

		private readonly Peer _Peer;
		public Peer Peer
		{
			get
			{
				return _Peer;
			}
		}

		public DateTimeOffset LastSeen
		{
			get;
			private set;
		}

		TraceCorrelation _TraceCorrelation = null;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TraceCorrelation TraceCorrelation
		{
			get
			{
				if(_TraceCorrelation == null)
				{
					_TraceCorrelation = new TraceCorrelation(NodeServerTrace.Trace, "Communication with " + Peer.NetworkAddress.Endpoint.ToString());
				}
				return _TraceCorrelation;
			}
		}
		public void SendMessage(Payload payload)
		{
			var socket = EnsureConnected();
			var message = new Message();
			message.Magic = NodeServer.Network.Magic;
			message.UpdatePayload(payload, Version);
			TraceCorrelation.LogInside(() => NodeServerTrace.Information("Sending message " + message));
			socket.Send(message.ToBytes());
		}

		public ProtocolVersion Version
		{
			get;
			private set;
		}

		private readonly MessageProducer<IncomingMessage> _MessageProducer = new MessageProducer<IncomingMessage>();
		public MessageProducer<IncomingMessage> MessageProducer
		{
			get
			{
				return _MessageProducer;
			}
		}

		VersionPayload _FullVersion;
		public void VersionHandshake(CancellationToken cancellationToken = default(CancellationToken))
		{
			var listener = new PollMessageListener<IncomingMessage>();
			using(MessageProducer.AddMessageListener(listener))
			{
				try
				{
					using(TraceCorrelation.Open())
					{
						SendMessage(NodeServer.CreateVersionPayload(Peer));

						var version = listener.RecieveMessage(cancellationToken).AssertPayload<VersionPayload>();
						_FullVersion = version;
						Version = version.Version;
						if(version.AddressReciever.Address != NodeServer.ExternalEndpoint.Address)
						{
							NodeServerTrace.Warning("Different external address detected by the node " + version.AddressReciever + " instead of " + NodeServer.ExternalEndpoint);
						}
						NodeServer.ExternalAddressDetected(version.AddressReciever.Address);
						if(version.Version < ProtocolVersion.MIN_PEER_PROTO_VERSION)
						{
							NodeServerTrace.Warning("Outdated version " + version.Version);
							IsOutdated = true;
							return;
						}
						SendMessage(new VerAckPayload());
						listener.RecieveMessage(cancellationToken).AssertPayload<VerAckPayload>();
						State = NodeState.HandShaked;
					}
				}
				catch(OperationCanceledException)
				{
					if(IsConnected)
						DisconnectAsync();
				}
			}
		}





		CancellationTokenSource _Connected;
		ManualResetEvent _Disconnected;

		public bool IsOutdated
		{
			get;
			private set;
		}

		public Task DisconnectAsync()
		{
			lock(this)
			{
				if(!IsConnected)
					throw new InvalidOperationException("Node already disconnected");
				using(TraceCorrelation.Open())
				{
					NodeServerTrace.Information("Disconnection request");
					var disconnected = new ManualResetEvent(false);
					_Disconnected = disconnected;
					State = NodeState.Disconnecting;
					_Connected.Cancel();
					return Task.Run(() => disconnected.WaitOne());
				}
			}
		}

		public bool IsConnected
		{
			get
			{
				return State >= NodeState.Connected;
			}
		}

		public override string ToString()
		{
			return State + " (" + Peer.NetworkAddress.Endpoint + ") " + Peer.Origin;
		}
	}
}
