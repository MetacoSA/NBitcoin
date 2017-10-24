using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// Singleton class which deals with a single instance of <see cref="SocketAsyncEventArgs"/>.
	/// </summary>
	internal sealed class NodeSocketEventManager : IDisposable
	{
		private SocketAsyncEventArgs socketEvent;

		private NodeSocketEventManager() { }

		/// <summary>
		/// Creates a <see cref="NodeSocketEventManager"/> with a instance of <see cref="SocketAsyncEventArgs"/>.
		/// </summary>
		/// <param name="completedEvent">The event that will fire once the connection has been completed.</param>
		/// <param name="endpoint">The end point to connect to.</param>
		internal static NodeSocketEventManager Create(ManualResetEvent completedEvent, IPEndPoint endPoint = null)
		{
			var eventManager = new NodeSocketEventManager();
			eventManager.SocketEvent.Completed += (s, a) => { Utils.SafeSet(completedEvent); };

			if (endPoint != null)
				eventManager.SocketEvent.RemoteEndPoint = endPoint;

			return eventManager;
		}

		/// <summary>
		/// An instance of <see cref="SocketAsyncEventArgs"/> which we will use in this manager.
		/// </summary>
		public SocketAsyncEventArgs SocketEvent
		{
			get
			{
				if (this.socketEvent == null)
					this.socketEvent = new SocketAsyncEventArgs();
				return this.socketEvent;
			}
		}

		public void Dispose()
		{
			if (this.socketEvent != null)
			{
				this.socketEvent.Dispose();
				this.socketEvent = null;
			}
		}
	}
}