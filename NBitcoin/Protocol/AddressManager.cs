#if !NOSOCKET
using NBitcoin.Crypto;
using NBitcoin.Protocol.Behaviors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NBitcoin.Logging;

namespace NBitcoin.Protocol
{

	/// <summary>
	/// The AddressManager, keep a set of peers discovered on the network in cache can update their actual states.
	/// Replicate AddressManager of Bitcoin Core, the Buckets and BucketPosition are not guaranteed to be coherent with Bitcoin Core
	/// </summary>
	public class AddressManager : IBitcoinSerializable
	{
		// Serialization versions.
		private const byte V0_HISTORICAL = 0;    // historic format, before bitcoin core commit e6b343d88
		private const byte V1_DETERMINISTIC = 1; // for pre-asmap files
		private const byte V2_ASMAP = 2;         // for files including asmap version
		private const byte V3_BIP155 = 3;        // same as V2_ASMAP plus addresses are in BIP155 format


		public IDnsResolver DnsResolver { get; set; }

		/// <summary>
		/// Will properly convert a endpoint to IPEndpoint
		/// If endpoint is a DNSEndpoint, a DNS resolution will be made and all addresses added
		/// If endpoint is a DNSEndpoint for onion, it will be converted into onioncat address
		/// If endpoint is an IPEndpoint it is added to AddressManager
		/// </summary>
		/// <param name="endpoint">The endpoint to add to the address manager</param>
		/// <param name="source">The source which advertized this endpoint (default: IPAddress.Loopback)</param>
		/// <returns></returns>
		public Task AddAsync(EndPoint endpoint, IPAddress source = null)
		{
			return AddAsync(endpoint, source, default);
		}
		/// <summary>
		/// Will properly convert a endpoint to IPEndpoint
		/// If endpoint is a DNSEndpoint, a DNS resolution will be made and all addresses added
		/// If endpoint is a DNSEndpoint for onion, it will be converted into onioncat address
		/// If endpoint is an IPEndpoint it is added to AddressManager
		/// </summary>
		/// <param name="endpoint">The endpoint to add to the address manager</param>
		/// <param name="source">The source which advertized this endpoint (default: IPAddress.Loopback)</param>
		/// <param name="cancellationToken">The cancellationToken</param>
		/// <returns></returns>
		public async Task AddAsync(EndPoint endpoint, IPAddress source, CancellationToken cancellationToken)
		{
			if (endpoint == null)
				throw new ArgumentNullException(nameof(endpoint));
			if (source == null)
				source = IPAddress.Loopback;
			if (endpoint is DnsEndPoint && !endpoint.IsI2P() && !endpoint.IsTor())
			{
				foreach (var ip in (await endpoint.ResolveToIPEndpointsAsync(DnsResolver, cancellationToken).ConfigureAwait(false)))
				{
					Add(new NetworkAddress(ip), source);
				}
			}
			else
			{
				Add(new NetworkAddress(endpoint), source);
			}
		}
		internal class AddressInfo : IBitcoinSerializable
		{
			#region IBitcoinSerializable Members

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWrite(ref _Address);
				stream.ReadWrite(ref source);
				stream.ReadWrite(ref nLastSuccess);
				stream.ReadWrite(ref nAttempts);
			}

			internal int nAttempts;
			internal long nLastSuccess;
			byte[] source = new byte[16];

			public DateTimeOffset LastSuccess
			{
				get
				{
					return Utils.UnixTimeToDateTime((uint)nLastSuccess);
				}
				set
				{
					nLastSuccess = Utils.DateTimeToUnixTime(value);
				}
			}

			public IPAddress Source
			{
				get
				{
					return new IPAddress(source);
				}
				set
				{
					var ipBytes = value.GetAddressBytes();
					if (ipBytes.Length == 16)
					{
						source = ipBytes;
					}
					else if (ipBytes.Length == 4)
					{
						//Convert to ipv4 mapped to ipv6
						//In these addresses, the first 80 bits are zero, the next 16 bits are one, and the remaining 32 bits are the IPv4 address
						source = new byte[16];
						Array.Copy(ipBytes, 0, source, 12, 4);
						Array.Copy(new byte[] { 0xFF, 0xFF }, 0, source, 10, 2);
					}
					else
						throw new NotSupportedException("Invalid IP address type");
				}
			}

			NetworkAddress _Address;
			public int nRandomPos = -1;
			public int nRefCount;
			public bool fInTried;
			internal long nLastTry;
			internal DateTimeOffset nTime
			{
				get
				{
					return Address.Time;
				}
				set
				{
					Address.Time = value;
				}
			}


			public AddressInfo()
			{

			}
			public AddressInfo(NetworkAddress addr, IPAddress addrSource)
			{
				Address = addr;
				Source = addrSource;
			}

			public bool IsTerrible
			{
				get
				{
					return _IsTerrible(DateTimeOffset.UtcNow);
				}
			}

			internal DateTimeOffset LastTry
			{
				get
				{
					return Utils.UnixTimeToDateTime((uint)nLastSuccess);
				}
				set
				{
					nLastTry = Utils.DateTimeToUnixTime(value);
				}
			}

			public NetworkAddress Address
			{
				get
				{
					return _Address;
				}
				set
				{
					_Address = value;
				}
			}

			#endregion

			internal int GetNewBucket(uint256 nKey)
			{
				return GetNewBucket(nKey, Source);
			}

			internal int GetNewBucket(uint256 nKey, IPAddress src)
			{
				byte[] vchSourceGroupKey = src.GetGroup();
				UInt64 hash1 = Cheap(Hashes.DoubleSHA256(
					nKey.ToBytes(true)
					.Concat(Address.Endpoint.GetGroup())
					.Concat(vchSourceGroupKey)
					.ToArray()));

				UInt64 hash2 = Cheap(Hashes.DoubleSHA256(
					nKey.ToBytes(true)
					.Concat(vchSourceGroupKey)
					.Concat(Utils.ToBytes(hash1 % AddressManager.ADDRMAN_NEW_BUCKETS_PER_SOURCE_GROUP, true))
					.ToArray()));
				return (int)(hash2 % ADDRMAN_NEW_BUCKET_COUNT);
			}

			private ulong Cheap(uint256 v)
			{
				return Utils.ToUInt64(v.ToBytes(), true);
			}

			internal int GetBucketPosition(uint256 nKey, bool fNew, int nBucket)
			{
				UInt64 hash1 = Cheap(
					Hashes.DoubleSHA256(
						nKey.ToBytes()
						.Concat(new byte[] { (fNew ? (byte)'N' : (byte)'K') })
						.Concat(Utils.ToBytes((uint)nBucket, false))
						.Concat(Address.GetKey())
					.ToArray()));
				return (int)(hash1 % ADDRMAN_BUCKET_SIZE);
			}

			internal int GetTriedBucket(uint256 nKey)
			{
				UInt64 hash1 = Cheap(Hashes.DoubleSHA256(nKey.ToBytes().Concat(Address.GetKey()).ToArray()));
				UInt64 hash2 = Cheap(Hashes.DoubleSHA256(nKey.ToBytes().Concat(Address.Endpoint.GetGroup()).Concat(Utils.ToBytes(hash1 % AddressManager.ADDRMAN_TRIED_BUCKETS_PER_GROUP, true)).ToArray()));
				return (int)(hash2 % ADDRMAN_TRIED_BUCKET_COUNT);
			}

			internal bool _IsTerrible(DateTimeOffset now)
			{
				if (nLastTry != 0 && LastTry >= now - TimeSpan.FromSeconds(60)) // never remove things tried in the last minute
					return false;

				if (Address.Time > now + TimeSpan.FromSeconds(10 * 60)) // came in a flying DeLorean
					return true;

				if (Address.nTime == 0 || now - Address.Time > TimeSpan.FromSeconds(ADDRMAN_HORIZON_DAYS * 24 * 60 * 60)) // not seen in recent history
					return true;

				if (nLastSuccess == 0 && nAttempts >= AddressManager.ADDRMAN_RETRIES) // tried N times and never a success
					return true;

				if (now - LastSuccess > TimeSpan.FromSeconds(ADDRMAN_MIN_FAIL_DAYS * 24 * 60 * 60) && nAttempts >= AddressManager.ADDRMAN_MAX_FAILURES) // N successive failures in the last week
					return true;

				return false;
			}

			internal bool Match(NetworkAddress addr)
			{
				return Address.Endpoint.IsEqualTo(addr.Endpoint);
			}

			internal double Chance
			{
				get
				{
					return GetChance(DateTimeOffset.UtcNow);
				}
			}

			//! Calculate the relative chance this entry should be given when selecting nodes to connect to
			internal double GetChance(DateTimeOffset nNow)
			{
				double fChance = 1.0;

				var nSinceLastSeen = nNow - nTime;
				var nSinceLastTry = nNow - LastTry;

				if (nSinceLastSeen < TimeSpan.Zero)
					nSinceLastSeen = TimeSpan.Zero;
				if (nSinceLastTry < TimeSpan.Zero)
					nSinceLastTry = TimeSpan.Zero;

				// deprioritize very recent attempts away
				if (nSinceLastTry < TimeSpan.FromSeconds(60 * 10))
					fChance *= 0.01;

				// deprioritize 66% after each failed attempt, but at most 1/28th to avoid the search taking forever or overly penalizing outages.
				fChance *= Math.Pow(0.66, Math.Min(nAttempts, 8));

				return fChance;
			}
		}

		//! total number of buckets for tried addresses
		internal const int ADDRMAN_TRIED_BUCKET_COUNT = 256;

		//! total number of buckets for new addresses
		internal const int ADDRMAN_NEW_BUCKET_COUNT = 1024;

		//! maximum allowed number of entries in buckets for new and tried addresses
		internal const int ADDRMAN_BUCKET_SIZE = 64;

		//! over how many buckets entries with tried addresses from a single group (/16 for IPv4) are spread
		internal const int ADDRMAN_TRIED_BUCKETS_PER_GROUP = 8;

		//! over how many buckets entries with new addresses originating from a single group are spread
		internal const int ADDRMAN_NEW_BUCKETS_PER_SOURCE_GROUP = 64;

		//! in how many buckets for entries with new addresses a single address may occur
		const int ADDRMAN_NEW_BUCKETS_PER_ADDRESS = 8;

		//! how old addresses can maximally be
		internal const int ADDRMAN_HORIZON_DAYS = 30;

		//! after how many failed attempts we give up on a new node
		internal const int ADDRMAN_RETRIES = 3;

		//! how many successive failures are allowed ...
		internal const int ADDRMAN_MAX_FAILURES = 10;

		//! ... in at least this many days
		internal const int ADDRMAN_MIN_FAIL_DAYS = 7;

		//! the maximum percentage of nodes to return in a getaddr call
		const int ADDRMAN_GETADDR_MAX_PCT = 23;

		//! the maximum number of nodes to return in a getaddr call
		const int ADDRMAN_GETADDR_MAX = 2500;


#if !NOFILEIO
		public static AddressManager LoadPeerFile(string filePath, Network expectedNetwork = null)
		{
			var addrman = new AddressManager();
			byte[] data, hash;
			using (var fs = File.Open(filePath, FileMode.Open, FileAccess.Read))
			{
				data = new byte[fs.Length - 32];
				fs.Read(data, 0, data.Length);
				hash = new byte[32];
				fs.Read(hash, 0, 32);
			}
			var actual = Hashes.DoubleSHA256(data);
			var expected = new uint256(hash);
			if (expected != actual)
				throw new FormatException("Invalid address manager file");

			BitcoinStream stream = new BitcoinStream(data);
			stream.Type = SerializationType.Disk;
			uint magic = 0;
			stream.ReadWrite(ref magic);
			if (expectedNetwork != null && expectedNetwork.Magic != magic)
			{
				throw new FormatException("This file is not for the expected network");
			}
			addrman.ReadWrite(stream);
			return addrman;
		}
		public void SavePeerFile(string filePath, Network network)
		{
			if (network == null)
				throw new ArgumentNullException(nameof(network));
			if (filePath == null)
				throw new ArgumentNullException(nameof(filePath));

			MemoryStream ms = new MemoryStream();
			BitcoinStream stream = new BitcoinStream(ms, true);
			stream.Type = SerializationType.Disk;
			stream.ReadWrite(network.Magic);
			stream.ReadWrite(this);
			var hash = Hashes.DoubleSHA256(ms.ToArray());
			stream.ReadWrite(hash.AsBitcoinSerializable());

			string dirPath = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrWhiteSpace(dirPath))
			{
				Directory.CreateDirectory(dirPath);
			}
			File.WriteAllBytes(filePath, ms.ToArray());
		}
#endif

		AddressInfo Find(NetworkAddress addr)
		{
			int unused;
			return Find(addr, out unused);
		}
		AddressInfo Find(NetworkAddress addr, out int pnId)
		{
			if (!mapAddr.TryGetValue(addr.ToAddressString(), out pnId))
				return null;
			return mapInfo.TryGet(pnId);
		}
		public AddressManager()
		{
			Clear();
		}


		private void Clear()
		{
			vRandom = new List<int>();
			nKey = new uint256(RandomUtils.GetBytes(32));
			vvNew = new int[ADDRMAN_NEW_BUCKET_COUNT, ADDRMAN_BUCKET_SIZE];
			for (int i = 0; i < ADDRMAN_NEW_BUCKET_COUNT; i++)
				for (int j = 0; j < ADDRMAN_BUCKET_SIZE; j++)
					vvNew[i, j] = -1;

			vvTried = new int[ADDRMAN_TRIED_BUCKET_COUNT, ADDRMAN_BUCKET_SIZE];
			for (int i = 0; i < ADDRMAN_TRIED_BUCKET_COUNT; i++)
				for (int j = 0; j < ADDRMAN_BUCKET_SIZE; j++)
					vvTried[i, j] = -1;

			nIdCount = 0;
			nTried = 0;
			nNew = 0;
		}

		byte nVersion = V3_BIP155;
		byte nKeySize = 32;
		internal uint256 nKey;
		internal int nNew;
		internal int nTried;
		List<int> vRandom;

		int[,] vvNew;
		int[,] vvTried;

		public int DiscoveredPeers => _DiscoveredPeers;
		int _DiscoveredPeers; // no auto-property because used as ref parameter below
		public int NeededPeers { get; private set; }

		Dictionary<int, AddressInfo> mapInfo = new Dictionary<int, AddressInfo>();
		Dictionary<string, int> mapAddr = new Dictionary<string, int>();
		private int nIdCount;

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			lock (cs)
			{
				Check();
				if (!stream.Serializing)
					Clear();
				stream.ReadWrite(ref nVersion);
				if (nVersion >= V3_BIP155)
					stream.ProtocolVersion = (stream.ProtocolVersion ?? 0) | NetworkAddress.AddrV2Format;
				stream.ReadWrite(ref nKeySize);
				if (!stream.Serializing && nKeySize != 32)
					throw new FormatException("Incorrect keysize in addrman deserialization");
				stream.ReadWrite(ref nKey);
				stream.ReadWrite(ref nNew);
				stream.ReadWrite(ref nTried);

				int nUBuckets = ADDRMAN_NEW_BUCKET_COUNT ^ (1 << 30);
				stream.ReadWrite(ref nUBuckets);
				if (nVersion > V0_HISTORICAL)
				{
					nUBuckets ^= (1 << 30);
				}
				if (!stream.Serializing)
				{
					// Deserialize entries from the new table.
					for (int n = 0; n < nNew; n++)
					{
						AddressInfo info = new AddressInfo();
						info.ReadWrite(stream);
						mapInfo.Add(n, info);
						mapAddr[info.Address.ToAddressString()] = n;
						info.nRandomPos = vRandom.Count;
						vRandom.Add(n);
						if (nVersion == V0_HISTORICAL || nUBuckets != ADDRMAN_NEW_BUCKET_COUNT)
						{
							// In case the new table data cannot be used (nVersion unknown/historical, or bucket count wrong),
							// immediately try to give them a reference based on their primary source address.
							int nUBucket = info.GetNewBucket(nKey);
							int nUBucketPos = info.GetBucketPosition(nKey, true, nUBucket);
							if (vvNew[nUBucket, nUBucketPos] == -1)
							{
								vvNew[nUBucket, nUBucketPos] = n;
								info.nRefCount++;
							}
						}
					}

					nIdCount = nNew;

					// Deserialize entries from the tried table.
					int nLost = 0;
					for (int n = 0; n < nTried; n++)
					{
						AddressInfo info = new AddressInfo();
						info.ReadWrite(stream);
						int nKBucket = info.GetTriedBucket(nKey);
						int nKBucketPos = info.GetBucketPosition(nKey, false, nKBucket);
						if (vvTried[nKBucket, nKBucketPos] == -1)
						{
							info.nRandomPos = vRandom.Count;
							info.fInTried = true;
							vRandom.Add(nIdCount);
							mapInfo[nIdCount] = info;
							mapAddr[info.Address.ToAddressString()] = nIdCount;
							vvTried[nKBucket, nKBucketPos] = nIdCount;
							nIdCount++;
						}
						else
						{
							nLost++;
						}
					}

					nTried -= nLost;

					// Deserialize positions in the new table (if possible).
					for (int bucket = 0; bucket < nUBuckets; bucket++)
					{
						int nSize = 0;
						stream.ReadWrite(ref nSize);
						for (int n = 0; n < nSize; n++)
						{
							int nIndex = 0;
							stream.ReadWrite(ref nIndex);
							if (nIndex >= 0 && nIndex < nNew)
							{
								AddressInfo info = mapInfo[nIndex];
								int nUBucketPos = info.GetBucketPosition(nKey, true, bucket);
								if (nVersion >= V1_DETERMINISTIC && nUBuckets == ADDRMAN_NEW_BUCKET_COUNT && vvNew[bucket, nUBucketPos] == -1 && info.nRefCount < ADDRMAN_NEW_BUCKETS_PER_ADDRESS)
								{
									info.nRefCount++;
									vvNew[bucket, nUBucketPos] = nIndex;
								}
							}
						}
					}

					// Prune new entries with refcount 0 (as a result of collisions).
					int nLostUnk = 0;
					foreach (var kv in mapInfo.ToList())
					{
						if (kv.Value.fInTried == false && kv.Value.nRefCount == 0)
						{
							Delete(kv.Key);
							nLostUnk++;
						}
					}
				}
				else
				{
					Dictionary<int, int> mapUnkIds = new Dictionary<int, int>();
					int nIds = 0;
					foreach (var kv in mapInfo)
					{
						mapUnkIds[kv.Key] = nIds;
						AddressInfo info = kv.Value;
						if (info.nRefCount != 0)
						{
							assert(nIds != nNew); // this means nNew was wrong, oh ow
							info.ReadWrite(stream);
							nIds++;
						}
					}
					nIds = 0;
					foreach (var kv in mapInfo)
					{
						AddressInfo info = kv.Value;
						if (info.fInTried)
						{
							assert(nIds != nTried); // this means nTried was wrong, oh ow
							info.ReadWrite(stream);
							nIds++;
						}
					}

					for (int bucket = 0; bucket < ADDRMAN_NEW_BUCKET_COUNT; bucket++)
					{
						int nSize = 0;
						for (int i = 0; i < ADDRMAN_BUCKET_SIZE; i++)
						{
							if (vvNew[bucket, i] != -1)
								nSize++;
						}
						stream.ReadWrite(ref nSize);
						for (int i = 0; i < ADDRMAN_BUCKET_SIZE; i++)
						{
							if (vvNew[bucket, i] != -1)
							{
								int nIndex = mapUnkIds[vvNew[bucket, i]];
								stream.ReadWrite(ref nIndex);
							}
						}
					}
				}
				Check();
			}
		}

		#endregion

		//! Add a single address.
		public bool Add(NetworkAddress addr, IPAddress source)
		{
			return Add(addr, source, TimeSpan.Zero);
		}

		object cs = new object();
		public bool Add(NetworkAddress addr)
		{
			return Add(addr, IPAddress.Loopback);
		}
		public bool Add(NetworkAddress addr, IPAddress source, TimeSpan nTimePenalty)
		{
			bool fRet = false;
			lock (cs)
			{
				Check();
				fRet |= Add_(addr, source, nTimePenalty);
				Check();
			}
			return fRet;
		}
		public bool Add(IEnumerable<NetworkAddress> vAddr, IPAddress source)
		{
			return Add(vAddr, source, TimeSpan.FromSeconds(0));
		}
		public bool Add(IEnumerable<NetworkAddress> vAddr, IPAddress source, TimeSpan nTimePenalty)
		{
			int nAdd = 0;
			lock (cs)
			{
				Check();
				foreach (var addr in vAddr)
					nAdd += Add_(addr, source, nTimePenalty) ? 1 : 0;
				Check();
			}
			return nAdd > 0;
		}

		private bool Add_(NetworkAddress addr, IPAddress source, TimeSpan nTimePenalty)
		{
			if (!addr.Endpoint.IsRoutable(true))
				return false;

			bool fNew = false;
			int nId;
			AddressInfo pinfo = Find(addr, out nId);
			if (pinfo != null)
			{
				// periodically update nTime
				bool fCurrentlyOnline = (DateTimeOffset.UtcNow - addr.Time < TimeSpan.FromSeconds(24 * 60 * 60));
				var nUpdateInterval = TimeSpan.FromSeconds(fCurrentlyOnline ? 60 * 60 : 24 * 60 * 60);
				if (addr.nTime != 0 && (pinfo.Address.nTime == 0 || pinfo.Address.Time < addr.Time - nUpdateInterval - nTimePenalty))
					pinfo.Address.nTime = (uint)Math.Max(0L, (long)Utils.DateTimeToUnixTime(addr.Time - nTimePenalty));

				// add services
				pinfo.Address.Services |= addr.Services;

				// do not update if no new information is present
				if (addr.nTime == 0 || (pinfo.Address.nTime != 0 && addr.Time <= pinfo.Address.Time))
					return false;

				// do not update if the entry was already in the "tried" table
				if (pinfo.fInTried)
					return false;

				// do not update if the max reference count is reached
				if (pinfo.nRefCount == ADDRMAN_NEW_BUCKETS_PER_ADDRESS)
					return false;

				// stochastic test: previous nRefCount == N: 2^N times harder to increase it
				int nFactor = 1;
				for (int n = 0; n < pinfo.nRefCount; n++)
					nFactor *= 2;
				if (nFactor > 1 && (GetRandInt(nFactor) != 0))
					return false;
			}
			else
			{
				pinfo = Create(addr, source, out nId);
				pinfo.Address.nTime = (uint)Math.Max((long)0, (long)Utils.DateTimeToUnixTime(pinfo.Address.Time - nTimePenalty));
				nNew++;
				fNew = true;
			}

			int nUBucket = pinfo.GetNewBucket(nKey, source);
			int nUBucketPos = pinfo.GetBucketPosition(nKey, true, nUBucket);
			if (vvNew[nUBucket, nUBucketPos] != nId)
			{
				bool fInsert = vvNew[nUBucket, nUBucketPos] == -1;
				if (!fInsert)
				{
					AddressInfo infoExisting = mapInfo[vvNew[nUBucket, nUBucketPos]];
					if (infoExisting.IsTerrible || (infoExisting.nRefCount > 1 && pinfo.nRefCount == 0))
					{
						// Overwrite the existing new table entry.
						fInsert = true;
					}
				}
				if (fInsert)
				{
					ClearNew(nUBucket, nUBucketPos);
					pinfo.nRefCount++;
					vvNew[nUBucket, nUBucketPos] = nId;
				}
				else
				{
					if (pinfo.nRefCount == 0)
					{
						Delete(nId);
					}
				}
			}
			return fNew;
		}

		private void ClearNew(int nUBucket, int nUBucketPos)
		{
			// if there is an entry in the specified bucket, delete it.
			if (vvNew[nUBucket, nUBucketPos] != -1)
			{
				int nIdDelete = vvNew[nUBucket, nUBucketPos];
				AddressInfo infoDelete = mapInfo[nIdDelete];
				assert(infoDelete.nRefCount > 0);
				infoDelete.nRefCount--;
				infoDelete.nRefCount = Math.Max(0, infoDelete.nRefCount);
				vvNew[nUBucket, nUBucketPos] = -1;
				if (infoDelete.nRefCount == 0)
				{
					Delete(nIdDelete);
				}
			}
		}

		private void Delete(int nId)
		{
			assert(mapInfo.ContainsKey(nId));
			AddressInfo info = mapInfo[nId];
			assert(!info.fInTried);
			assert(info.nRefCount == 0);

			SwapRandom(info.nRandomPos, vRandom.Count - 1);
			vRandom.RemoveAt(vRandom.Count - 1);
			mapAddr.Remove(info.Address.ToAddressString());
			mapInfo.Remove(nId);
			nNew--;


		}

		private void SwapRandom(int nRndPos1, int nRndPos2)
		{
			if (nRndPos1 == nRndPos2)
				return;

			assert(nRndPos1 < vRandom.Count && nRndPos2 < vRandom.Count);

			int nId1 = vRandom[nRndPos1];
			int nId2 = vRandom[nRndPos2];

			assert(mapInfo.ContainsKey(nId1));
			assert(mapInfo.ContainsKey(nId2));

			mapInfo[nId1].nRandomPos = nRndPos2;
			mapInfo[nId2].nRandomPos = nRndPos1;

			vRandom[nRndPos1] = nId2;
			vRandom[nRndPos2] = nId1;
		}

		private AddressInfo Create(NetworkAddress addr, IPAddress addrSource, out int pnId)
		{
			int nId = nIdCount++;
			mapInfo[nId] = new AddressInfo(addr, addrSource);
			mapAddr[addr.ToAddressString()] = nId;
			mapInfo[nId].nRandomPos = vRandom.Count;
			vRandom.Add(nId);
			pnId = nId;
			return mapInfo[nId];
		}

		internal bool DebugMode
		{
			get;
			set;
		}

		internal void Check()
		{
			if (!DebugMode)
				return;
			lock (cs)
			{
				assert(Check_() == 0);
			}
		}

		private int Check_()
		{
			List<int> setTried = new List<int>();
			Dictionary<int, int> mapNew = new Dictionary<int, int>();

			if (vRandom.Count != nTried + nNew)
				return -7;

			foreach (var kv in mapInfo)
			{
				int n = kv.Key;
				AddressInfo info = kv.Value;
				if (info.fInTried)
				{
					if (info.nLastSuccess == 0)
						return -1;
					if (info.nRefCount != 0)
						return -2;
					setTried.Add(n);
				}
				else
				{
					if (info.nRefCount < 0 || info.nRefCount > ADDRMAN_NEW_BUCKETS_PER_ADDRESS)
						return -3;
					if (info.nRefCount == 0)
						return -4;
					mapNew[n] = info.nRefCount;
				}
				if (mapAddr[info.Address.ToAddressString()] != n)
					return -5;
				if (info.nRandomPos < 0 || info.nRandomPos >= vRandom.Count || vRandom[info.nRandomPos] != n)
					return -14;
				if (info.nLastTry < 0)
					return -6;
				if (info.nLastSuccess < 0)
					return -8;
			}
			if (setTried.Count != nTried)
				return -9;
			if (mapNew.Count != nNew)
				return -10;

			for (int n = 0; n < ADDRMAN_TRIED_BUCKET_COUNT; n++)
			{
				for (int i = 0; i < ADDRMAN_BUCKET_SIZE; i++)
				{
					if (vvTried[n, i] != -1)
					{
						if (!setTried.Contains(vvTried[n, i]))
							return -11;
						if (mapInfo[vvTried[n, i]].GetTriedBucket(nKey) != n)
							return -17;
						if (mapInfo[vvTried[n, i]].GetBucketPosition(nKey, false, n) != i)
							return -18;
						setTried.Remove(vvTried[n, i]);
					}
				}
			}

			for (int n = 0; n < ADDRMAN_NEW_BUCKET_COUNT; n++)
			{
				for (int i = 0; i < ADDRMAN_BUCKET_SIZE; i++)
				{
					if (vvNew[n, i] != -1)
					{
						if (!mapNew.ContainsKey(vvNew[n, i]))
							return -12;
						if (mapInfo[vvNew[n, i]].GetBucketPosition(nKey, true, n) != i)
							return -19;
						if (--mapNew[vvNew[n, i]] == 0)
							mapNew.Remove(vvNew[n, i]);
					}
				}
			}

			if (setTried.Count != 0)
				return -13;
			if (mapNew.Count != 0)
				return -15;
			if (nKey == null || nKey == uint256.Zero)
				return -16;
			return 0;
		}



		public void Good(NetworkAddress addr)
		{
			Good(addr, DateTimeOffset.UtcNow);
		}

		public void Good(NetworkAddress addr, DateTimeOffset nTime)
		{
			lock (cs)
			{
				Check();
				Good_(addr, nTime);
				Check();
			}
		}

		private void Good_(NetworkAddress addr, DateTimeOffset nTime)
		{
			int nId;
			AddressInfo pinfo = Find(addr, out nId);

			// if not found, bail out
			if (pinfo == null)
				return;

			AddressInfo info = pinfo;

			// check whether we are talking about the exact same CService (including same port)
			if (!info.Match(addr))
				return;

			// update info
			info.LastSuccess = nTime;
			info.LastTry = nTime;
			info.nAttempts = 0;
			// nTime is not updated here, to avoid leaking information about
			// currently-connected peers.

			// if it is already in the tried set, don't do anything else
			if (info.fInTried)
				return;

			// find a bucket it is in now
			int nRnd = GetRandInt(ADDRMAN_NEW_BUCKET_COUNT);
			int nUBucket = -1;
			for (int n = 0; n < ADDRMAN_NEW_BUCKET_COUNT; n++)
			{
				int nB = (n + nRnd) % ADDRMAN_NEW_BUCKET_COUNT;
				int nBpos = info.GetBucketPosition(nKey, true, nB);
				if (vvNew[nB, nBpos] == nId)
				{
					nUBucket = nB;
					break;
				}
			}

			// if no bucket is found, something bad happened;
			// TODO: maybe re-add the node, but for now, just bail out
			if (nUBucket == -1)
				return;

			// move nId to the tried tables
			MakeTried(info, nId);
		}

		private void MakeTried(AddressInfo info, int nId)
		{
			// remove the entry from all new buckets
			for (int bucket = 0; bucket < ADDRMAN_NEW_BUCKET_COUNT; bucket++)
			{
				int pos = info.GetBucketPosition(nKey, true, bucket);
				if (vvNew[bucket, pos] == nId)
				{
					vvNew[bucket, pos] = -1;
					info.nRefCount--;
				}
			}
			nNew--;

			assert(info.nRefCount == 0);

			// which tried bucket to move the entry to
			int nKBucket = info.GetTriedBucket(nKey);
			int nKBucketPos = info.GetBucketPosition(nKey, false, nKBucket);

			// first make space to add it (the existing tried entry there is moved to new, deleting whatever is there).
			if (vvTried[nKBucket, nKBucketPos] != -1)
			{
				// find an item to evict
				int nIdEvict = vvTried[nKBucket, nKBucketPos];
				assert(mapInfo.ContainsKey(nIdEvict));
				AddressInfo infoOld = mapInfo[nIdEvict];

				// Remove the to-be-evicted item from the tried set.
				infoOld.fInTried = false;
				vvTried[nKBucket, nKBucketPos] = -1;
				nTried--;

				// find which new bucket it belongs to
				int nUBucket = infoOld.GetNewBucket(nKey);
				int nUBucketPos = infoOld.GetBucketPosition(nKey, true, nUBucket);
				ClearNew(nUBucket, nUBucketPos);
				assert(vvNew[nUBucket, nUBucketPos] == -1);

				// Enter it into the new set again.
				infoOld.nRefCount = 1;
				vvNew[nUBucket, nUBucketPos] = nIdEvict;
				nNew++;
			}
			assert(vvTried[nKBucket, nKBucketPos] == -1);

			vvTried[nKBucket, nKBucketPos] = nId;
			nTried++;
			info.fInTried = true;
		}

		private static void assert(bool value)
		{
			if (!value)
				throw new InvalidOperationException("Bug in AddressManager, should never happen, contact NBitcoin developers if you see this exception");
		}
		//! Mark an entry as connection attempted to.
		public void Attempt(NetworkAddress addr)
		{
			Attempt(addr, DateTimeOffset.UtcNow);
		}
		//! Mark an entry as connection attempted to.
		public void Attempt(NetworkAddress addr, DateTimeOffset nTime)
		{
			lock (cs)
			{
				Check();
				Attempt_(addr, nTime);
				Check();
			}
		}

		private void Attempt_(NetworkAddress addr, DateTimeOffset nTime)
		{
			AddressInfo pinfo = Find(addr);

			// if not found, bail out
			if (pinfo == null)
				return;

			AddressInfo info = pinfo;

			// check whether we are talking about the exact same CService (including same port)
			if (!info.Match(addr))
				return;

			// update info
			info.LastTry = nTime;
			info.nAttempts++;
		}

		//! Mark an entry as currently-connected-to.
		public void Connected(NetworkAddress addr)
		{
			Connected(addr, DateTimeOffset.UtcNow);
		}

		//! Mark an entry as currently-connected-to.
		public void Connected(NetworkAddress addr, DateTimeOffset nTime)
		{
			lock (cs)
			{
				Check();
				Connected_(addr, nTime);
				Check();
			}
		}
		void Connected_(NetworkAddress addr, DateTimeOffset nTime)
		{
			int unused;
			AddressInfo pinfo = Find(addr, out unused);

			// if not found, bail out
			if (pinfo == null)
				return;

			AddressInfo info = pinfo;

			// check whether we are talking about the exact same CService (including same port)
			if (!info.Match(addr))
				return;

			// update info
			var nUpdateInterval = TimeSpan.FromSeconds(20 * 60);
			if (nTime - info.nTime > nUpdateInterval)
				info.nTime = nTime;
		}

		/// <summary>
		/// Choose an address to connect to.
		/// </summary>
		/// <returns>The network address of a peer, or null if none are found</returns>
		public NetworkAddress Select()
		{
			AddressInfo addrRet = null;
			lock (cs)
			{
				Check();
				addrRet = Select_();
				Check();
			}
			return addrRet == null ? null : addrRet.Address;
		}

		private AddressInfo Select_()
		{
			if (vRandom.Count == 0)
				return null;

			var rnd = new Random();

			// Use a 50% chance for choosing between tried and new table entries.
			if (nTried > 0 && (nNew == 0 || GetRandInt(2) == 0))
			{
				// use a tried node
				double fChanceFactor = 1.0;
				while (true)
				{
					int nKBucket = GetRandInt(ADDRMAN_TRIED_BUCKET_COUNT);
					int nKBucketPos = GetRandInt(ADDRMAN_BUCKET_SIZE);
					while (vvTried[nKBucket, nKBucketPos] == -1)
					{
						nKBucket = (nKBucket + rnd.Next(ADDRMAN_TRIED_BUCKET_COUNT)) % ADDRMAN_TRIED_BUCKET_COUNT;
						nKBucketPos = (nKBucketPos + rnd.Next(ADDRMAN_BUCKET_SIZE)) % ADDRMAN_BUCKET_SIZE;
					}
					int nId = vvTried[nKBucket, nKBucketPos];
					assert(mapInfo.ContainsKey(nId));
					AddressInfo info = mapInfo[nId];
					if (GetRandInt(1 << 30) < fChanceFactor * info.Chance * (1 << 30))
						return info;
					fChanceFactor *= 1.2;
				}
			}
			else
			{
				// use a new node
				double fChanceFactor = 1.0;
				while (true)
				{
					int nUBucket = GetRandInt(ADDRMAN_NEW_BUCKET_COUNT);
					int nUBucketPos = GetRandInt(ADDRMAN_BUCKET_SIZE);
					while (vvNew[nUBucket, nUBucketPos] == -1)
					{
						nUBucket = (nUBucket + rnd.Next(ADDRMAN_NEW_BUCKET_COUNT)) % ADDRMAN_NEW_BUCKET_COUNT;
						nUBucketPos = (nUBucketPos + rnd.Next(ADDRMAN_BUCKET_SIZE)) % ADDRMAN_BUCKET_SIZE;
					}
					int nId = vvNew[nUBucket, nUBucketPos];
					assert(mapInfo.ContainsKey(nId));
					AddressInfo info = mapInfo[nId];
					if (GetRandInt(1 << 30) < fChanceFactor * info.Chance * (1 << 30))
						return info;
					fChanceFactor *= 1.2;
				}
			}
		}

		private static int GetRandInt(int max)
		{
			return (int)(RandomUtils.GetUInt32() % (uint)max);
		}

		/// <summary>
		/// Return a bunch of addresses, selected at random.
		/// </summary>
		/// <returns></returns>
		public NetworkAddress[] GetAddr()
		{
			NetworkAddress[] result = null;
			lock (cs)
			{
				Check();
				result = GetAddr_().ToArray();
				Check();
			}
			return result;
		}
		IEnumerable<NetworkAddress> GetAddr_()
		{
			List<NetworkAddress> vAddr = new List<NetworkAddress>();
			int nNodes = ADDRMAN_GETADDR_MAX_PCT * vRandom.Count / 100;
			if (nNodes > ADDRMAN_GETADDR_MAX)
				nNodes = ADDRMAN_GETADDR_MAX;
			// gather a list of random nodes, skipping those of low quality
			for (int n = 0; n < vRandom.Count; n++)
			{
				if (vAddr.Count >= nNodes)
					break;

				int nRndPos = GetRandInt(vRandom.Count - n) + n;
				SwapRandom(n, nRndPos);
				assert(mapInfo.ContainsKey(vRandom[n]));

				AddressInfo ai = mapInfo[vRandom[n]];
				if (!ai.IsTerrible)
					vAddr.Add(ai.Address);
			}
			return vAddr;
		}

		public int Count
		{
			get
			{
				return vRandom.Count;
			}
		}

		internal void DiscoverPeers(Network network, NodeConnectionParameters parameters, int peerToFind)
		{
			TimeSpan backoff = TimeSpan.Zero;
			Logs.NodeServer.LogTrace("Discovering nodes");

			_DiscoveredPeers = 0;
			NeededPeers = peerToFind;

			{
				while (_DiscoveredPeers < peerToFind)
				{
					Thread.Sleep(backoff);
					backoff = backoff == TimeSpan.Zero ? TimeSpan.FromSeconds(1.0) : TimeSpan.FromSeconds(backoff.TotalSeconds * 2);
					if (backoff > TimeSpan.FromSeconds(10.0))
						backoff = TimeSpan.FromSeconds(10.0);

					parameters.ConnectCancellation.ThrowIfCancellationRequested();

					Logs.NodeServer.LogTrace("Remaining peer to get {remainingPeerCount}", (-_DiscoveredPeers + peerToFind));

					List<NetworkAddress> peers = new List<NetworkAddress>();
					peers.AddRange(this.GetAddr());
					if (peers.Count == 0)
					{
						PopulateTableWithDNSNodes(network, peers, parameters.ConnectCancellation).GetAwaiter().GetResult();
						PopulateTableWithHardNodes(network, peers);
						peers = new List<NetworkAddress>(peers.OrderBy(a => RandomUtils.GetInt32()));
						if (peers.Count == 0)
							return;
					}

					CancellationTokenSource peerTableFull = new CancellationTokenSource();
					CancellationToken loopCancel = CancellationTokenSource.CreateLinkedTokenSource(peerTableFull.Token, parameters.ConnectCancellation).Token;
					try
					{
						Parallel.ForEach(peers, new ParallelOptions()
						{
							MaxDegreeOfParallelism = 5,
							CancellationToken = loopCancel,
						}, p =>
						{
							using (CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
							using (var cancelConnection = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, loopCancel))
							{
								Node n = null;
								try
								{
									var param2 = parameters.Clone();
									param2.ConnectCancellation = cancelConnection.Token;
									var addrman = param2.TemplateBehaviors.Find<AddressManagerBehavior>();
									var socks = param2.TemplateBehaviors.Find<SocksSettingsBehavior>();
									param2.TemplateBehaviors.Clear();
									param2.TemplateBehaviors.Add(addrman);
									if (socks != null)
										param2.TemplateBehaviors.Add(socks);
									n = Node.Connect(network, p, param2);
									n.VersionHandshake(cancelConnection.Token);
									n.MessageReceived += (s, a) =>
									{
										var addr = (a.Message.Payload as AddrPayload);
										if (addr != null)
										{
											Interlocked.Add(ref _DiscoveredPeers, addr.Addresses.Length);
											backoff = TimeSpan.FromSeconds(0);
											try
											{
												cancelConnection.Cancel();
											}
											catch { }
											if (_DiscoveredPeers >= peerToFind)
												peerTableFull.Cancel();
										}
									};
									n.SendMessageAsync(new GetAddrPayload());
									cancelConnection.Token.WaitHandle.WaitOne(2000);
								}
								catch
								{
								}
								finally
								{
									if (n != null)
										n.DisconnectAsync();
								}
							}
							if (_DiscoveredPeers >= peerToFind)
								peerTableFull.Cancel();
							else
								Logs.NodeServer.LogInformation("Need {neededPeerCount} more peers", (-_DiscoveredPeers + peerToFind));
						});
					}
					catch (OperationCanceledException)
					{
						if (parameters.ConnectCancellation.IsCancellationRequested)
							throw;
					}
				}
			}
		}

		private async Task PopulateTableWithDNSNodes(Network network, List<NetworkAddress> peers, CancellationToken cancellationToken)
		{
			var result = await Task.WhenAll(network.DNSSeeds
				.Select(async dns =>
				{
					try
					{
						return (await dns.GetAddressNodesAsync(network.DefaultPort, DnsResolver, cancellationToken).ConfigureAwait(false)).Select(o => new NetworkAddress(o)).ToArray();
					}
					catch
					{
						return new NetworkAddress[0];
					}
				})
				.ToArray()).ConfigureAwait(false);

				peers.AddRange(result.SelectMany(x => x));
		}

		private static void PopulateTableWithHardNodes(Network network, List<NetworkAddress> peers)
		{
			peers.AddRange(network.SeedNodes);
		}
	}
}
#endif
