#nullable enable
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using NBitcoin.DataEncoders;
using Map = System.Collections.Generic.SortedDictionary<byte[], byte[]>;
using NBitcoin.BIP370;

namespace NBitcoin
{
	public abstract class PSBTOutput : PSBTCoin
	{
		
		public abstract Script ScriptPubKey { get; set; }
		public abstract Money Value { get; set; }
		public uint Index { get; set; }

		protected static uint defaultKeyLen = 1;

		internal PSBTOutput(PSBT parent, uint index) : base(parent)
		{
			
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));
			
			Index = index;
		}
		internal PSBTOutput(Map map, PSBT parent, uint index) : base(parent)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));
			Index = index;

			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_OUT_REDEEMSCRIPT, out var b))
				redeem_script = Script.FromBytesUnsafe(b);
			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_OUT_WITNESSSCRIPT, out b))
				witness_script = Script.FromBytesUnsafe(b);
			if (map.TryRemove<byte[]>(PSBTConstants.PSBT_OUT_TAP_INTERNAL_KEY, out b))
			{
				if (!TaprootInternalPubKey.TryCreate(b, out var tpk))
					throw new FormatException("Invalid PSBTOutput. Contains invalid internal taproot pubkey");
				TaprootInternalKey = tpk;
			}
			foreach (var kv in map.RemoveAll<byte[]>(PSBTConstants.PSBT_OUT_BIP32_DERIVATION))
			{
				var pubkey2 = new PubKey(kv.Key.Skip(1).ToArray());
				if (hd_keypaths.ContainsKey(pubkey2))
					throw new FormatException("Invalid PSBTOutput, duplicate key for hd_keypaths");
				KeyPath path = KeyPath.FromBytes(kv.Value.Skip(4).ToArray());
				hd_keypaths.Add(pubkey2, new RootedKeyPath(new HDFingerprint(kv.Value.Take(4).ToArray()), path));
			}
			foreach (var kv in map.RemoveAll<byte[]>(PSBTConstants.PSBT_OUT_TAP_BIP32_DERIVATION))
			{
				var pubkey3 = new TaprootPubKey(kv.Key.Skip(1).ToArray());
				if (hd_taprootkeypaths.ContainsKey(pubkey3))
					throw new FormatException("Invalid PSBTOutput, duplicate key for hd_taproot_keypaths");
				var bs = new BitcoinStream(kv.Value);
				List<uint256> hashes = null!;
				bs.ReadWrite(ref hashes);
				var pos = (int)bs.Inner.Position;
				KeyPath path2 = KeyPath.FromBytes(kv.Value.Skip(pos + 4).ToArray());
				hd_taprootkeypaths.Add(pubkey3,
					new TaprootKeyPath(
						new RootedKeyPath(new HDFingerprint(kv.Value.Skip(pos).Take(4).ToArray()), path2),
						hashes.ToArray()));
			}
			unknown = map;
		}

		/// <summary>
		/// Import informations contained by <paramref name="other"/> into this instance.
		/// </summary>
		/// <param name="other"></param>
		public void UpdateFrom(PSBTOutput other)
		{
			if (other == null)
				throw new ArgumentNullException(nameof(other));
			if (redeem_script == null && other.redeem_script != null)
				redeem_script = other.redeem_script;

			if (witness_script == null && other.witness_script != null)
				witness_script = other.witness_script;

			foreach (var keyPath in other.hd_keypaths)
				hd_keypaths.TryAdd(keyPath.Key, keyPath.Value);

			foreach (var uk in other.Unknown)
				unknown.TryAdd(uk.Key, uk.Value);
		}

		#region IBitcoinSerializable Members

		internal virtual void FillMap(Map map)
		{
			if (redeem_script != null)
				map.Add([PSBTConstants.PSBT_OUT_REDEEMSCRIPT], redeem_script.ToBytes());

			if (witness_script != null)
				map.Add([PSBTConstants.PSBT_OUT_WITNESSSCRIPT], witness_script.ToBytes());

			if (this.TaprootInternalKey is TaprootInternalPubKey tp)
				map.Add([PSBTConstants.PSBT_OUT_TAP_INTERNAL_KEY], tp.ToBytes());

			foreach (var pathPair in hd_keypaths)
			{
				var key = new byte[] { PSBTConstants.PSBT_OUT_BIP32_DERIVATION }.Concat(pathPair.Key.ToBytes());
				var path = pathPair.Value.KeyPath.ToBytes();
				var pathInfo = pathPair.Value.MasterFingerprint.ToBytes().Concat(path);
				map.Add(key, pathInfo);
			}
			foreach (var pathPair in hd_taprootkeypaths)
			{
				var key = new byte[] { PSBTConstants.PSBT_OUT_TAP_BIP32_DERIVATION }.Concat(pathPair.Key.ToBytes());
				uint leafCount = (uint)pathPair.Value.LeafHashes.Length;
				BitcoinStream bs = new BitcoinStream(new MemoryStream(), true);
				bs.ReadWriteAsVarInt(ref leafCount);
				foreach (var hash in pathPair.Value.LeafHashes)
				{
					bs.ReadWrite(hash);
				}
				var b = pathPair.Value.RootedKeyPath.MasterFingerprint.ToBytes();
				bs.ReadWrite(b);
				b = pathPair.Value.RootedKeyPath.KeyPath.ToBytes();
				bs.ReadWrite(b);
				b = ((MemoryStream)bs.Inner).ToArrayEfficient();
				map.Add(key, b);
			}
			unknown = map;
		}

		#endregion

		public override bool Equals(object? obj)
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
			var m = new Map();
			this.FillMap(m);
			return m.ToBytes();
		}

		public void UpdateFromCoin(ICoin coin)
		{
			if (coin == null)
				throw new ArgumentNullException(nameof(coin));
			if (coin.TxOut.ScriptPubKey != ScriptPubKey)
				throw new ArgumentException("This coin does not match the scriptPubKey of this output");
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

			if (unknown.Count != 0)
			{
				jsonWriter.WritePropertyName("unknown");
				jsonWriter.WriteStartObject();
				foreach (var el in unknown)
				{
					jsonWriter.WritePropertyValue(Encoders.Hex.EncodeData(el.Key), Encoders.Hex.EncodeData(el.Value));
				}
				jsonWriter.WriteEndObject();
			}
			if (this.TaprootInternalKey is TaprootInternalPubKey tpk)
			{
				jsonWriter.WritePropertyValue("taproot_internal_key", tpk.ToString());
			}
			if (this.redeem_script != null)
			{
				jsonWriter.WritePropertyValue("redeem_script", redeem_script.ToString());
			}
			if (this.witness_script != null)
			{
				jsonWriter.WritePropertyValue("witness_script", witness_script.ToString());
			}
			jsonWriter.WriteBIP32Derivations(this.hd_keypaths);
			jsonWriter.WriteBIP32Derivations(this.hd_taprootkeypaths);
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

		protected override PSBTHDKeyMatch CreateHDKeyMatch(IHDKey accountKey, KeyPath addressKeyPath, KeyValuePair<IPubKey, RootedKeyPath> kv)
		{
			return new PSBTHDKeyMatch<PSBTOutput>(this, accountKey, addressKeyPath, kv);
		}
	}


	public class PSBTOutputList : PSBTCoinList<PSBTOutput>
	{
		internal void Add(PSBTOutput item)
		{
			_Inner.Add(item);
		}
	}
}
#nullable disable
