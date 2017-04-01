using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NBitcoin.DataEncoders
{
	public class Bech32Encoder
	{
		private static readonly byte[] Byteset = Encoders.ASCII.DecodeData("qpzry9x8gf2tvdw0s3jn54khce6mua7l");
		private static readonly uint[] Generator = { 0x3b6a57b2U, 0x26508e6dU, 0x1ea119faU, 0x3d4233ddU, 0x2a1462b3U };


		internal Bech32Encoder(string hrp): this(hrp == null ? null : Encoders.ASCII.DecodeData(hrp))
		{

		}
		public Bech32Encoder(byte[] hrp)
		{
			if(hrp == null)
				throw new ArgumentNullException("hrp");
			_Hrp = hrp;
			var len = hrp.Length;
			_HrpExpand = new byte[(2 * len) + 1];
			for(int i = 0; i < len; i++)
			{
				_HrpExpand[i] = (byte)(hrp[i] >> 5);
				_HrpExpand[i + len + 1] = (byte)(hrp[i] & 31);
			}
		}

		private readonly byte[] _HrpExpand;
		private readonly byte[] _Hrp;
		public byte[] HumanReadablePart
		{
			get
			{
				return _Hrp;
			}
		}
		private static uint Polymod(byte[] values)
		{
			uint chk = 1;
			foreach (var value in values)
			{
				var top = chk >> 25;
				chk = value ^ ((chk & 0x1ffffff) << 5);
				foreach (var i in Enumerable.Range(0, 5))
				{
					chk ^= ((top >> i) & 1) == 1 ? Generator[i] : 0;
				}
			}
			return chk;
		}
		
		private bool VerifyChecksum(byte[] data)
		{
			var values = _HrpExpand.Concat(data);
			return Polymod(values) == 1;
		}

		private byte[] CreateChecksum(byte[] data)
		{
			var values = _HrpExpand.Concat(data, new byte[] { 0, 0, 0, 0, 0, 0 });
			var polymod = Polymod(values) ^ 1;
			var ret = new byte[6];
			foreach (var i in Enumerable.Range(0, 6))
			{
				ret[i] = (byte)((polymod >> 5 * (5 - i)) & 31);
			}
			return ret;
		}

		public string Bech32Encode(byte[] data)
		{
			var combined = data.Concat(CreateChecksum(data));
			var tmp = new byte[combined.Length];
			for (int i = 0; i < combined.Length; i++)
			{
				tmp[i] = Byteset[combined[i]];
			}
			return Encoders.ASCII.EncodeData(_Hrp.Concat(new byte[] { 49 }, tmp));
		}

		internal static void CheckCase(string hrp)
		{
			if(hrp.ToLowerInvariant().Equals(hrp))
				return;
			if(hrp.ToUpperInvariant().Equals(hrp))
				return;
			throw new FormatException("Invalid bech32 string, mixed case detected");
		}

		public static Bech32Encoder ExtractEncoderFromString(string test)
		{
			var i = test.IndexOf('1');
			if(i == -1)
				throw new FormatException("Invalid Bech32 string");
			return Encoders.Bech32(test.Substring(0, i));
		}

		public byte[] Bech32Decode(string bech)
		{
			if(bech == null)
				throw new ArgumentNullException("bech");
			CheckCase(bech);
			var buffer = Encoders.ASCII.DecodeData(bech);
			if (buffer.Any(b => b < 33 || b > 126))
			{
				throw new FormatException("bech chars are out of range");
			}
			bech = bech.ToLowerInvariant();
			buffer = Encoders.ASCII.DecodeData(bech);
			var pos = bech.LastIndexOf("1", StringComparison.InvariantCultureIgnoreCase);
			if (pos < 1 || pos + 7 > bech.Length || bech.Length > 90)
			{
				throw new FormatException("bech missing separator, separator misplaced or too long input");
			}
			if (buffer.Skip(pos + 1).Any(x => !Byteset.Contains(x)))
			{
				throw new FormatException("bech chars are out of range");
			}

			buffer = Encoders.ASCII.DecodeData(bech);
			var hrp = Encoders.ASCII.DecodeData(bech.Substring(0, pos));
			if(!hrp.SequenceEqual(_Hrp))
			{
				throw new FormatException("Mismatching human readeable part");
			}
			var data = new byte[bech.Length - pos - 1];
			for (int j = 0, i = pos + 1; i < bech.Length; i++, j++)
			{
				data[j] = (byte)Array.IndexOf(Byteset, buffer[i]);
			}
			if (!VerifyChecksum(data))
			{
				throw new FormatException("Error while veriying checksum");
			}
			return data.Take(data.Length - 6).ToArray();
		}

		private static byte[] ConvertBits(IEnumerable<byte> data, int fromBits, int toBits, bool pad = true)
		{
			var acc = 0;
			var bits = 0;
			var maxv = (1 << toBits) - 1;
			var ret = new List<byte>();
			foreach (var value in data)
			{
				if ((value >> fromBits) > 0)
					throw new FormatException("Invalid Bech32 string");
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
				throw new FormatException("Invalid Bech32 string");
			}
			return ret.ToArray();
		}

		public byte[] Decode(string addr, out byte witnessVerion)
		{
			if(addr == null)
				throw new ArgumentNullException("addr");
			CheckCase(addr);
			var data = Bech32Decode(addr);

			var decoded = ConvertBits(data.Skip(1), 5, 8, false);
			if (decoded.Length < 2 || decoded.Length > 40)
				throw new FormatException("Invalid decoded data length");

			witnessVerion = data[0];
			if (witnessVerion > 16)
				throw new FormatException("Invalid decoded witness version");

			if (witnessVerion == 0 && decoded.Length != 20 && decoded.Length != 32)
				throw new FormatException("Decoded witness program with unknown length");
			return decoded;
		}

		public string Encode(byte witnessVerion, byte[] witnessProgramm)
		{
			var data = (new[] { witnessVerion }).Concat(ConvertBits(witnessProgramm, 8, 5));
			var ret = Bech32Encode(data);
			return ret;
		}
	}
}
