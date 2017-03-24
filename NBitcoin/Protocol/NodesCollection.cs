#if !NOSOCKET
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class NodeEventArgs : EventArgs
	{
		public NodeEventArgs(Node node, bool added)
		{
			_Added = added;
			_Node = node;
		}

		private readonly bool _Added;
		public bool Added
		{
			get
			{
				return _Added;
			}
		}
		private readonly Node _Node;
		public Node Node
		{
			get
			{
				return _Node;
			}
		}
	}
	public class NodesCollection : IEnumerable<Node>
	{
		class Bridge : MessageListener<IncomingMessage>
		{
			MessageProducer<IncomingMessage> _Prod;
			public Bridge(MessageProducer<IncomingMessage> prod)
			{
				_Prod = prod;
			}
			#region MessageListener<IncomingMessage> Members

			public void PushMessage(IncomingMessage message)
			{
				_Prod.PushMessage(message);
			}

			#endregion
		}
		Bridge bridge;
		public NodesCollection()
		{
			bridge = new Bridge(_MessageProducer);
		}

		MessageProducer<IncomingMessage> _MessageProducer = new MessageProducer<IncomingMessage>();
		public MessageProducer<IncomingMessage> MessageProducer
		{
			get
			{
				return _MessageProducer;
			}
		}

		ConcurrentDictionary<Node, Node> _Nodes = new ConcurrentDictionary<Node, Node>();

		public int Count
		{
			get
			{
				return _Nodes.Count;
			}
		}
		public bool Add(Node node)
		{
			if(node == null)
				throw new ArgumentNullException("node");
			if(_Nodes.TryAdd(node, node))
			{
				node.MessageProducer.AddMessageListener(bridge);
				OnNodeAdded(node);
				return true;
			}
			return false;
		}

		public bool Remove(Node node)
		{
			Node old;
			if(_Nodes.TryRemove(node, out old))
			{
				node.MessageProducer.RemoveMessageListener(bridge);
				OnNodeRemoved(old);
				return true;
			}
			return false;
		}

		public event EventHandler<NodeEventArgs> Added;
		public event EventHandler<NodeEventArgs> Removed;

		private void OnNodeAdded(Node node)
		{
			var added = Added;
			if(added != null)
				added(this, new NodeEventArgs(node, true));
		}

		public void OnNodeRemoved(Node node)
		{
			var removed = Removed;
			if(removed != null)
				removed(this, new NodeEventArgs(node, false));
		}

		public Node FindLocal()
		{
			return FindByIp(IPAddress.Loopback);
		}

		public Node FindByIp(IPAddress ip)
		{
			ip = ip.EnsureIPv6();
			return _Nodes.Where(n => Match(ip, null, n.Key)).Select(s => s.Key).FirstOrDefault();
		}



		public Node FindByEndpoint(IPEndPoint endpoint)
		{
			var ip = endpoint.Address.EnsureIPv6();
			int port = endpoint.Port;
			return _Nodes.Select(n => n.Key).FirstOrDefault(n => Match(ip, port, n));
		}

		private static bool Match(IPAddress ip, int? port, Node n)
		{
			if(port.HasValue)
			{
				return (n.State > NodeState.Disconnecting && n.RemoteSocketAddress.Equals(ip) && n.RemoteSocketPort == port.Value) ||
						(n.PeerVersion.AddressFrom.Address.Equals(ip) && n.PeerVersion.AddressFrom.Port == port.Value);
			}
			else
			{
				return (n.State > NodeState.Disconnecting && n.RemoteSocketAddress.Equals(ip)) ||
						n.PeerVersion.AddressFrom.Address.Equals(ip);
			}
		}


		#region IEnumerable<Node> Members

		public IEnumerator<Node> GetEnumerator()
		{
			return _Nodes.Select(n => n.Key).AsEnumerable().GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		public void DisconnectAll(CancellationToken cancellation = default(CancellationToken))
		{
			foreach(var node in _Nodes)
				node.Key.DisconnectAsync();
		}

		public void Clear()
		{
			_Nodes.Clear();
		}
	}
}
#endif