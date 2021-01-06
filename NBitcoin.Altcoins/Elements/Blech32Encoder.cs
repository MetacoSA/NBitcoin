using System;
using System.Collections.Generic;
using System.Linq;
using NBitcoin.DataEncoders;

namespace NBitcoin.Altcoins.Elements
{
	public class Blech32Encoder : Bech32Encoder
	{
		protected static readonly ulong[] Generator = { 0x7d52fba40bd886, 0x5e8dbf1a03950c, 0x1c3a3c74072a18, 0x385d72fa0e5139, 0x7093e5a608865b };

#if HAS_SPAN
		private static ulong Polymod(ReadOnlySpan<byte> values)
#else
		private static ulong Polymod(byte[] values)
#endif
		{
			ulong chk = 1;
			for (int ii = 0; ii < values.Length; ii++)
			{
				var value = values[ii];
				var top = chk >> 55;
				chk = value ^ ((chk & 0x7fffffffffffff) <<
				               5);
				chk ^= ((top >> 0) & 1) == 1 ? Generator[0] : 0;
				chk ^= ((top >> 1) & 1) == 1 ? Generator[1] : 0;
				chk ^= ((top >> 2) & 1) == 1 ? Generator[2] : 0;
				chk ^= ((top >> 3) & 1) == 1 ? Generator[3] : 0;
				chk ^= ((top >> 4) & 1) == 1 ? Generator[4] : 0;
			}
			return chk;
		}

#if HAS_SPAN
		protected override bool VerifyChecksum(byte[] data, int bechStringLen, out Bech32EncodingType encodingType, out int[] errorPosition)
		{
			return VerifyChecksum(data.AsSpan(), bechStringLen, out encodingType, out errorPosition);
		}
		protected override bool VerifyChecksum(ReadOnlySpan<byte> data, int bechStringLen, out Bech32EncodingType encodingType, out int[] errorPosition)
#else
		protected override bool VerifyChecksum(byte[] data, int bechStringLen, out Bech32EncodingType encodingType, out int[] errorPosition)
#endif
		{
#if HAS_SPAN
			Span<byte> values = _HrpExpand.Length + data.Length is int v && v > 256 ? new byte[v] : stackalloc byte[v];
			_HrpExpand.CopyTo(values);
			data.CopyTo(values.Slice(_HrpExpand.Length));
#else
			var values = _HrpExpand.Concat(data);
#endif
			errorPosition = new int[0];

			encodingType = Bech32EncodingType.BECH32;
			return Polymod(values) == 1;
		}
#if HAS_SPAN
		private byte[] CreateChecksum(ReadOnlySpan<byte> data)
#else
		private byte[] CreateChecksum(byte[] data, int offset, int count)
#endif

		{
#if HAS_SPAN
			Span<byte> values = _HrpExpand.Length + data.Length + 12 is int v && v > 256 ? new byte[v] : stackalloc byte[v];
#else
			var values = new byte[_HrpExpand.Length + count + 12];
#endif

			var valuesOffset = 0;
#if HAS_SPAN
			_HrpExpand.CopyTo(values.Slice(valuesOffset));
#else
			Array.Copy(_HrpExpand, 0, values, valuesOffset, _HrpExpand.Length);
#endif
			valuesOffset += _HrpExpand.Length;
#if HAS_SPAN
			data.CopyTo(values.Slice(valuesOffset));
#else
			Array.Copy(data, offset, values, valuesOffset, count);
#endif
			var polymod = Polymod(values) ^ 1;
			var ret = new byte[12];
			for (int i = 0; i < 12; i++)
			{
				ret[i] = (byte)((polymod >> 5 * (11 - i)) & 31);
			}
			return ret;
		}


#if HAS_SPAN
		public override string EncodeData(ReadOnlySpan<byte> data, Bech32EncodingType encodingType)
#else
		public override string EncodeData(byte[] data, int offset, int count, Bech32EncodingType encodingType)
#endif

		{
#if HAS_SPAN
			Span<byte> combined = _Hrp.Length + 1 + data.Length + 12 is int v && v > 256 ? new byte[v] : stackalloc byte[v];
			_Hrp.AsSpan().CopyTo(combined);
#else
			var combined = new byte[_Hrp.Length + 1 + count + 12];
			Array.Copy(_Hrp, 0, combined, 0, _Hrp.Length);
#endif
			int combinedOffset = 0;
			combinedOffset += _Hrp.Length;
			combined[combinedOffset] = 49;
			combinedOffset++;
#if HAS_SPAN
			data.CopyTo(combined.Slice(combinedOffset));
			combinedOffset += data.Length;
#else
			Array.Copy(data, offset, combined, combinedOffset, count);
			combinedOffset += count;
#endif

#if HAS_SPAN
			var checkSum = CreateChecksum(data);
			checkSum.AsSpan().Slice(0, 12).CopyTo(combined.Slice(combinedOffset));
			for (int i = 0; i < data.Length + 12; i++)
#else
			var checkSum = CreateChecksum(data, offset, count);
			Array.Copy(checkSum, 0, combined, combinedOffset, 12);
			for (int i = 0; i < count + 12; i++)
#endif
			{
				combined[_Hrp.Length + 1 + i] = Byteset[combined[_Hrp.Length + 1 + i]];
			}
			return DataEncoders.Encoders.ASCII.EncodeData(combined);
		}

		public new static Blech32Encoder ExtractEncoderFromString(string test)
		{
			var i = test.IndexOf('1');
			if (i == -1)
				throw new FormatException("Invalid Blech32 string");
			return ElementsEncoders.Blech32(test.Substring(0, i));
		}

		protected override void CheckCase(string hrp)
		{
			if (hrp.ToLowerInvariant().Equals(hrp))
				return;
			if (hrp.ToUpperInvariant().Equals(hrp))
				return;
			throw new FormatException("Invalid blech32 string, mixed case detected");
		}

		protected override byte[] DecodeDataCore(string encoded, out Bech32EncodingType encodingType)
		{
			if (encoded == null)
				throw new ArgumentNullException(nameof(encoded));
			CheckCase(encoded);
			var buffer = DataEncoders.Encoders.ASCII.DecodeData(encoded);
			if (buffer.Any(b => b < 33 || b > 126))
			{
				throw new FormatException("bech chars are out of range");
			}
			encoded = encoded.ToLowerInvariant();
			buffer = DataEncoders.Encoders.ASCII.DecodeData(encoded);
			var pos = encoded.LastIndexOf("1", StringComparison.OrdinalIgnoreCase);
			if (encoded.Length > 1000 || pos == -1 || pos == 0 || pos + 13 > encoded.Length)
			{ // ELEMENTS: 90->1000, 7->13

				throw new FormatException("blech missing separator, separator misplaced or too long input");
			}
			if (buffer.Skip(pos + 1).Any(x => !Byteset.Contains(x)))
			{
				throw new FormatException("bech chars are out of range");
			}

			buffer = DataEncoders.Encoders.ASCII.DecodeData(encoded);
			var hrp = DataEncoders.Encoders.ASCII.DecodeData(encoded.Substring(0, pos));
			if (!hrp.SequenceEqual(_Hrp))
			{
				throw new FormatException("Mismatching human readable part");
			}
#if HAS_SPAN
			Span<byte> data = encoded.Length - pos - 1 is int l && l > 256 ? new byte[l] : stackalloc byte[l];
#else
			var data = new byte[encoded.Length - pos - 1];
#endif
			for (int j = 0, i = pos + 1; i < encoded.Length; i++, j++)
			{
				data[j] = (byte)Array.IndexOf(Byteset, buffer[i]);
			}

			if (!VerifyChecksum(data, encoded.Length, out encodingType, out var _))
			{
				throw new FormatException("Error while verifying Blech32 checksum");
			}
#if HAS_SPAN
			return data.Slice(0, data.Length - 12).ToArray();
#else
			return data.Take(data.Length - 12).ToArray();
#endif
		}

#if HAS_SPAN
		protected override byte[] ConvertBits(ReadOnlySpan<byte> data, int fromBits, int toBits, bool pad = true)
#else
		protected override byte[] ConvertBits(IEnumerable<byte> data, int fromBits, int toBits, bool pad = true)
#endif
		{
			var acc = 0;
			var bits = 0;
			var maxv = (1 << toBits) - 1;
			var ret = new List<byte>();
			foreach (var value in data)
			{
				if ((value >> fromBits) > 0)
					throw new FormatException("Invalid Blech32 string");
				acc = (acc << fromBits) | value;
				bits += fromBits;
				while (bits >= toBits)
				{
					bits -= toBits;
					ret.Add((byte)((acc >> bits) & maxv));
				}
			}
			if (pad)
			{
				if (bits > 0)
				{
					ret.Add((byte)((acc << (toBits - bits)) & maxv));
				}
			}
			else if (bits >= fromBits || (byte)(((acc << (toBits - bits)) & maxv)) != 0)
			{
				throw new FormatException("Invalid Blech32 string");
			}
			return ret.ToArray();
		}

		public override byte[] Decode(string addr, out byte witnessVerion)
		{
			if (addr == null)
				throw new ArgumentNullException(nameof(addr));
			CheckCase(addr);
			var data = DecodeDataCore(addr, out var encodingType);
			witnessVerion = data[0];
#if HAS_SPAN
			var decoded = ConvertBits(data.AsSpan().Slice(1), 5, 8, false);
#else
			var decoded = ConvertBits(data.Skip(1), 5, 8, false);
#endif
			if (decoded.Length  < 34 || encodingType != Bech32EncodingType.BECH32)
				throw new FormatException("Invalid decoded data length");
			return decoded;
		}

		internal Blech32Encoder(string hrp) : base(hrp)
		{
		}

		public Blech32Encoder(byte[] hrp) : base(hrp)
		{
		}
	}
}
