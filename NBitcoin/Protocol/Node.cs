using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class Node
	{
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

		Socket _Socket;
		public Socket Socket
		{
			get
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
							ProtocolTrace.Information("Connection successfull");
						}
						catch(Exception ex)
						{
							ProtocolTrace.Error("Error connecting to the remote endpoint ", ex);
							throw;
						}
						BeginListen();
					}
				}
				return _Socket;
			}
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
			var socket = Socket;

			Task.Run(() =>
			{
				using(TraceCorrelation.Open())
				{
					ProtocolTrace.Information("Start listening");
					try
					{
						var stream = new NetworkStream(socket, false);
						BitcoinStream bitStream = new BitcoinStream(stream, false);
						while(true)
						{
							ReadMagic(stream);
							Message message = new Message();
							message.SkipMagic = true;
							message.Magic = RPCServer.Network.Magic;
							message.ReadWrite(bitStream);
							message.SkipMagic = false;
							ProtocolTrace.Information("Message recieved " + message.Command);
							MessageQueue.Add(message);
						}
					}
					catch(Exception ex)
					{
						if(Socket != null)
						{
							ProtocolTrace.Error("Connection to server stopped unexpectedly", ex);
						}
					}
					finally
					{
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
						}
						if(socket == _Socket)
							_Socket = null;
					}
					ProtocolTrace.Information("Stop listening");
				}
			});
		}

		private void ReadMagic(NetworkStream stream)
		{
			for(int i = 0 ; i < RPCServer.Network.MagicBytes.Length ; i++)
			{
				var v = stream.ReadByte();

				if(RPCServer.Network.MagicBytes[i] != v)
					i = -1;

			}
		}

		public Node(NetworkAddress networkAddress, ProtocolServer rpcServer)
		{
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
					using(_TraceCorrelation.Open())
					{
						ProtocolTrace.Trace.TraceEvent(TraceEventType.Start, 0, "Communication with " + Endpoint.ToString());
					}
				}
				return _TraceCorrelation;
			}
		}
		public void SendMessage(Payload payload)
		{
			using(TraceCorrelation.Open())
			{
				var message = new Message();
				message.Magic = RPCServer.Network.Magic;
				message.UpdatePayload(payload, RPCServer.Version);
				ProtocolTrace.Information("Sending message " + message.Command);
				Socket.Send(message.ToBytes());
			}
		}

		public void Version()
		{
			SendMessage(new VersionPayload()
			{
				Nonce = GetNonce(),
				UserAgent = GetUserAgent(),
				Version = (uint)RPCServer.Version,
				StartHeight = 0,
				Timestamp = DateTimeOffset.UtcNow,
				AddressReciever = Endpoint,
				AddressFrom = new IPEndPoint(IPAddress.Parse("128.79.220.96"), RPCServer.Network.DefaultPort)
			});
			var result = RecieveMessage<VersionPayload>();
		}

		private T RecieveMessage<T>() where T : Payload
		{
			var message = RecieveMessage();
			if(message.Payload is T)
				return (T)(message.Payload);
			else
				throw new FormatException("Expected message " + typeof(T).Name + " but got " + message.Payload.GetType().Name);
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

		public void Disconnect()
		{
			var socket = Socket;
			_Socket = null;
			if(socket != null)
			{
				try
				{
					socket.Disconnect(false);
				}
				catch(Exception)
				{
				}
				try
				{
					socket.Dispose();
				}
				catch(Exception)
				{
				}
			}
		}
	}
}
