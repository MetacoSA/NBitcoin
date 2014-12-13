#if !NOSOCKET
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
	public delegate void NodeSetEventHandler(NodeSet sender, Node node);
	public class NodeSet : IDisposable
	{
		class NodeListener : MessageListener<IncomingMessage>
		{
			NodeSet _Parent;
			public NodeListener(NodeSet parent)
			{
				_Parent = parent;
			}
			#region MessageListener<IncomingMessage> Members

			public void PushMessage(IncomingMessage message)
			{
				_Parent.MessageProducer.PushMessage(message);
			}

			#endregion
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Dictionary<IPEndPoint, List<Node>> _Nodes = new Dictionary<IPEndPoint, List<Node>>();
		readonly NodeListener _MessageListener;
		public NodeSet()
		{
			_MessageListener = new NodeListener(this);
		}


		private readonly MessageProducer<IncomingMessage> _MessageProducer = new MessageProducer<IncomingMessage>();
		public MessageProducer<IncomingMessage> MessageProducer
		{
			get
			{
				return _MessageProducer;
			}
		}

		public Node GetNodeByEndpoint(IPEndPoint endpoint)
		{
			lock(_Nodes)
			{
				endpoint = Utils.EnsureIPv6(endpoint);
				return GetNodesList(endpoint).FirstOrDefault();
			}
		}

		public Node GetNodeByPeer(Peer peer)
		{
			lock(_Nodes)
			{
				return GetNodesList(peer.NetworkAddress.Endpoint).FirstOrDefault();
			}
		}

		List<Node> GetNodesList(IPEndPoint endpoint)
		{
			List<Node> result = null;
			if(!_Nodes.TryGetValue(endpoint, out result))
				result = new List<Node>();
			return result;
		}

		public Node AddNode(Node node)
		{
			var added = false;
			lock(_Nodes)
			{
				if(node.State < NodeState.Connected)
					return null;
				node.StateChanged += node_StateChanged;
				node.MessageProducer.AddMessageListener(_MessageListener);
				var list = GetNodesList(node.Peer.NetworkAddress.Endpoint);
				list.Add(node);
				added = true;
				if(list.Count == 1)
					_Nodes.Add(node.Peer.NetworkAddress.Endpoint, list);
			}
			if(added)
			{
				var nodeAdded = NodeAdded;
				if(nodeAdded != null)
					nodeAdded(this, node);
			}
			return node;
		}

		public event NodeSetEventHandler NodeAdded;
		public event NodeSetEventHandler NodeRemoved;
		void node_StateChanged(Node node, NodeState oldState)
		{
			if(node.State == NodeState.Offline || node.State == NodeState.Disconnecting || node.State == NodeState.Failed)
			{
				RemoveNode(node);
			}
		}

		public void RemoveNode(Node node)
		{
			bool removed = false;
			lock(_Nodes)
			{
				var endpoint = node.Peer.NetworkAddress.Endpoint;
				var nodes = GetNodesList(endpoint);
				if(nodes.Remove(node))
				{
					removed = true;
					node.MessageProducer.RemoveMessageListener(_MessageListener);
					node.StateChanged -= node_StateChanged;
					if(nodes.Count == 0)
						_Nodes.Remove(endpoint);
				}
			}
			if(removed)
			{
				var nodeRemoved = NodeRemoved;
				if(nodeRemoved != null)
					nodeRemoved(this, node);
			}
		}

		public Node[] GetNodes()
		{
			lock(_Nodes)
			{
				return _Nodes.Values.SelectMany(s => s).ToArray();
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

		#region IDisposable Members

		public void Dispose()
		{
			Task.Factory.StartNew(() =>
			{
				try
				{
					DisconnectAll();
				}
				catch(Exception)
				{

				}
			}, TaskCreationOptions.LongRunning);
		}

		#endregion

		public void SendMessage(Payload payload)
		{
			var nodes = GetNodes();

			var tasks =
				nodes
				.Select(n => Task.Factory.StartNew(() =>
				{
					n.SendMessage(payload);
				}, TaskCreationOptions.LongRunning))
				.ToArray();

			Task.WaitAll(tasks);
		}
	}
}
#endif