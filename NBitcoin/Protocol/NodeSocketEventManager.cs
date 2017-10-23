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
		private SocketAsyncEventArgs instance;

		private NodeSocketEventManager() { }

		/// <summary>
		/// Creates an instance of <see cref="SocketAsyncEventArgs"/>.
		/// </summary>
		/// <param name="completedEvent">The event that will fire once the connection has been completed.</param>
		internal static NodeSocketEventManager Create(ManualResetEvent completedEvent)
		{
			var eventManager = new NodeSocketEventManager();
			eventManager.Instance.Completed += (s, a) => { Utils.SafeSet(completedEvent); };
			return eventManager;
		}

		/// <summary>
		/// Creates an instance of <see cref="SocketAsyncEventArgs"/>.
		/// </summary>
		/// <param name="completedEvent">The event that will fire once the connection has been completed.</param>
		/// <param name="endpoint">The end point to connect to.</param>
		internal static NodeSocketEventManager Create(ManualResetEvent completedEvent, IPEndPoint endPoint)
		{
			var eventManager = Create(completedEvent);
			eventManager.Instance.RemoteEndPoint = endPoint;
			return eventManager;
		}

		/// <summary>
		/// An instance of <see cref="SocketAsyncEventArgs"/> which we will use in this manager.
		/// </summary>
		public SocketAsyncEventArgs Instance
		{
			get
			{
				if (this.instance == null)
					this.instance = new SocketAsyncEventArgs();
				return this.instance;
			}
		}

		public void Dispose()
		{
			this.instance.Dispose();
			this.instance = null;
		}
	}
}