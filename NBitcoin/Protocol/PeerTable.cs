#if !NOSOCKET
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

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

		public Peer Clone()
		{
			return new Peer(Origin, NetworkAddress.Clone());
		}
	}
	public class PeerTable : InMemoryPeerTableRepository
	{
		Dictionary<string, Peer> _PeerSeeds = new Dictionary<string, Peer>();
		public PeerTable()
		{
			ValiditySpan = TimeSpan.FromHours(3.0);
		}
		public bool Randomize
		{
			get;
			set;
		}




		public Peer[] GetActivePeers(int maxCount)
		{
			maxCount = Math.Min(1000, maxCount);
			List<Peer> result = new List<Peer>();
			lock(_Peers)
			{
				result.AddRange(_Peers
									.Select(p => p.Value)
									.Concat(_PeerSeeds.Select(p => p.Value))
									.OrderBy(p => p.Origin)
									.ThenBy(p => p.NetworkAddress.Ago)
									.Take(maxCount));
			}
			var shuffled = result.ToArray();
			if(Randomize)
				Utils.Shuffle(shuffled);
			return shuffled;
		}

		public override void WritePeers(IEnumerable<Peer> peers)
		{
			lock(_Peers)
			{
				var normalPeers = peers.Where(p => p.Origin != PeerOrigin.DNSSeed && p.Origin != PeerOrigin.HardSeed);
				base.WritePeers(normalPeers);
				var seedPeers = peers.Where(p => p.Origin == PeerOrigin.DNSSeed || p.Origin == PeerOrigin.HardSeed);
				foreach(var s in seedPeers)
				{
					_PeerSeeds.AddOrReplace(s.NetworkAddress.Endpoint.ToString(), s);
				}
			}
		}

		public override IEnumerable<Peer> GetPeers()
		{
			lock(_Peers)
			{
				return base.GetPeers().Concat(_PeerSeeds.Select(s => s.Value)).ToList();
			}
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
				return _Peers.Concat(_PeerSeeds).Where(p => !IsFree(p.Value, seedsAsFree)).Count();
			}
		}



		public Peer GetPeer(IPEndPoint endpoint)
		{
			if(endpoint == null)
				throw new ArgumentNullException("endpoint");
			if(endpoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				endpoint = new IPEndPoint(endpoint.Address.MapToIPv6(), endpoint.Port);
			lock(_Peers)
			{
				Peer existing = null;
				_Peers.TryGetValue(endpoint.ToString(), out existing);
				return existing;
			}
		}



		public void RemovePeer(Peer peer)
		{
			lock(_Peers)
			{
				_Peers.Remove(peer.NetworkAddress.Endpoint.ToString());
				_PeerSeeds.Remove(peer.NetworkAddress.Endpoint.ToString());
			}
		}
	}
}
#endif