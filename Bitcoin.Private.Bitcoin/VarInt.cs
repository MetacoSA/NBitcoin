using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitcoin.Private.Bitcoin
{
	//https://en.bitcoin.it/wiki/Protocol_specification#Variable_length_integer
	public class VarInt
	{
		private long _Value;

		public VarInt(long value)
		{
			this._Value = value;
		}

		public byte[] ToBytes()
		{
			if(_Value < 0xFD)
				return new byte[1] { (byte)_Value };
			if(_Value <= 0xffff)
			{
				var v = (short)_Value;
				return new byte[3]
				{
					0xfd,
					(byte)_Value,
					(byte)(_Value >> 8)
				};
			}
			if(_Value <= 0xffffffff)
			{
				var v = (uint)_Value;
				return new byte[5]
				{
					0xfe,
					(byte)_Value,
					(byte)(_Value >> 8),
					(byte)(_Value >> 16),
					(byte)(_Value >> 24)
				};
			}
			else
			{
				var v = (ulong)_Value;
				return new byte[9]
				{
					0xff,
					(byte)_Value,
					(byte)(_Value >> 8),
					(byte)(_Value >> 16),
					(byte)(_Value >> 24),
					(byte)(_Value >> 32),
					(byte)(_Value >> 40),
					(byte)(_Value >> 48),
					(byte)(_Value >> 56)
				};
			}
		}
	}
}
