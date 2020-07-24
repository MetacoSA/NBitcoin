using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NBitcoin.Crypto;
using NBitcoin.Protocol;

namespace NBitcoin
{
	/// <summary>
	/// Implements a Golomb-coded set to be use in the creation of client-side filter
	/// for a new kind Bitcoin light clients. This code is based on the BIP:
	/// https://github.com/bitcoin/bips/blob/master/bip-0158.mediawiki
	/// </summary>
	public class GolombRiceFilter
	{
		// This is the value used by default as P as defined in the BIP.
		internal const byte DefaultP = 19;
		internal const uint DefaultM = 784_931;

		/// <summary>
		/// a value which is computed as 1/fp where fp is the desired false positive rate.
		/// </summary>
		public byte P { get; }

		/// <summary>
		/// a value which is computed as N * fp (or false positive rate = 1/M).
		/// this value allows filter to uniquely tune the range that items are hashed onto
		/// before compressing
		/// </summary>
		public uint M { get; }

		/// <summary>
		/// Number of elements in the filter
		/// </summary>
		public int N { get; }

		/// <summary>
		/// Raw filter data
		/// </summary>
		public byte[] Data { get; }

		private ulong ModulusP { get; }
		private ulong ModulusNP { get; }

		public static GolombRiceFilter Empty { get; } = new GolombRiceFilter(new byte[] { 0 });

		/// <summary>
		/// Creates a new Golomb-Rice filter from the data byte array which
		/// contains a serialized filter. Uses the DefaultP value (20).
		/// </summary>
		/// <param name="data">A serialized Golomb-Rice filter.</param>
		public static GolombRiceFilter Parse(string str)
		{
			var bytes = NBitcoin.DataEncoders.Encoders.Hex.DecodeData(str);
			return new GolombRiceFilter(bytes);
		}


		/// <summary>
		/// Creates a new Golomb-Rice filter from the data byte array which
		/// contains a serialized filter. Uses the DefaultP value (20).
		/// </summary>
		/// <param name="data">A serialized Golomb-Rice filter.</param>
		public GolombRiceFilter(byte[] data)
			: this(data, DefaultP, DefaultM)
		{
		}

		/// <summary>
		/// Creates a new Golomb-Rice filter from the data byte array which
		/// contains a serialized filter.
		/// </summary>
		/// <param name="data">A serialized Golomb-Rice filter.</param>
		/// <param name="p">The P value to use.</param>
		/// <param name="m">The M value to use.</param>
		public GolombRiceFilter(byte[] data, byte p, uint m)
		{
			P = p;
			M = m;
			var n = new VarInt();
			var stream = new BitcoinStream(data);
			stream.ReadWrite(ref n);
			N = (int)n.ToLong();
			var l = n.ToBytes().Length;
			Data = data.SafeSubarray(l);
		}

		/// <summary>
		/// Creates a new Golomb-Rice filter from the data byte array.
		/// </summary>
		/// <param name="data">A serialized Golomb-Rice filter.</param>
		/// <param name="n">The number of elements in the filter.</param>
		/// <param name="p">The P value to use.</param>
		/// <param name="m">The M value to use.</param>
		internal GolombRiceFilter(byte[] data, int n, byte p, uint m)
		{
			this.P = p;
			this.N = n;
			this.M = m;

			this.ModulusP = 1UL << P;
			this.ModulusNP = (ulong)N * M;
			this.Data = data;
		}

		/// <summary>
		/// Computes the sorted-and-uncompressed list of values to be included in the filter.
		/// /// </summary>
		/// <param name="P">P value used.</param>
		/// <param name="key">Key used for hashing the datalements.</param>
		/// <param name="data">Data elements to be computed in the list.</param>
		/// <returns></returns>
		internal static ulong[] ConstructHashedSet(byte P, int n, uint m, byte[] key, IEnumerable<byte[]> data, int dataCount)
		{
			// N the number of items to be inserted into the set.
			// The list of data item hashes.
			var values = new ulong[dataCount];
			var valuesIndex = 0;
			var modP = 1UL << P;
			var modNP = ((ulong)n) * m;
			var nphi = modNP >> 32;
			var nplo = (ulong)((uint)modNP);

			var k0 = BitConverter.ToUInt64(key, 0);
			var k1 = BitConverter.ToUInt64(key, 8);

			// Process the data items and calculate the 64 bits hash for each of them.
			foreach (var item in data)
			{
				var hash = SipHash(k0, k1, item);
				var value = FastReduction(hash, nphi, nplo);
				values[valuesIndex++] = value;
			}

			Array.Sort(values);
			return values;
		}

		/// <summary>
		/// Calculates the filter's header.
		/// </summary>
		/// <param name="previousHeader">Previous filter header.</param>
		/// <returns>The filter header.</returns>
		public uint256 GetHeader(uint256 previousHeader)
		{
			var curFilterHashBytes = Hashes.DoubleSHA256(ToBytes()).ToBytes();
			var prvFilterHashBytes = previousHeader.ToBytes();
			return Hashes.DoubleSHA256(curFilterHashBytes.Concat(prvFilterHashBytes));
		}

		/// <summary>
		/// Checks if the value passed is in the filter.
		/// </summary>
		/// <param name="data">Data element to check in the filter.</param>
		/// <param name="key">Key used for hashing the data elements.</param>
		/// <returns>true if the element is in the filter, otherwise false.</returns>
		public bool Match(byte[] data, byte[] key)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			return MatchAny(new[] { data }, 1, key);
		}

		/// <summary>
		/// Checks if any of the provided elements is in the filter.
		/// </summary>
		/// <param name="data">Data elements to check in the filter.</param>
		/// <param name="key">Key used for hashing the data elements.</param>
		/// <returns>true if at least one of the elements is in the filter, otherwise false.</returns>
		public bool MatchAny(byte[][] data, byte[] key)
		{
			return MatchAny(data, data.Length, key);
		}

		/// <summary>
		/// Checks if any of the provided elements is in the filter.
		/// </summary>
		/// <param name="data">Data elements to check in the filter.</param>
		/// <param name="key">Key used for hashing the data elements.</param>
		/// <returns>true if at least one of the elements is in the filter, otherwise false.</returns>
		public bool MatchAny(IEnumerable<byte[]> data, byte[] key)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (data is byte[][] dataArray)
			{
				return MatchAny(dataArray, dataArray.Length, key);
			}
			else if (data is ICollection<byte[]> dataCollection)
			{
				return MatchAny(dataCollection, dataCollection.Count, key);
			}
			else
			{
				return MatchAny(data, data.Count(), key);
			}
		}

		/// <summary>
		/// Checks if any of the provided elements is in the filter.
		/// </summary>
		/// <param name="data">Data elements to check in the filter.</param>
		/// <param name="key">Key used for hashing the data elements.</param>
		/// <returns>true if at least one of the elements is in the filter, otherwise false.</returns>
		internal bool MatchAny(IEnumerable<byte[]> data, int dataCount, byte[] key)
		{
			if (data == null || dataCount == 0)
				throw new ArgumentException("data can not be null or empty array.", nameof(data));
			if (key == null)
				throw new ArgumentNullException(nameof(key));

			var hs = ConstructHashedSet(P, N, M, key, data, dataCount);

			var bitStream = new BitStream(Data);
			var sr = new GRCodedStreamReader(bitStream, P, 0);

			while(sr.TryRead(out var val))
			{
				var dataIndex = 0;
				while(true)
				{
					if (dataIndex == dataCount)
						return false;

					if (hs[dataIndex] == val)
						return true;

					if (hs[dataIndex] > val)
						break;

					dataIndex++;
				}
			}

			return false;
		}

		/// <summary>
		/// Serialize the filter as a array of bytes using [varint(N) | data].
		/// </summary>
		/// <returns>A array of bytes with the serialized filter data.</returns>
		public byte[] ToBytes()
		{
			var n = new VarInt((ulong)N).ToBytes();
			return n.Concat(this.Data);
		}

		/// <summary>
		/// Serialize the filter as hexadecimal string.
		/// </summary>
		/// <returns>A string with the serialized filter data</returns>
		public override string ToString()
		{
			return NBitcoin.DataEncoders.Encoders.Hex.EncodeData(ToBytes());
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

		private static ulong SipHash(ulong k0, ulong k1, byte[] data)
		{
			var hasher = new Hashes.SipHasher(k0, k1);
			hasher.Write(data);
			return hasher.Finalize();
		}
	}

	/// <summary>
	/// Class for creating Golomb-Rice filters for a given block.
	/// It provides methods for building two kind of filters out-of-the-box:
	/// Basic Filters and Extenden Filters.
	/// </summary>
	public class GolombRiceFilterBuilder
	{
		private byte _p = GolombRiceFilter.DefaultP;
		private uint _m = GolombRiceFilter.DefaultM;
		private byte[] _key;
		private HashSet<byte[]> _values;

		/// <summary>
		/// Helper class for making sure not two identical data elements are
		/// included in a filter.
		/// </summary>
		class ByteArrayComparer : IEqualityComparer<byte[]>
		{
			public static readonly ByteArrayComparer Instance = new ByteArrayComparer();
			private ByteArrayComparer()
			{

			}
			public bool Equals(byte[] a, byte[] b)
			{
				if (a.Length != b.Length)
					return false;
				for (int i = 0; i < a.Length; i++)
					if (a[i] != b[i])
						return false;
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

		/// <summary>
		/// Builds the basic filter for a given block.
		///
		/// The basic filter is designed to contain everything that a light client needs to sync a regular Bitcoin wallet.
		/// A basic filter MUST contain exactly the following items for each transaction in a block:
		///  * The outpoint of each input, except for the coinbase transaction
		///  * The scriptPubKey of each output
		///  * The txid of the transaction itself
		/// </summary>
		/// <param name="block">The block used for building the filter.</param>
		/// <returns>The basic filter for the block.</returns>
		public static GolombRiceFilter BuildBasicFilter(Block block)
		{
			var builder = new GolombRiceFilterBuilder()
				.SetKey(block.GetHash());

			foreach (var tx in block.Transactions)
			{
				if (!tx.IsCoinBase) // except for the coinbase transaction
				{
					foreach (var txin in tx.Inputs)
					{
						// The outpoint of each input
						builder.AddOutPoint(txin.PrevOut);
					}
				}

				foreach (var txout in tx.Outputs)
				{
					// The scriptPubKey of each output
					builder.AddScriptPubkey(txout.ScriptPubKey);
				}
			}

			return builder.Build();
		}


		/// <summary>
		/// Creates a new Golob-Rice filter builder.
		/// </summary>
		public GolombRiceFilterBuilder()
		{
			_values = new HashSet<byte[]>(ByteArrayComparer.Instance);
		}

		/// <summary>
		/// Sets the key used for hashing the filter data elements.
		/// The first half of the block hash is used as described in the BIP.
		/// </summary>
		/// <param name="blockHash">The block hash which the hashing key is derived from.</param>
		/// <returns>The updated filter builder instance</returns>
		public GolombRiceFilterBuilder SetKey(uint256 blockHash)
		{
			if (blockHash == null)
				throw new ArgumentNullException(nameof(blockHash));

			_key = blockHash.ToBytes().SafeSubarray(0, 16);
			return this;
		}

		/// <summary>
		/// Sets the P value to use.
		/// </summary>
		/// <param name="p">P value</param>
		/// <returns>The updated filter builder instance.</returns>
		public GolombRiceFilterBuilder SetP(int p)
		{
			if (p <= 0 || p > 32)
				throw new ArgumentOutOfRangeException(nameof(p), "value has to be greater than zero and less or equal to 32.");

			_p = (byte)p;
			return this;
		}

		/// <summary>
		/// Sets the M value to use.
		/// </summary>
		/// <param name="m">M value</param>
		/// <returns>The updated filter builder instance.</returns>
		public GolombRiceFilterBuilder SetM(uint m)
		{
			_m = m;
			return this;
		}


		/// <summary>
		/// Adds a transacion id to the list of elements that will be used for building the filter.
		/// </summary>
		/// <param name="id">The transaction id.</param>
		/// <returns>The updated filter builder instance.</returns>
		public GolombRiceFilterBuilder AddTxId(uint256 id)
		{
			if (id == null)
				throw new ArgumentNullException(nameof(id));

			_values.Add(id.ToBytes());
			return this;
		}

		/// <summary>
		/// Adds a scriptPubKey to the list of elements that will be used for building the filter.
		/// </summary>
		/// <param name="scriptPubkey">The scriptPubkey.</param>
		/// <returns>The updated filter builder instance.</returns>
		public GolombRiceFilterBuilder AddScriptPubkey(Script scriptPubkey)
		{
			if (scriptPubkey == null)
				throw new ArgumentNullException(nameof(scriptPubkey));

			// Unsafe is OK because Script is readonly and we do not modify the arrays inside values
			_values.Add(scriptPubkey.ToBytes(true));
			return this;
		}

		/// <summary>
		/// Adds a scriptSig to the list of elements that will be used for building the filter.
		/// </summary>
		/// <param name="scriptSig">The scriptSig.</param>
		/// <returns>The updated filter builder instance.</returns>
		public GolombRiceFilterBuilder AddScriptSig(Script scriptSig)
		{
			if (scriptSig == null)
				throw new ArgumentNullException(nameof(scriptSig));

			var data = new List<byte[]>();
			foreach (var op in scriptSig.ToOps())
			{
				if (op.PushData != null)
					data.Add(op.PushData);
				else if (op.Code == OpcodeType.OP_0)
					data.Add(EmptyBytes);
			}
			AddEntries(data);
			return this;
		}
		static readonly byte[] EmptyBytes = new byte[0];
		/// <summary>
		/// Adds a witness stack to the list of elements that will be used for building the filter.
		/// </summary>
		/// <param name="witScript">The witScript.</param>
		/// <returns>The updated filter builder instance.</returns>
		public void AddWitness(WitScript witScript)
		{
			if (witScript == null)
				throw new ArgumentNullException(nameof(witScript));

			AddEntries(witScript.Pushes);
		}

		/// <summary>
		/// Adds an outpoint to the list of elements that will be used for building the filter.
		/// </summary>
		/// <param name="outpoint">The outpoint.</param>
		/// <returns>The updated filter builder instance.</returns>
		public GolombRiceFilterBuilder AddOutPoint(OutPoint outpoint)
		{
			if (outpoint == null)
				throw new ArgumentNullException(nameof(outpoint));

			MemoryStream ms = new MemoryStream(32 + 4);
			outpoint.ReadWrite(new BitcoinStream(ms, true));
			_values.Add(ms.ToArrayEfficient());
			return this;
		}

		/// <summary>
		/// Adds a list of elements to the list of elements that will be used for building the filter.
		/// </summary>
		/// <param name="entries">The entries.</param>
		/// <returns>The updated filter builder instance.</returns>
		public GolombRiceFilterBuilder AddEntries(IEnumerable<byte[]> entries)
		{
			if (entries == null)
				throw new ArgumentNullException(nameof(entries));

			foreach (var entry in entries)
			{
				_values.Add(entry);
			}
			return this;
		}

		/// <summary>
		/// Builds the Golomb-Rice filters from the parameters and data elements included.
		/// </summary>
		/// <returns>The built filter.</returns>
		public GolombRiceFilter Build()
		{
			var n = _values.Count;
			var hs = GolombRiceFilter.ConstructHashedSet(_p, n, _m, _key, _values, _values.Count);
			var filterData = Compress(hs, _p);

			return new GolombRiceFilter(filterData, n, _p, _m);
		}

		private static byte[] Compress(ulong[] values, byte P)
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
