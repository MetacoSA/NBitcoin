using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.Crypto;
using NBitcoin.Protocol;

namespace NBitcoin
{
	/// <summary>
	/// Implements a Golomb-coded set to be use in the creation of client-side filter
	/// for a new kind Bitcoin light clients. This code is based on the BIP:
	/// https://github.com/Roasbeef/bips/blob/master/gcs_light_client.mediawiki
	/// </summary>
	public class GolombRiceFilter
	{
		public byte P { get; }
		public int N { get; }
		public ulong ModulusP { get;  }
		public ulong ModulusNP { get; }
		public byte[] Data { get;  }

		internal GolombRiceFilter(byte[] data, int n, byte p)
		{
			this.P = p;
			this.N = n;

			var modP = 1UL << P;
			this.ModulusP = modP;
			this.ModulusNP = ((ulong) N) * modP;
			this.Data = data;
		}

		internal static List<ulong> ConstructHashedSet(byte P, int N, byte[] key, IEnumerable<byte[]> data)
		{
			// N the number of items to be inserted into the set.
			var dataArrayBytes = data as byte[][] ?? data.ToArray();

			// The list of data item hashes.
			var values = new List<ulong>();
			var modP = 1UL << P;
			var modNP = ((ulong)N) * modP;
			var nphi = modNP >> 32;
			var nplo = (ulong)((uint)modNP);

			// Process the data items and calculate the 64 bits hash for each of them.
			foreach(var item in dataArrayBytes )
			{
				var hash = SipHash(key, item);
				var value = FastReduction(hash, nphi, nplo);
				values.Add(value);
			}

			values.Sort();
			return values;
		}

		internal uint256 GetHeader(uint256 previousHeader)
		{
			var curFilterHashBytes = Hashes.Hash256(ToBytes()).ToBytes();
			var prvFilterHashBytes = previousHeader.ToBytes();
			return Hashes.Hash256(curFilterHashBytes.Concat(prvFilterHashBytes));
		}

		private static ulong SipHash(byte[] key, byte[] data)
		{
			var k0 = BitConverter.ToUInt64(key, 0);
			var k1 = BitConverter.ToUInt64(key, 8);

			var hasher = new Hashes.SipHasher(k0, k1);
			hasher.Write(data);
			return hasher.Finalize();
		}

		public bool Match(byte[] data, byte[] key)
		{
			return MatchAny(new []{data}, key);
		}

		public bool MatchAny(IEnumerable<byte[]> data, byte[] key)
		{
			if (data == null || !data.Any())
				throw new ArgumentException("data can not be null or empty array.", nameof(data));

			var hs = ConstructHashedSet(P, N, key, data);

			var lastValue1 = 0UL;
			var lastValue2 = hs[0];
			var i = 1;

			var bitStream = new BitStream(Data);
			var sr = new GRCodedStreamReader(bitStream, P, 0);

			try
			{
				while (lastValue1 != lastValue2)
				{
					if (lastValue1 > lastValue2)
					{
						if (i < hs.Count)
						{
							lastValue2 = hs[i];
							i++;
						}
						else
						{
							return false;
						}
					}
					else if (lastValue2 > lastValue1)
					{
						var val = sr.Read();
						lastValue1 = val;
					}
				}
			}
			catch (ArgumentOutOfRangeException) // end-of-stream 
			{
				return false;
			}

			return true;
		}

		public byte[] ToBytes()
		{
			var n = new VarInt((ulong)N).ToBytes();
			return n.Concat(this.Data);
		}

		public override string ToString()
		{
			return DataEncoders.Encoders.Hex.EncodeData(ToBytes());
		}

		internal static ulong FastReduction(ulong value, ulong nhi, ulong nlo)
		{
			// First, we'll spit the item we need to reduce into its higher and lower bits.
			var vhi = value >> 32;
			var vlo = (ulong)((uint)value);

			// Then, we distribute multiplication over each part.
			var vnphi = vhi * nhi;
			var vnpmid = vhi * nlo;
			var npvmid = nhi * vlo;
			var vnplo = vlo * nlo;

			// We calculate the carry bit.
			var carry = ((ulong)((uint)vnpmid) + (ulong)((uint)npvmid) +
			(vnplo >> 32)) >> 32;

			// Last, we add the high bits, the middle bits, and the carry.
			value = vnphi + (vnpmid >> 32) + (npvmid >> 32) + carry;

			return value;
		}
	}

	public class GolombRiceFilterBuilder
	{
		private const byte DefaultP = 20;
		private byte _p = DefaultP;
		private byte[] _key;
		private HashSet<byte[]> _values;
		
		class ByteArrayComparer : IEqualityComparer<byte[]> {
			public bool Equals(byte[] a, byte[] b)
			{
				if (a.Length != b.Length) return false;
				for (int i = 0; i < a.Length; i++)
					if (a[i] != b[i]) return false;
				return true;
			}
			public int GetHashCode(byte[] a)
			{
				uint b = 0;
				for (int i = 0; i < a.Length; i++)
					b = ((b << 23) | (b >> 9)) ^ a[i];
				return unchecked((int)b);
			}
		}

		public static GolombRiceFilter BuildBasicFilter(Block block)
		{
			var builder = new GolombRiceFilterBuilder()
				.SetKey(block.GetHash());

			foreach(var tx in block.Transactions)
			{
				builder.AddTxId(tx.GetHash());
				if(!tx.IsCoinBase)
				{
					foreach(var txin in tx.Inputs)
					{
						builder.AddOutPoint(txin.PrevOut);
					}
				}

				foreach(var txout in tx.Outputs)
				{
					builder.AddScriptPubkey(txout.ScriptPubKey);
				}
			}

			return builder.Build();
		}

		public static GolombRiceFilter BuildExtendedFilter(Block block)
		{
			var builder = new GolombRiceFilterBuilder()
				.SetKey(block.GetHash());

			foreach(var tx in block.Transactions)
			{
				if(!tx.IsCoinBase)
				{
					foreach(var txin in tx.Inputs)
					{
						if(txin.ScriptSig != Script.Empty)
						{
							builder.AddScriptSig(txin.ScriptSig);
						}

						if( txin.WitScript != WitScript.Empty)
						{
							builder.AddWitness(txin.WitScript);
						}
					}
				}
			}

			return builder.Build();
		}

		public GolombRiceFilterBuilder()
		{
			_values = new HashSet<byte[]>(new ByteArrayComparer());
		}

		public GolombRiceFilterBuilder SetKey(uint256 blockHash)
		{
			_key = blockHash.ToBytes().SafeSubarray(0,16);
			return this;
		}

		public GolombRiceFilterBuilder SetP(int p)
		{
			if (p <= 0 || p > 32)
				throw new ArgumentOutOfRangeException(nameof(p), "value has to be greater than zero and less or equal to 32.");
			
			_p = (byte)p;
			return this;
		}

		public GolombRiceFilterBuilder AddTxId(uint256 id)
		{
			if (id == null)
				throw new ArgumentNullException(nameof(id));

			_values.Add(id.ToBytes());
			return this;
		}

		public GolombRiceFilterBuilder AddScriptPubkey(Script scriptPubkey)
		{
			if (scriptPubkey == null)
				throw new ArgumentNullException(nameof(scriptPubkey));

			_values.Add(scriptPubkey.ToBytes());
			return this;
		}

		public GolombRiceFilterBuilder AddScriptSig(Script scriptSig)
		{
			if (scriptSig == null)
				throw new ArgumentNullException(nameof(scriptSig));

			var data = new List<byte[]>();
			foreach(var op in scriptSig.ToOps())
			{
				if(op.PushData != null)
					data.Add(op.PushData);
				else if(op.Code == OpcodeType.OP_0)
					data.Add(new byte[0]);
			}
			AddEntries(data);
			return this;
		}

		public void AddWitness(WitScript witScript)
		{
			if (witScript == null)
				throw new ArgumentNullException(nameof(witScript));

			AddEntries(witScript.Pushes);
		}

		public GolombRiceFilterBuilder AddOutPoint(OutPoint outpoint)
		{
			if (outpoint == null)
				throw new ArgumentNullException(nameof(outpoint));

			_values.Add(outpoint.ToBytes());
			return this;
		}

		public GolombRiceFilterBuilder AddEntries(IEnumerable<byte[]> entries)
		{
			if (entries == null)
				throw new ArgumentNullException(nameof(entries));

			foreach(var entry in entries)
			{
				_values.Add(entry);
			}
			return this;
		}

		public GolombRiceFilter Build()
		{
			var n = _values.Count;
			var hs = GolombRiceFilter.ConstructHashedSet(_p, n, _key, _values);
			var filterData = Compress(hs, _p);

			return new GolombRiceFilter(filterData, n, _p);
		}

		public GolombRiceFilter BuildBasicFilter()
		{
			var n = _values.Count;
			var hs = GolombRiceFilter.ConstructHashedSet(_p, n, _key, _values);
			var filterData = Compress(hs, _p);

			return new GolombRiceFilter(filterData, n, _p);
		}

		private static byte[] Compress(List<ulong> values, byte P)
		{
			var bitStream = new BitStream();
			var sw = new GRCodedStreamWriter(bitStream, P);

			foreach (var value in values)
			{
				sw.Write(value);
			}
			return bitStream.ToByteArray();
		}		
	}
}