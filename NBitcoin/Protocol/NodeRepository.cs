using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class NodeRepository
	{
		Dictionary<uint256, IBitcoinSerializable> _Index = new Dictionary<uint256, IBitcoinSerializable>();
		public bool UpdateAddr(NetworkAddress address)
		{
			lock(_Index)
			{
				IBitcoinSerializable existing = null;
				var hash = GetAddressHash(address);
				if(_Index.TryGetValue(hash, out existing))
				{
					var old = (NetworkAddress)existing;
					if(old.Time < address.Time)
					{
						_Index.Remove(hash);
						_Index.Add(hash, address);
						return true;
					}
				}
				else
				{
					_Index.Add(hash, address);
					return true;
				}
			}
			return false;
		}

		private uint256 GetAddressHash(NetworkAddress address)
		{
			var old = address.Time;
			address.Time = Utils.UnixTimeToDateTime(0);
			try
			{
				return GetHash(address);
			}
			finally
			{
				address.Time = old;
			}
		}
		private uint256 GetHash(IBitcoinSerializable obj)
		{
			var bytes = obj.ToBytes();
			return Hashes.Hash256(bytes);
		}
		public IEnumerable<NetworkAddress> GetActiveAddresses()
		{
			lock(_Index)
			{
				var now = DateTimeOffset.Now;
				var threeH = TimeSpan.FromHours(3.0);
				return _Index.Values.OfType<NetworkAddress>().Where(v => (now - v.Time) <= threeH).OrderByDescending(v => v.Time).ToList();
			}
		}
		public NetworkAddress GetAddress(IPEndPoint endpoint)
		{
			IBitcoinSerializable result = null;
			lock(_Index)
			{
				NetworkAddress addr = new NetworkAddress();
				addr.Endpoint = endpoint;
				var hash = GetAddressHash(addr);
				_Index.TryGetValue(hash, out result);
				return (NetworkAddress)result;
			}
		}
	}
}
