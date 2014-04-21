using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
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
		private NodeState _State;
		public NodeState State
		{
			get
			{
				return _State;
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
		bool disconnectAsked;

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
						_State = NodeState.Connected;
						_Disconnected.Reset();
						ProtocolTrace.Information("Outbound connection successfull");
					}
					catch(Exception ex)
					{
						_State = NodeState.Failed;
						ProtocolTrace.Error("Error connecting to the remote endpoint ", ex);
						throw;
					}
					BeginListen();
				}
			}
			return _Socket;
		}

		BlockingCollection<Message> _MessageQueue = new BlockingCollection<Message>(new ConcurrentQueue<Message>());

		public BlockingCollection<Message> MessageQueue
		{
			get
			{
				return _MessageQueue;
			}
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
						var stream = new NetworkStream(socket, false);
						BitcoinStream bitStream = new BitcoinStream(stream, false);
						while(!disconnectAsked)
						{
							if(ReadMagic(stream))
							{
								Message message = new Message();
								message.SkipMagic = true;
								message.Magic = RPCServer.Network.Magic;
								message.ReadWrite(bitStream);
								message.SkipMagic = false;
								ProtocolTrace.Information("Message recieved " + message.Command);
								MessageQueue.Add(message);
							}
						}
					}
					catch(Exception ex)
					{
						_State = NodeState.Failed;
						ProtocolTrace.Error("Connection to server stopped unexpectedly", ex);
					}
					ProtocolTrace.Information("Stop listening");
					if(_State != NodeState.Failed)
						_State = NodeState.Offline;
					try
					{
						socket.Disconnect(false);
					}
					catch
					{
					}
					try
					{
						socket.Dispose();
					}
					catch
					{
						_Socket = null;
					}
					disconnectAsked = false;
					_Disconnected.Set();
				}
			});
		}



		private bool ReadMagic(NetworkStream stream)
		{
			for(int i = 0 ; i < RPCServer.Network.MagicBytes.Length ; i++)
			{
				if(disconnectAsked)
					return false;
				var v = stream.ReadByte();
				if(v == -1)
				{
					i--;
				}
				if(RPCServer.Network.MagicBytes[i] != v)
					i = -1;

			}
			return true;
		}

		internal Node(NetworkAddress networkAddress, ProtocolServer rpcServer, NodeOrigin origin)
		{
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
			message.UpdatePayload(payload, RPCServer.Version);
			TraceCorrelation.LogInside(() => ProtocolTrace.Information("Sending message " + message.Command));
			socket.Send(message.ToBytes());
		}

		public ProtocolVersion Version
		{
			get;
			private set;
		}

		public void VersionHandshake()
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
			var version = RecieveMessage<VersionPayload>();
			Version = RPCServer.Version;
			if(version.AddressReciever.Address != RPCServer.ExternalEndpoint.Address)
			{
				TraceCorrelation.LogInside(() => ProtocolTrace.Warning("Different external address detected by the node " + version.AddressReciever.Address + " instead of " + RPCServer.ExternalEndpoint));
			}
			RPCServer.ExternalAddressDetected(version.AddressReciever.Address);
			if(version.Version < ProtocolVersion.MIN_PEER_PROTO_VERSION)
			{
				TraceCorrelation.LogInside(() => ProtocolTrace.Warning("Outdated version " + version.Version));
				IsOutdated = true;
				return;
			}
			SendMessage(new VerAckPayload());
			RecieveMessage<VerAckPayload>();
			_State = NodeState.HandShaked;
		}

		private T RecieveMessage<T>() where T : Payload
		{

			var message = RecieveMessage();
			if(message.Payload is T)
				return (T)(message.Payload);
			else
			{
				var ex = new FormatException("Expected message " + typeof(T).Name + " but got " + message.Payload.GetType().Name);
				TraceCorrelation.LogInside(() => ProtocolTrace.Error("Unexpected message type received", ex));
				throw ex;
			}

		}



		private Message RecieveMessage()
		{
			return MessageQueue.Take();
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

		ManualResetEvent _Disconnected = new ManualResetEvent(true);
		public void Disconnect()
		{
			disconnectAsked = true;
			_Disconnected.WaitOne();
		}

		public bool IsOutdated
		{
			get;
			private set;
		}
	}
}
