#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace NBitcoin.Protocol
{
	public class SocketSettings
	{
		/// <summary>
		/// Set <see cref="System.Net.Sockets.Socket.ReceiveTimeout"/> value before connecting
		/// </summary>
		public TimeSpan? ReceiveTimeout { get; set; }

		/// <summary>
		/// Set <see cref="System.Net.Sockets.Socket.SendTimeout"/> value before connecting
		/// </summary>
		public TimeSpan? SendTimeout { get; set; }


		/// <summary>
		/// Set <see cref="System.Net.Sockets.Socket.ReceiveBufferSize"/> value before connecting
		/// </summary>
		public int? ReceiveBufferSize
		{
			get;
			set;
		}
		/// <summary>
		/// Set <see cref="System.Net.Sockets.Socket.SendBufferSize"/> value before connecting
		/// </summary>
		public int? SendBufferSize
		{
			get;
			set;
		}

		public void SetSocketProperties(Socket socket)
		{
			if (ReceiveTimeout is TimeSpan s && s.TotalMilliseconds < int.MaxValue)
				socket.ReceiveTimeout = s.TotalMilliseconds > int.MaxValue ? 0 : (int)s.TotalMilliseconds;
			if (SendTimeout is TimeSpan s2 && s2.TotalMilliseconds < int.MaxValue)
				socket.SendTimeout = s2.TotalMilliseconds > int.MaxValue ? 0 : (int)s2.TotalMilliseconds;
			// Use max supported by MAC OSX Yosemite/Mavericks/Sierra (https://fasterdata.es.net/host-tuning/osx/)
			if (ReceiveBufferSize is int v)
				socket.ReceiveBufferSize = Math.Min(v, 1048576);
			if (SendBufferSize is int v2)
				socket.SendBufferSize = Math.Min(v2, 1048576);
			////////////////////////
		}

		public SocketSettings Clone()
		{
			return new SocketSettings()
			{
				ReceiveTimeout = ReceiveTimeout,
				SendTimeout = SendTimeout,
				SendBufferSize = SendBufferSize,
				ReceiveBufferSize = ReceiveBufferSize
			};
		}
	}
}
#endif