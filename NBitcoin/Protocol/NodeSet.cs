using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class NodeSet
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Dictionary<IPEndPoint, Node> _Nodes = new Dictionary<IPEndPoint, Node>();
		readonly MessageListener<IncomingMessage> _MessageListener;
		public NodeSet()
			: this(null)
		{

		}
		public NodeSet(MessageListener<IncomingMessage> listener)
		{
			if(listener == null)
				listener = new NullMessageListener<IncomingMessage>();
			_MessageListener = listener;
		}


		public Node GetNodeByEndpoint(IPEndPoint endpoint)
		{
			lock(_Nodes)
			{
				endpoint = Utils.EnsureIPv6(endpoint);

				Node result = null;
				_Nodes.TryGetValue(endpoint, out result);
				return result;
			}
		}

		public Node GetNodeByPeer(Peer peer)
		{
			lock(_Nodes)
			{
				Node result = null;
				_Nodes.TryGetValue(peer.NetworkAddress.Endpoint, out result);
				return result;
			}
		}


		public Node AddNode(Node node)
		{
			lock(_Nodes)
			{
				if(node.State < NodeState.Connected)
					return null;
				node.MessageProducer.AddMessageListener(_MessageListener);
				_Nodes.Add(node.Peer.NetworkAddress.Endpoint, node);
			}
			return node;
		}

		public void RemoveNode(Node node)
		{
			lock(_Nodes)
			{
				if(_Nodes.Remove(node.Peer.NetworkAddress.Endpoint))
				{
					node.MessageProducer.RemoveMessageListener(_MessageListener);
				}
			}
		}

		public Node[] GetNodes()
		{
			lock(_Nodes)
			{
				return _Nodes.Values.ToArray();
			}
		}

		public void DisconnectAll(CancellationToken cancellation = default(CancellationToken))
		{
			DisconnectNodes(GetNodes());
		}



		public void DisconnectNodes(Node[] nodes, CancellationToken cancellation = default(CancellationToken))
		{
			var tasks = nodes.Select(n => Task.Factory.StartNew(() => n.Disconnect())).ToArray();
			Task.WaitAll(tasks, cancellation);
		}

		public bool Contains(IPEndPoint endpoint)
		{
			lock(_Nodes)
			{
				return _Nodes.ContainsKey(endpoint);
			}
		}

		public void AddNodes(Node[] nodes)
		{
			lock(_Nodes)
			{
				foreach(var node in nodes)
				{
					AddNode(node);
				}
			}
		}

		public int Count()
		{
			lock(_Nodes)
			{
				return _Nodes.Count;
			}
		}



		public void RemoveNodes(Node[] nodes)
		{
			lock(_Nodes)
			{
				foreach(var node in nodes)
				{
					RemoveNode(node);
				}
			}
		}
	}
}
