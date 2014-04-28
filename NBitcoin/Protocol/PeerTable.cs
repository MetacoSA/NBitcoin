using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public enum PeerOrigin : byte
	{
		Manual,
		Addr,
		Advertised,
		DNSSeed,
		HardSeed,
	}
	public class Peer
	{
		public Peer(PeerOrigin origin, NetworkAddress address)
		{
			_Origin = origin;
			_NetworkAddress = address;
		}
		private readonly PeerOrigin _Origin;
		public PeerOrigin Origin
		{
			get
			{
				return _Origin;
			}
		}
		private readonly NetworkAddress _NetworkAddress;
		public NetworkAddress NetworkAddress
		{
			get
			{
				return _NetworkAddress;
			}
		}
	}
	public class PeerTable
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		Peer[] _Peers = new Peer[1000];
		public Peer[] GetActivePeers(int maxCount)
		{
			maxCount = Math.Min(1000, maxCount);
			List<Peer> result = new List<Peer>();
			lock(_Peers)
			{
				Random rand = new Random();
				result.AddRange(_Peers
									.Where(p => p != null)
									.Where(p => p.NetworkAddress.Ago < TimeSpan.FromHours(3.0) || p.Origin == PeerOrigin.DNSSeed || p.Origin == PeerOrigin.HardSeed)
									.OrderBy(p => p.Origin)
									.ThenBy(p => p.NetworkAddress.Ago)
									.ThenBy(p => rand.Next())
									.Take(maxCount));
			}
			return result.ToArray();
		}
		public void UpdatePeers(IEnumerable<Peer> address)
		{
			lock(_Peers)
			{
				foreach(var a in address)
					UpdatePeerCore(a);
			}
		}
		public Peer UpdatePeer(Peer address)
		{
			lock(_Peers)
			{
				return UpdatePeerCore(address);
			}
		}

		private Peer UpdatePeerCore(Peer peer)
		{
			if(!peer.NetworkAddress.Endpoint.Address.IsRoutable())
				return peer;
			var index = GetIndex(p => p != null &&
									  p.NetworkAddress.Endpoint.Equals(peer.NetworkAddress.Endpoint));
			if(index == -1)
			{
				var freeIndex = GetIndex(p => IsFree(p, false));
				if(freeIndex == -1)
				{
					freeIndex = GetIndex(p => IsFree(p, true));
					if(freeIndex == -1)
						return peer;
				}
				index = freeIndex;
			}
			_Peers[index] = peer;
			return peer;
		}

		private bool IsFree(Peer p, bool seedsAsFree = true)
		{
			if(p == null)
				return true;
			var isExpired = p.NetworkAddress.Ago > TimeSpan.FromHours(3.0);
			var isSeed = p.Origin == PeerOrigin.DNSSeed ||
							p.Origin == PeerOrigin.HardSeed;

			return isSeed ? seedsAsFree : isExpired;
		}

		public int CountUsed(bool seedsAsFree = true)
		{
			lock(_Peers)
			{
				return _Peers.Where(p => !IsFree(p, seedsAsFree)).Count();
			}
		}

		private int GetIndex(Predicate<Peer> predicate)
		{
			for(int i = 0 ; i < _Peers.Length ; i++)
			{
				if(predicate(_Peers[i]))
					return i;
			}
			return -1;
		}


		public Peer GetPeer(IPEndPoint endpoint)
		{
			if(endpoint == null)
				throw new ArgumentNullException("endpoint");
			if(endpoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				endpoint = new IPEndPoint(endpoint.Address.MapToIPv6(), endpoint.Port);
			lock(_Peers)
			{
				return _Peers.Where(p => p != null && p.NetworkAddress.Endpoint.Equals(endpoint)).FirstOrDefault();
			}
		}

		public void RemovePeer(Peer peer)
		{
			RemovePeer(peer.NetworkAddress.Endpoint);
		}

		private void RemovePeer(IPEndPoint endpoint)
		{
			lock(_Peers)
			{
				var i = GetIndex(p => p != null && p.NetworkAddress.Endpoint.Equals(endpoint));
				if(i != -1)
					_Peers[i] = null;
			}
		}
	}
}
