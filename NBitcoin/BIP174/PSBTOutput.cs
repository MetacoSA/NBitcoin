using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using NBitcoin.DataEncoders;
using NBitcoin.Crypto;
using UnKnownKVMap = System.Collections.Generic.SortedDictionary<byte[], byte[]>;
using HDKeyPathKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.PubKey, System.Tuple<NBitcoin.HDFingerprint, NBitcoin.KeyPath>>;
using PartialSigKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.KeyId, System.Tuple<NBitcoin.PubKey, NBitcoin.Crypto.ECDSASignature>>;
using System.Collections;

namespace NBitcoin.BIP174
{
	public class PSBTOutput
	{
		public TxOut TxOut { get; set; }
		public uint Index { get; set; }
		internal Transaction Transaction => Parent.tx;

		public bool IsRelatedKey(PubKey pk) =>
		HDKeyPaths.ContainsKey(pk) || // in HDKeyPathMap or
		pk.Hash.ScriptPubKey.Equals(TxOut.ScriptPubKey) || // matches as p2pkh or
		pk.WitHash.ScriptPubKey.Equals(TxOut.ScriptPubKey) || // as p2wpkh or
		pk.WitHash.ScriptPubKey.Hash.ScriptPubKey.Equals(TxOut.ScriptPubKey) || // as p2sh-p2wpkh
		(RedeemScript != null && pk.WitHash.ScriptPubKey.Equals(RedeemScript)) || // as p2sh-p2wpkh or
		(RedeemScript != null && RedeemScript.GetAllPubKeys().Any(p => p.Equals(pk))) || // more paranoia check (Probably unnecessary)
		(WitnessScript != null && WitnessScript.GetAllPubKeys().Any(p => p.Equals(pk)));

		private Script redeem_script;
		private Script witness_script;
		private HDKeyPathKVMap hd_keypaths;
		private UnKnownKVMap unknown;

		private static uint defaultKeyLen = 1;

		private class PubKeyComparer : IComparer<PubKey>
		{
			public int Compare(PubKey x, PubKey y)
			{
				return BytesComparer.Instance.Compare(x.ToBytes(), y.ToBytes());
			}
		}


		public Script RedeemScript
		{
			get
			{
				return redeem_script;
			}
			set
			{
				redeem_script = value;
			}
		}

		public Script WitnessScript
		{
			get
			{
				return witness_script;
			}
			set
			{
				witness_script = value;
			}
		}

		public HDKeyPathKVMap HDKeyPaths
		{
			get
			{
				return hd_keypaths;
			}
		}

		public UnKnownKVMap Unknown
		{
			get
			{
				return unknown;
			}
		}

		public PSBT Parent { get; set; }
		internal PSBTOutput(PSBT parent, uint index, TxOut txOut)
		{
			if (txOut == null)
				throw new ArgumentNullException(nameof(txOut));
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));
			hd_keypaths = new HDKeyPathKVMap(new PubKeyComparer());
			unknown = new UnKnownKVMap(BytesComparer.Instance);
			Parent = parent;
			TxOut = txOut;
			Index = index;
		}
		internal PSBTOutput(BitcoinStream stream, PSBT parent, uint index, TxOut txOut)
		{
			if (txOut == null)
				throw new ArgumentNullException(nameof(txOut));
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));
			hd_keypaths = new HDKeyPathKVMap(new PubKeyComparer());
			unknown = new UnKnownKVMap(BytesComparer.Instance);
			Parent = parent;
			TxOut = txOut;
			Index = index;

			byte[] k = new byte[0];
			byte[] v = new byte[0];
			try
			{
				stream.ReadWriteAsVarString(ref k);
			}
			catch (EndOfStreamException e)
			{
				throw new FormatException("Invalid PSBTOutput. Could not read key", e);
			}
			while (k.Length != 0)
			{
				try
				{
					stream.ReadWriteAsVarString(ref v);
				}
				catch (EndOfStreamException e)
				{
					throw new FormatException("Invalid PSBTOutput. Could not read value", e);
				}
				switch (k.First())
				{
					case PSBTConstants.PSBT_OUT_REDEEMSCRIPT:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTOutput. Contains illegal value in key for redeem script");
						if (redeem_script != null)
							throw new FormatException("Invalid PSBTOutput, duplicate key for redeem_script");
						redeem_script = Script.FromBytesUnsafe(v);
						break;
					case PSBTConstants.PSBT_OUT_WITNESSSCRIPT:
						if (k.Length != 1)
							throw new FormatException("Invalid PSBTOutput. Contains illegal value in key for witness script");
						if (witness_script != null)
							throw new FormatException("Invalid PSBTOutput, duplicate key for redeem_script");
						witness_script = Script.FromBytesUnsafe(v);
						break;
					case PSBTConstants.PSBT_OUT_BIP32_DERIVATION:
						var pubkey2 = new PubKey(k.Skip(1).ToArray());
						if (hd_keypaths.ContainsKey(pubkey2))
							throw new FormatException("Invalid PSBTOutput, duplicate key for hd_keypaths");
						KeyPath path = KeyPath.FromBytes(v.Skip(4).ToArray());
						hd_keypaths.Add(pubkey2, Tuple.Create(new HDFingerprint(v.Take(4).ToArray()), path));
						break;
					default:
						if (unknown.ContainsKey(k))
							throw new FormatException("Invalid PSBTInput, duplicate key for unknown value");
						unknown.Add(k, v);
						break;
				}
				stream.ReadWriteAsVarString(ref k);
			}
		}
		internal void Combine(PSBTOutput other)
		{
			if (redeem_script == null && other.redeem_script != null)
				redeem_script = other.redeem_script;

			if (witness_script == null && other.witness_script != null)
				witness_script = other.witness_script;

			foreach (var keyPath in other.hd_keypaths)
				hd_keypaths.TryAdd(keyPath.Key, keyPath.Value);

			foreach (var uk in other.Unknown)
				unknown.TryAdd(uk.Key, uk.Value);
		}

		public void AddKeyPath(ExtPubKey extPubKey, KeyPath keyPath)
		{
			if (extPubKey == null)
				throw new ArgumentNullException(nameof(extPubKey));
			AddKeyPath(extPubKey.Fingerprint, extPubKey.PubKey, keyPath);
		}

		public void AddKeyPath(HDFingerprint fingerprint, PubKey key, KeyPath keyPath)
		{
			if (keyPath == null)
				throw new ArgumentNullException(nameof(keyPath));
			if (key == null)
				throw new ArgumentNullException(nameof(key));
			hd_keypaths.Add(key, new Tuple<HDFingerprint, KeyPath>(fingerprint, keyPath));
		}

		#region IBitcoinSerializable Members

		public void Serialize(BitcoinStream stream)
		{
			if (redeem_script != null)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				stream.ReadWrite(PSBTConstants.PSBT_OUT_REDEEMSCRIPT);
				var value = redeem_script.ToBytes();
				stream.ReadWriteAsVarString(ref value);
			}

			if (witness_script != null)
			{
				stream.ReadWriteAsVarInt(ref defaultKeyLen);
				stream.ReadWrite(PSBTConstants.PSBT_OUT_WITNESSSCRIPT);
				var value = witness_script.ToBytes();
				stream.ReadWriteAsVarString(ref value);
			}

			foreach (var pathPair in hd_keypaths)
			{
				var key = new byte[] { PSBTConstants.PSBT_OUT_BIP32_DERIVATION }.Concat(pathPair.Key.ToBytes());
				stream.ReadWriteAsVarString(ref key);
				var path = pathPair.Value.Item2.ToBytes();
				var pathInfo = pathPair.Value.Item1.ToBytes().Concat(path);
				stream.ReadWriteAsVarString(ref pathInfo);
			}

			foreach (var entry in unknown)
			{
				var k = entry.Key;
				var v = entry.Value;
				stream.ReadWriteAsVarString(ref k);
				stream.ReadWriteAsVarString(ref v);
			}

			var sep = PSBTConstants.PSBT_SEPARATOR;
			stream.ReadWrite(ref sep);
		}

		#endregion

		public override bool Equals(object obj)
		{
			var item = obj as PSBTOutput;
			if (item == null)
				return false;
			return item.Equals(this);
		}
		public bool Equals(PSBTOutput b) =>
			b != null && this.ToBytes().SequenceEqual(b.ToBytes());

		public override int GetHashCode() => Utils.GetHashCode(this.ToBytes());

		public byte[] ToBytes()
		{
			MemoryStream ms = new MemoryStream();
			var bs = new BitcoinStream(ms, true);
			bs.ConsensusFactory = Parent.tx.GetConsensusFactory();
			this.Serialize(bs);
			return ms.ToArrayEfficient();
		}

		internal void SetCoin(ICoin coin)
		{
			if (coin is ScriptCoin scriptCoin)
			{
				if (scriptCoin.RedeemType == RedeemType.P2SH)
				{
					redeem_script = scriptCoin.Redeem;
				}
				else
				{
					witness_script = scriptCoin.Redeem;
					if (scriptCoin.IsP2SH)
						redeem_script = witness_script.WitHash.ScriptPubKey;
				}
			}
		}

		internal void Write(JsonTextWriter jsonWriter)
		{
			jsonWriter.WriteStartObject();
			if (this.redeem_script != null)
			{
				jsonWriter.WritePropertyValue("redeem_script", redeem_script.ToString());
			}
			if (this.witness_script != null)
			{
				jsonWriter.WritePropertyValue("witness_script", witness_script.ToString());
			}
			jsonWriter.WriteBIP32Derivations(this.hd_keypaths);
			jsonWriter.WriteEndObject();
		}

		public override string ToString()
		{
			var strWriter = new StringWriter();
			var jsonWriter = new JsonTextWriter(strWriter);
			jsonWriter.Formatting = Formatting.Indented;
			Write(jsonWriter);
			jsonWriter.Flush();
			return strWriter.ToString();
		}
	}
	

	public class PSBTOutputList : IReadOnlyCollection<PSBTOutput>
	{
		List<PSBTOutput> _Inner = new List<PSBTOutput>();
		public int Count => _Inner.Count;

		internal void Add(PSBTOutput item)
		{
			_Inner.Add(item);
		}
		public IEnumerator<PSBTOutput> GetEnumerator()
		{
			return _Inner.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _Inner.GetEnumerator();
		}
		public PSBTOutput this[int index] => _Inner[index];
	}
}
