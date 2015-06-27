using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using NBitcoin.Crypto;

namespace NBitcoin
{
	public abstract class Packer
	{

		private static readonly Packer SwapConv = new SwapConverter();
		private static readonly Packer CopyConv = new CopyConverter();

		public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

		public bool ToBoolean(byte[] value, int offset)
		{
			return BitConverter.ToBoolean(value, offset);
		}

		public char ToChar(byte[] value, int offset)
		{
			return unchecked((char)(FromBytes(value, offset, sizeof(char))));
		}

		public short ToInt16(byte[] value, int offset)
		{
			return unchecked((short)(FromBytes(value, offset, sizeof(short))));
		}

		public int ToInt32(byte[] value, int offset)
		{
			return unchecked((int)(FromBytes(value, offset, sizeof(int))));
		}

		public long ToInt64(byte[] value, int offset)
		{
			return FromBytes(value, offset, sizeof(long));
		}

		public ushort ToUInt16(byte[] value, int offset)
		{
			return unchecked((ushort)(FromBytes(value, offset, sizeof(ushort))));
		}

		public uint ToUInt32(byte[] value, int offset)
		{
			return unchecked((uint)(FromBytes(value, offset, sizeof(uint))));
		}

		public ulong ToUInt64(byte[] value, int offset)
		{
			return unchecked((ulong)(FromBytes(value, offset, sizeof(ulong))));
		}

		private byte[] GetBytes(long value, int bytes)
		{
			var buffer = new byte[bytes];
			CopyBytes(value, bytes, buffer, 0);
			return buffer;
		}

		private byte[] GetBytes(byte[] value, int bytes)
		{
			var buffer = new byte[bytes];
			CopyBytes(value, bytes, buffer, 0);
			return buffer;
		}

		public byte[] GetBytes(bool value)
		{
			return BitConverter.GetBytes(value);
		}

		public byte[] GetBytes(char value)
		{
			return GetBytes(value, sizeof(char));
		}

		public byte[] GetBytes(short value)
		{
			return GetBytes(value, sizeof(short));
		}

		public byte[] GetBytes(int value)
		{
			return GetBytes(value, sizeof(int));
		}

		public byte[] GetBytes(long value)
		{
			return GetBytes(value, sizeof(long));
		}

		public byte[] GetBytes(ushort value)
		{
			return GetBytes(value, sizeof(ushort));
		}

		public byte[] GetBytes(uint value)
		{
			return GetBytes(value, sizeof(uint));
		}

		public byte[] GetBytes(ulong value)
		{
			return GetBytes(unchecked((long)value), sizeof(ulong));
		}

		public byte[] GetBytes(uint160 value)
		{
			return GetBytes(value.ToBytes(), value.Size);
		}

		public byte[] GetBytes(uint256 value)
		{
			return GetBytes(value.ToBytes(), value.Size);
		}

		public byte[] GetBytes(byte[] value)
		{
			return GetBytes(value, value.Length);
		}

		protected abstract long FromBytes(byte[] value, int offset, int count);
		protected abstract void CopyBytes(long value, int bytes, byte[] buffer, int index);
		protected abstract void CopyBytes(byte[] output, int bytes, byte[] buffer, int index);


		static public Packer LittleEndian
		{
			get
			{
				return BitConverter.IsLittleEndian ? CopyConv : SwapConv;
			}
		}

		static public Packer BigEndian
		{
			get
			{
				return BitConverter.IsLittleEndian ? SwapConv : CopyConv;
			}
		}

		static public Packer Native
		{
			get
			{
				return CopyConv;
			}
		}

		static int Align(int current, int align)
		{
			return ((current + align - 1) / align) * align;
		}

		class PackContext
		{
			// Buffer
			private byte[] _buffer;
			int _next;

			public string description;
			public int i; // position in the description
			public Packer conv;
			public int repeat;

			//
			// if align == -1, auto align to the size of the byte array
			// if align == 0, do not do alignment
			// Any other values aligns to that particular size
			//
			public int align;

			public void Add(byte[] group)
			{
				//Console.WriteLine ("Adding {0} bytes to {1} (next={2}", group.Length,
				// buffer == null ? "null" : buffer.Length.ToString (), next);

				if (_buffer == null)
				{
					_buffer = group;
					_next = group.Length;
					return;
				}
				if (align != 0)
				{
					if (align == -1)
						_next = Align(_next, group.Length);
					else
						_next = Align(_next, align);
					align = 0;
				}

				if (_next + group.Length > _buffer.Length)
				{
					byte[] nb = new byte[System.Math.Max(_next, 16) * 2 + group.Length];
					Array.Copy(_buffer, nb, _buffer.Length);
					Array.Copy(group, 0, nb, _next, group.Length);
					_next = _next + group.Length;
					_buffer = nb;
				}
				else
				{
					Array.Copy(group, 0, _buffer, _next, group.Length);
					_next += group.Length;
				}
			}

			public byte[] Get()
			{
				if (_buffer == null)
					return new byte[0];

				if (_buffer.Length != _next)
				{
					byte[] b = new byte[_next];
					Array.Copy(_buffer, b, _next);
					return b;
				}
				return _buffer;
			}
		}

		//
		// Format includes:
		// Control:
		//   ^    Switch to big endian encoding
		//   _    Switch to little endian encoding
		//   %    Switch to host (native) encoding
		//   !    aligns the next data type to its natural boundary (for strings this is 4).
		//
		// Types:
		//   s    Int16
		//   S    UInt16
		//   i    Int32
		//   I    UInt32
		//   l    Int64
		//   L    UInt64
		//   f    float
		//   d    double
		//   b    byte
		//   c    1-byte signed character
		//   C    1-byte unsigned character
		//   z8   string encoded as UTF8 with 1-byte null terminator
		//   z6   string encoded as UTF16 with 2-byte null terminator
		//   z7   string encoded as UTF7 with 1-byte null terminator
		//   zb   string encoded as BigEndianUnicode with 2-byte null terminator
		//   z3   string encoded as UTF32 with 4-byte null terminator
		//   z4   string encoded as UTF32 big endian with 4-byte null terminator
		//   $8   string encoded as UTF8
		//   $6   string encoded as UTF16
		//   $7   string encoded as UTF7
		//   $b   string encoded as BigEndianUnicode
		//   $3   string encoded as UTF32
		//   $4   string encoded as UTF-32 big endian encoding
		//   x    null byte
		//
		// Repeats, these are prefixes:
		//   N    a number between 1 and 9, indicates a repeat count (process N items
		//        with the following datatype
		//   [N]  For numbers larger than 9, use brackets, for example [20]
		//   *    Repeat the next data type until the arguments are exhausted
		//
		static public byte[] Pack(string description, params object[] args)
		{
			int argn = 0;
			var b = new PackContext();
			b.conv = CopyConv;
			b.description = description;

			for (b.i = 0; b.i < description.Length; )
			{
				object oarg;

				if (argn < args.Length)
					oarg = args[argn];
				else
				{
					if (b.repeat != 0)
						break;

					oarg = null;
				}

				int save = b.i;

				if (PackOne(b, oarg))
				{
					argn++;
					if (b.repeat > 0)
					{
						if (--b.repeat > 0)
							b.i = save;
						else
							b.i++;
					}
					else
						b.i++;
				}
				else
					b.i++;
			}
			return b.Get();
		}

		static public byte[] PackEnumerable(string description, IEnumerable args)
		{
			var b = new PackContext();
			b.conv = CopyConv;
			b.description = description;

			IEnumerator enumerator = args.GetEnumerator();
			bool ok = enumerator.MoveNext();

			for (b.i = 0; b.i < description.Length; )
			{
				object oarg;

				if (ok)
					oarg = enumerator.Current;
				else
				{
					if (b.repeat != 0)
						break;
					oarg = null;
				}

				int save = b.i;

				if (PackOne(b, oarg))
				{
					ok = enumerator.MoveNext();
					if (b.repeat > 0)
					{
						if (--b.repeat > 0)
							b.i = save;
						else
							b.i++;
					}
					else
						b.i++;
				}
				else
					b.i++;
			}
			return b.Get();
		}

		static public uint256 Hash256(string description, params object[] args)
		{
			return Hashes.Hash256(Pack(description, args));
		}

		//
		// Packs one datum `oarg' into the buffer `b', using the string format
		// in `description' at position `i'
		//
		// Returns: true if we must pick the next object from the list
		//
		static bool PackOne(PackContext b, object oarg)
		{
			int n;

			switch (b.description[b.i])
			{
				case '^':
					b.conv = BigEndian;
					return false;
				case '_':
					b.conv = LittleEndian;
					return false;
				case '%':
					b.conv = Native;
					return false;

				case '!':
					b.align = -1;
					return false;

				case 'x':
					b.Add(new byte[] { 0 });
					return false;

				// Type Conversions
				case 'i':
					b.Add(b.conv.GetBytes(Convert.ToInt32(oarg)));
					break;

				case 'I':
					b.Add(b.conv.GetBytes(Convert.ToUInt32(oarg)));
					break;

				case 's':
					b.Add(b.conv.GetBytes(Convert.ToInt16(oarg)));
					break;

				case 'S':
					b.Add(b.conv.GetBytes(Convert.ToUInt16(oarg)));
					break;

				case 'l':
					b.Add(b.conv.GetBytes(Convert.ToInt64(oarg)));
					break;

				case 'L':
					b.Add(b.conv.GetBytes(Convert.ToUInt64(oarg)));
					break;

				case 'b':
					b.Add(new[] { Convert.ToByte(oarg) });
					break;

				case 'c':
					b.Add(new[] { (byte)(Convert.ToSByte(oarg)) });
					break;

				case 'C':
					b.Add(new[] { Convert.ToByte(oarg) });
					break;
				case 'X':
					b.Add(b.conv.GetBytes(((IBitcoinSerializable)oarg).ToBytes()));
					break;
				case 'A':
					b.Add(b.conv.GetBytes((byte[])oarg));
					break;

				// Repeat acount;
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					b.repeat = ((short)b.description[b.i]) - ((short)'0');
					return false;

				case '*':
					b.repeat = Int32.MaxValue;
					return false;

				case '[':
					int count = -1, j;

					for (j = b.i + 1; j < b.description.Length; j++)
					{
						if (b.description[j] == ']')
							break;
						n = ((short)b.description[j]) - ((short)'0');
						if (n >= 0 && n <= 9)
						{
							if (count == -1)
								count = n;
							else
								count = count * 10 + n;
						}
					}
					if (count == -1)
						throw new ArgumentException("invalid size specification");
					b.i = j;
					b.repeat = count;
					return false;

				case '$':
				case 'z':
					bool add_null = b.description[b.i] == 'z';
					b.i++;
					if (b.i >= b.description.Length)
						throw new ArgumentException("$ description needs a type specified", "description");
					char d = b.description[b.i];
					Encoding e;

					switch (d)
					{
						case '8':
							e = Encoding.UTF8;
							n = 1;
							break;
						case '6':
							e = Encoding.Unicode;
							n = 2;
							break;
						case '7':
#if PCL
					e = Encoding.GetEncoding ("utf-7");
#else
							e = Encoding.UTF7;
#endif
							n = 1;
							break;
						case 'b':
							e = Encoding.BigEndianUnicode;
							n = 2;
							break;
						case '3':
#if PCL
					e = Encoding.GetEncoding ("utf-32");
#else
							e = Encoding.GetEncoding(12000);
#endif
							n = 4;
							break;
						case '4':
#if PCL
					e = Encoding.GetEncoding ("utf-32BE");
#else
							e = Encoding.GetEncoding(12001);
#endif
							n = 4;
							break;

						default:
							throw new ArgumentException("Invalid format for $ specifier", "description");
					}
					if (b.align == -1)
						b.align = 4;
					b.Add(e.GetBytes(Convert.ToString(oarg)));
					if (add_null)
						b.Add(new byte[n]);
					break;
				default:
					throw new ArgumentException(String.Format("invalid format specified `{0}'",
											b.description[b.i]));
			}
			return true;
		}

		static bool Prepare(byte[] buffer, ref int idx, int size, ref bool align)
		{
			if (align)
			{
				idx = Align(idx, size);
				align = false;
			}
			if (idx + size > buffer.Length)
			{
				idx = buffer.Length;
				return false;
			}
			return true;
		}

		static public IList Unpack(string description, byte[] buffer, int startIndex)
		{
			Packer conv = CopyConv;
			var result = new List<object>();
			int idx = startIndex;
			bool align = false;
			int repeat = 0, n;

			for (int i = 0; i < description.Length && idx < buffer.Length; )
			{
				int save = i;

				switch (description[i])
				{
					case '^':
						conv = BigEndian;
						break;
					case '_':
						conv = LittleEndian;
						break;
					case '%':
						conv = Native;
						break;
					case 'x':
						idx++;
						break;

					case '!':
						align = true;
						break;

					// Type Conversions
					case 'i':
						if (Prepare(buffer, ref idx, 4, ref align))
						{
							result.Add(conv.ToInt32(buffer, idx));
							idx += 4;
						}
						break;

					case 'I':
						if (Prepare(buffer, ref idx, 4, ref align))
						{
							result.Add(conv.ToUInt32(buffer, idx));
							idx += 4;
						}
						break;

					case 's':
						if (Prepare(buffer, ref idx, 2, ref align))
						{
							result.Add(conv.ToInt16(buffer, idx));
							idx += 2;
						}
						break;

					case 'S':
						if (Prepare(buffer, ref idx, 2, ref align))
						{
							result.Add(conv.ToUInt16(buffer, idx));
							idx += 2;
						}
						break;

					case 'l':
						if (Prepare(buffer, ref idx, 8, ref align))
						{
							result.Add(conv.ToInt64(buffer, idx));
							idx += 8;
						}
						break;

					case 'L':
						if (Prepare(buffer, ref idx, 8, ref align))
						{
							result.Add(conv.ToUInt64(buffer, idx));
							idx += 8;
						}
						break;

					case 'b':
						if (Prepare(buffer, ref idx, 1, ref align))
						{
							result.Add(buffer[idx]);
							idx++;
						}
						break;

					case 'c':
					case 'C':
						if (Prepare(buffer, ref idx, 1, ref align))
						{
							char c;

							if (description[i] == 'c')
								c = ((char)((sbyte)buffer[idx]));
							else
								c = ((char)(buffer[idx]));

							result.Add(c);
							idx++;
						}
						break;
					case 'A':
						if (Prepare(buffer, ref idx, buffer.Length, ref align))
						{
							result.Add(buffer);
							idx += buffer.Length;
						}
						break;
					case 'U':
						if (Prepare(buffer, ref idx, 32, ref align))
						{
							result.Add(new uint256(buffer.SafeSubarray(idx)));
							idx += 32;
						}
						break;
					case 'u':
						if (Prepare(buffer, ref idx, 20, ref align))
						{
							result.Add(new uint160(buffer.SafeSubarray(idx)));
							idx += 20;
						}
						break;


					// Repeat acount;
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
						repeat = ((short)description[i]) - ((short)'0');
						save = i + 1;
						break;

					case '*':
						repeat = Int32.MaxValue;
						break;

					case '[':
						int count = -1, j;

						for (j = i + 1; j < description.Length; j++)
						{
							if (description[j] == ']')
								break;
							n = ((short)description[j]) - ((short)'0');
							if (n >= 0 && n <= 9)
							{
								if (count == -1)
									count = n;
								else
									count = count * 10 + n;
							}
						}
						if (count == -1)
							throw new ArgumentException("invalid size specification");
						i = j;
						save = i + 1;
						repeat = count;
						break;

					case '$':
					case 'z':
						// bool with_null = description [i] == 'z';
						i++;
						if (i >= description.Length)
							throw new ArgumentException("$ description needs a type specified", "description");
						char d = description[i];
						Encoding e;
						if (align)
						{
							idx = Align(idx, 4);
							align = false;
						}
						if (idx >= buffer.Length)
							break;

						switch (d)
						{
							case '8':
								e = Encoding.UTF8;
								n = 1;
								break;
							case '6':
								e = Encoding.Unicode;
								n = 2;
								break;
							case '7':
								e = Encoding.UTF7;
								n = 1;
								break;
							case 'b':
								e = Encoding.BigEndianUnicode;
								n = 2;
								break;
							case '3':
								e = Encoding.GetEncoding(12000);
								n = 4;
								break;
							case '4':
								e = Encoding.GetEncoding(12001);
								n = 4;
								break;

							default:
								throw new ArgumentException("Invalid format for $ specifier", "description");
						}
						int k = idx;
						switch (n)
						{
							case 1:
								for (; k < buffer.Length && buffer[k] != 0; k++)
								{
								}
								result.Add(e.GetChars(buffer, idx, k - idx));
								if (k == buffer.Length)
									idx = k;
								else
									idx = k + 1;
								break;

							case 2:
								for (; k < buffer.Length; k++)
								{
									if (k + 1 == buffer.Length)
									{
										k++;
										break;
									}
									if (buffer[k] == 0 && buffer[k + 1] == 0)
										break;
								}
								result.Add(e.GetChars(buffer, idx, k - idx));
								if (k == buffer.Length)
									idx = k;
								else
									idx = k + 2;
								break;

							case 4:
								for (; k < buffer.Length; k++)
								{
									if (k + 3 >= buffer.Length)
									{
										k = buffer.Length;
										break;
									}
									if (buffer[k] == 0 && buffer[k + 1] == 0 && buffer[k + 2] == 0 && buffer[k + 3] == 0)
										break;
								}
								result.Add(e.GetChars(buffer, idx, k - idx));
								if (k == buffer.Length)
									idx = k;
								else
									idx = k + 4;
								break;
						}
						break;
					default:
						throw new ArgumentException(String.Format("invalid format specified `{0}'",
												description[i]));
				}

				if (repeat > 0)
				{
					if (--repeat > 0)
						i = save;
				}
				else
					i++;
			}
			return result;
		}

		internal void Check(byte[] dest, int destIdx, int size)
		{
			if (dest == null)
				throw new ArgumentNullException("dest");
			if (destIdx < 0 || destIdx > dest.Length - size)
				throw new ArgumentException("destIdx");
		}

		class CopyConverter : Packer
		{
			protected override long FromBytes(byte[] value, int offset, int count)
			{
				long ret = 0;
				for (var i = 0; i < count; i++)
				{
					ret = unchecked((ret << 8) | value[offset + count - 1 - i]);
				}
				return ret;
			}

			protected override void CopyBytes(long value, int bytes, byte[] buffer, int index)
			{
				for (var i = 0; i < bytes; i++)
				{
					buffer[i + index] = unchecked((byte)(value & 0xff));
					value = value >> 8;
				}
			}

			protected override void CopyBytes(byte[] output, int bytes, byte[] buffer, int index)
			{
				Array.Copy(output, index, buffer, 0, bytes);
			}
		}

		class SwapConverter : Packer
		{
			protected override long FromBytes(byte[] buffer, int offset, int count)
			{
				long ret = 0;
				for (var i = 0; i < count; i++)
				{
					ret = unchecked((ret << 8) | buffer[offset + i]);
				}
				return ret;
			}

			protected override void CopyBytes(long value, int bytes, byte[] buffer, int index)
			{
				var endOffset = index + bytes - 1;
				for (var i = 0; i < bytes; i++)
				{
					buffer[endOffset - i] = unchecked((byte)(value & 0xff));
					value = value >> 8;
				}
			}

			protected override void CopyBytes(byte[] output, int bytes, byte[] buffer, int index)
			{
				Array.Copy(buffer, index, output, 0, bytes);
				Array.Reverse(output);
			}
		}
	}
}