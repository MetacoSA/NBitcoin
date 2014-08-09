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
		Dictionary<IPEndPoint, Node> _Nodes = new Dictionary<IPEndPoint, Node>();
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

        Random rnd = new Random();  //Allways be carefull with Random in multithreading TODO init better
        public Node GetRandomNode()
        {
            lock(_Nodes)
            {
                return _Nodes.ElementAt(rnd.Next(_Nodes.Count)).Value;
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

		public void AddNodes(IEnumerable<Node> nodes)
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
