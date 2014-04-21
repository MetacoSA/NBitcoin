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

	public enum NodeOrigin
	{
		DNSSeed,
		HardSeed,
		Addr,
		Manually
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
				TraceCorrelation.LogInside(()=>ProtocolTrace.Information("State changed from " + _State + " to " + value));
				_State = value;
			}
		}

		private readonly NodeOrigin _Origin;
		public NodeOrigin Origin
		{
			get
			{
				return _Origin;
			}
		}


		static Random _RandNonce = new Random();
		private readonly ProtocolServer _RPCServer;
		public ProtocolServer RPCServer
		{
			get
			{
				return _RPCServer;
			}
		}

		public IPEndPoint Endpoint
		{
			get;
			private set;
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
						_Socket.Connect(Endpoint);
						State = NodeState.Connected;
						_Connected = new CancellationTokenSource();
						ProtocolTrace.Information("Outbound connection successfull");
					}
					catch(Exception ex)
					{
						State = NodeState.Failed;
						ProtocolTrace.Error("Error connecting to the remote endpoint ", ex);
						throw;
					}
					BeginListen();
				}
			}
			return _Socket;
		}


		private void BeginListen()
		{
			var socket = _Socket;
			Task.Run(() =>
			{
				using(TraceCorrelation.Open())
				{
					ProtocolTrace.Information("Listening");
					try
					{
						while(!_Connected.IsCancellationRequested)
						{
							var message = Message.ReadNext(socket, RPCServer.Network, Version, _Connected.Token);
							ProtocolTrace.Information("Message recieved " + message);
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
							ProtocolTrace.Error("Connection to server stopped unexpectedly", ex);
						}
					}
					ProtocolTrace.Information("Stop listening");
					if(State != NodeState.Failed)
						State = NodeState.Offline;
					Utils.SafeCloseSocket(socket);
					socket = null;
					_Disconnected.Set();
				}
			});
		}





		internal Node(NetworkAddress networkAddress, ProtocolServer rpcServer, NodeOrigin origin)
		{
			Version = rpcServer.Version;
			_Origin = origin;
			_RPCServer = rpcServer;
			LastSeen = networkAddress.Time;
			Endpoint = networkAddress.Endpoint;
		}

		public DateTimeOffset LastSeen
		{
			get;
			private set;
		}

		TraceCorrelation _TraceCorrelation = null;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		TraceCorrelation TraceCorrelation
		{
			get
			{
				if(_TraceCorrelation == null)
				{
					_TraceCorrelation = new TraceCorrelation();

					TraceCorrelation.LogInside(() =>
						ProtocolTrace.Trace.TraceEvent(TraceEventType.Start, 0, "Communication with " + Endpoint.ToString()));

				}
				return _TraceCorrelation;
			}
		}
		public void SendMessage(Payload payload)
		{
			var socket = EnsureConnected();
			var message = new Message();
			message.Magic = RPCServer.Network.Magic;
			message.UpdatePayload(payload, Version);
			TraceCorrelation.LogInside(() => ProtocolTrace.Information("Sending message " + message));
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

		public void VersionHandshake()
		{
			var listener = new PollMessageListener<IncomingMessage>();
			using(MessageProducer.AddMessageListener(listener))
			{
				using(TraceCorrelation.Open())
				{
					SendMessage(new VersionPayload()
					{
						Nonce = GetNonce(),
						UserAgent = GetUserAgent(),
						Version = RPCServer.Version,
						StartHeight = 0,
						Timestamp = DateTimeOffset.UtcNow,
						AddressReciever = Endpoint,
						AddressFrom = RPCServer.ExternalEndpoint
					});

					var version = listener.RecieveMessage().AssertPayload<VersionPayload>();
					Version = version.Version;
					if(version.AddressReciever.Address != RPCServer.ExternalEndpoint.Address)
					{
						ProtocolTrace.Warning("Different external address detected by the node " + version.AddressReciever + " instead of " + RPCServer.ExternalEndpoint);
					}
					RPCServer.ExternalAddressDetected(version.AddressReciever.Address);
					if(version.Version < ProtocolVersion.MIN_PEER_PROTO_VERSION)
					{
						ProtocolTrace.Warning("Outdated version " + version.Version);
						IsOutdated = true;
						return;
					}
					SendMessage(new VerAckPayload());
					listener.RecieveMessage().AssertPayload<VerAckPayload>();
					State = NodeState.HandShaked;
				}
			}
		}



		private string GetUserAgent()
		{
			var version = this.GetType().Assembly.GetName().Version;
			return "/NBitcoin:" + version.Major + "." + version.MajorRevision + "." + version.Minor + "/";
		}

		private ulong GetNonce()
		{
			lock(_RandNonce)
			{
				var bytes = new byte[8];
				_RandNonce.NextBytes(bytes);
				return BitConverter.ToUInt64(bytes, 0);
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
					ProtocolTrace.Information("Disconnection request");
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
	}
}
