using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	public class uint256_tests
	{
		public uint256_tests()
		{
			R1Array = ToBytes("\x9c\x52\x4a\xdb\xcf\x56\x11\x12\x2b\x29\x12\x5e\x5d\x35\xd2\xd2\x22\x81\xaa\xb5\x33\xf0\x08\x32\xd5\x56\xb1\xf9\xea\xe5\x1d\x7d");
			R1L = new uint256(R1Array);
			NegR1L = ~R1L;
			R1S = new uint160(R1Array.Take(20).ToArray());
			NegR1S = ~R1S;
			NegR1Array = NegR1L.ToBytes();

			R2Array = ToBytes("\x70\x32\x1d\x7c\x47\xa5\x6b\x40\x26\x7e\x0a\xc3\xa6\x9c\xb6\xbf\x13\x30\x47\xa3\x19\x2d\xda\x71\x49\x13\x72\xf0\xb4\xca\x81\xd7");
			R2L = new uint256(R2Array);
			NegR2L = ~R2L;
			R2S = new uint160(R2Array.Take(20).ToArray());
			NegR2S = ~R2S;
			NegR2Array = NegR2L.ToBytes();

			ZeroArray = ToBytes("\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00");
			ZeroL = new uint256(ZeroArray);
			ZeroS = new uint160(ZeroArray.Take(20).ToArray());

			OneArray = ToBytes("\x01\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00");
			OneL = new uint256(OneArray);
			OneS = new uint160(OneArray.Take(20).ToArray());

			MaxArray = ToBytes("\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff\xff");
			MaxL = new uint256(MaxArray);
			MaxS = new uint160(MaxArray.Take(20).ToArray());

			HalfL = OneL << 255;
			HalfS = OneS << 159;
		}

		private byte[] ToBytes(string data)
		{

			var b = TestUtils.ToBytes(data);
			return b;
		}
		ulong R1LLow64 = 0x121156cfdb4a529cUL;
		const double R1Ldouble = 0.4887374590559308955; // R1L equals roughly R1Ldouble * 2^256
		const double R1Sdouble = 0.7096329412477836074;
		string R1LplusR2L = "549FB09FEA236A1EA3E31D4D58F1B1369288D204211CA751527CFC175767850C";
		uint256 HalfL;
		uint160 HalfS;
		byte[] R1Array;
		uint256 R1L;
		uint256 NegR1L;
		uint160 R1S;
		uint160 NegR1S;
		uint256 R2L;
		uint256 NegR2L;
		uint160 R2S;
		uint160 NegR2S;
		uint256 ZeroL;
		uint160 ZeroS;
		uint256 OneL;
		uint160 OneS;
		uint256 MaxL;
		uint160 MaxS;
		private byte[] R2Array;
		private byte[] NegR1Array;
		private byte[] NegR2Array;
		private byte[] ZeroArray;
		private byte[] OneArray;
		private byte[] MaxArray;
		string R1ArrayHex = "7D1DE5EAF9B156D53208F033B5AA8122D2d2355d5e12292b121156cfdb4a529c";


		private string ArrayToString(byte[] array)
		{
			StringBuilder builder = new StringBuilder();
			for(int i = 0 ; i < array.Length ; i++)
			{
				builder.AppendFormat("{0:x2}", array[array.Length - i - 1]);
			}
			return builder.ToString();
		}


		[Fact]
		[Trait("Core", "Core")]
		public void unaryOperators()
		{
			Assert.True(!ZeroL);
			Assert.True(!ZeroS);
			Assert.True(!(!OneL));
			Assert.True(!(!OneS));
			for(int i = 0 ; i < 256 ; ++i)
				Assert.True(!(!(OneL << i)));
			for(int i = 0 ; i < 160 ; ++i)
				Assert.True(!(!(OneS << i)));
			Assert.True(!(!R1L));
			Assert.True(!(!R1S));
			Assert.True(!(!R2S));
			Assert.True(!(!R2S));
			Assert.True(!(!MaxL));
			Assert.True(!(!MaxS));

			Assert.True(~ZeroL == MaxL);
			Assert.True(~ZeroS == MaxS);

			byte[] TmpArray = new byte[32];
			for(int i = 0 ; i < 32 ; ++i)
			{
				TmpArray[i] = (byte)(~R1Array[i]);
			}
			Assert.True(new uint256(TmpArray) == (~R1L));
			Assert.True(new uint160(TmpArray.Take(20).ToArray()) == (~R1S));

			Assert.True(-ZeroL == ZeroL);
			Assert.True(-ZeroS == ZeroS);
			Assert.True(-R1L == (~R1L) + 1);
			Assert.True(-R1S == (~R1S) + 1);
			for(int i = 0 ; i < 256 ; ++i)
				Assert.True(-(OneL << i) == (MaxL << i));
			for(int i = 0 ; i < 160 ; ++i)
				Assert.True(-(OneS << i) == (MaxS << i));
		}
		[Fact]
		[Trait("Core", "Core")]
		public void methods()
		{
			Assert.True(R1L.GetHex() == R1L.ToString());
			Assert.True(R2L.GetHex() == R2L.ToString());
			Assert.True(OneL.GetHex() == OneL.ToString());
			Assert.True(MaxL.GetHex() == MaxL.ToString());
			uint256 TmpL = new uint256(R1L);
			Assert.True(TmpL == R1L);
			TmpL.SetHex(R2L.ToString());
			Assert.True(TmpL == R2L);
			TmpL.SetHex(ZeroL.ToString());
			Assert.True(TmpL == 0);
			TmpL.SetHex(HalfL.ToString());
			Assert.True(TmpL == HalfL);

			TmpL.SetHex(R1L.ToString());
			AssertEx.CollectionEquals(R1L.ToBytes(), R1Array);
			AssertEx.CollectionEquals(TmpL.ToBytes(), R1Array);
			AssertEx.CollectionEquals(R2L.ToBytes(), R2Array);
			AssertEx.CollectionEquals(ZeroL.ToBytes(), ZeroArray);
			AssertEx.CollectionEquals(OneL.ToBytes(), OneArray);
			Assert.True(R1L.Size == 32);
			Assert.True(R2L.Size == 32);
			Assert.True(ZeroL.Size == 32);
			Assert.True(MaxL.Size == 32);

			//No sense in .NET
			//Assert.True(R1L.begin() + 32 == R1L.end());
			//Assert.True(R2L.begin() + 32 == R2L.end());
			//Assert.True(OneL.begin() + 32 == OneL.end());
			//Assert.True(MaxL.begin() + 32 == MaxL.end());
			//Assert.True(TmpL.begin() + 32 == TmpL.end());
			Assert.True(R1L.GetLow64() == R1LLow64);
			Assert.True(HalfL.GetLow64() == 0x0000000000000000UL);
			Assert.True(OneL.GetLow64() == 0x0000000000000001UL);
			Assert.True(R1L.GetSerializeSize(0, ProtocolVersion.PROTOCOL_VERSION) == 32);
			Assert.True(ZeroL.GetSerializeSize(0, ProtocolVersion.PROTOCOL_VERSION) == 32);

			MemoryStream ss = new MemoryStream();
			R1L.Serialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(ArrayToString(ss.ToArray()) == ArrayToString(R1Array));
			TmpL.Unserialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(R1L == TmpL);
			ss = new MemoryStream();
			ZeroL.Serialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(ArrayToString(ss.ToArray()) == ArrayToString(ZeroArray));
			ss.Position = 0;
			TmpL.Unserialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(ZeroL == TmpL);
			ss = new MemoryStream();
			MaxL.Serialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(ArrayToString(ss.ToArray()) == ArrayToString(MaxArray));
			ss.Position = 0;
			TmpL.Unserialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(MaxL == TmpL);
			ss = new MemoryStream();

			Assert.True(R1S.GetHex() == R1S.ToString());
			Assert.True(R2S.GetHex() == R2S.ToString());
			Assert.True(OneS.GetHex() == OneS.ToString());
			Assert.True(MaxS.GetHex() == MaxS.ToString());
			uint160 TmpS = new uint160(R1S);
			Assert.True(TmpS == R1S);
			TmpS.SetHex(R2S.ToString());
			Assert.True(TmpS == R2S);
			TmpS.SetHex(ZeroS.ToString());
			Assert.True(TmpS == 0);
			TmpS.SetHex(HalfS.ToString());
			Assert.True(TmpS == HalfS);

			TmpS.SetHex(R1S.ToString());

			Assert.True(ArrayToString(R1S.ToBytes()) == ArrayToString(R1Array.Take(20).ToArray()));
			Assert.True(ArrayToString(TmpS.ToBytes()) == ArrayToString(R1Array.Take(20).ToArray()));
			Assert.True(ArrayToString(R2S.ToBytes()) == ArrayToString(R2Array.Take(20).ToArray()));
			Assert.True(ArrayToString(ZeroS.ToBytes()) == ArrayToString(ZeroArray.Take(20).ToArray()));
			Assert.True(ArrayToString(OneS.ToBytes()) == ArrayToString(OneArray.Take(20).ToArray()));
			Assert.True(R1S.Size == 20);
			Assert.True(R2S.Size == 20);
			Assert.True(ZeroS.Size == 20);
			Assert.True(MaxS.Size == 20);
			//No sense in .NET
			//Assert.True(R1S.begin() + 20 == R1S.end());
			//Assert.True(R2S.begin() + 20 == R2S.end());
			//Assert.True(OneS.begin() + 20 == OneS.end());
			//Assert.True(MaxS.begin() + 20 == MaxS.end());
			//Assert.True(TmpS.begin() + 20 == TmpS.end());
			Assert.True(R1S.GetLow64() == R1LLow64);
			Assert.True(HalfS.GetLow64() == 0x0000000000000000UL);
			Assert.True(OneS.GetLow64() == 0x0000000000000001UL);
			Assert.True(R1S.GetSerializeSize(0, ProtocolVersion.PROTOCOL_VERSION) == 20);
			Assert.True(ZeroS.GetSerializeSize(0, ProtocolVersion.PROTOCOL_VERSION) == 20);

			R1S.Serialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(ArrayToString(ss.ToArray()) == ArrayToString(R1Array.Take(20).ToArray()));
			ss.Position = 0;
			TmpS.Unserialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(R1S == TmpS);
			ss = new MemoryStream();
			ZeroS.Serialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(ArrayToString(ss.ToArray()) == ArrayToString(ZeroArray.Take(20).ToArray()));
			ss.Position = 0;
			TmpS.Unserialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(ZeroS == TmpS);
			ss = new MemoryStream();
			MaxS.Serialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(ArrayToString(ss.ToArray()) == ArrayToString(MaxArray.Take(20).ToArray()));
			ss.Position = 0;
			TmpS.Unserialize(ss, 0, ProtocolVersion.PROTOCOL_VERSION);
			Assert.True(MaxS == TmpS);
			ss = new MemoryStream();

			//for(int i = 0 ; i < 255 ; ++i)
			//{
			//	Assert.True((OneL << i).GetDouble() == Math.Pow(1.0, i));
			//	if(i < 160)
			//		Assert.True((OneS << i).GetDouble() == Math.Pow(1.0, i));
			//}
			//Assert.True(ZeroL.GetDouble() == 0.0);
			//Assert.True(ZeroS.GetDouble() == 0.0);
			//for(int i = 256 ; i > 53 ; --i)
			//	Assert.True(almostEqual((R1L >> (256 - i)).GetDouble(), Math.Pow(R1Ldouble, i)));
			//for(int i = 160 ; i > 53 ; --i)
			//	Assert.True(almostEqual((R1S >> (160 - i)).GetDouble(), Math.Pow(R1Sdouble, i)));
			//ulong R1L64part = (R1L >> 192).GetLow64();
			//ulong R1S64part = (R1S >> 96).GetLow64();
			//for(int i = 53 ; i > 0 ; --i) // doubles can store all integers in {0,...,2^54-1} exactly
			//{
			//	Assert.True((R1L >> (256 - i)).GetDouble() == (double)(R1L64part >> (64 - i)));
			//	Assert.True((R1S >> (160 - i)).GetDouble() == (double)(R1S64part >> (64 - i)));
			//}
		}
		bool almostEqual(double d1, double d2)
		{
			return Math.Abs(d1 - d2) <= 4 * Math.Abs(d1) * double.Epsilon;
		}

		[Fact]
		[Trait("Core", "Core")]
		public void plusMinus()
		{
			uint256 TmpL = 0;
			Assert.True(R1L + R2L == new uint256(R1LplusR2L));
			TmpL += R1L;
			Assert.True(TmpL == R1L);
			TmpL += R2L;
			Assert.True(TmpL == R1L + R2L);
			Assert.True(OneL + MaxL == ZeroL);
			Assert.True(MaxL + OneL == ZeroL);
			for(int i = 1 ; i < 256 ; ++i)
			{
				Assert.True((MaxL >> i) + OneL == (HalfL >> (i - 1)));
				Assert.True(OneL + (MaxL >> i) == (HalfL >> (i - 1)));
				TmpL = (MaxL >> i);
				TmpL += OneL;
				Assert.True(TmpL == (HalfL >> (i - 1)));
				TmpL = (MaxL >> i);
				TmpL += 1;
				Assert.True(TmpL == (HalfL >> (i - 1)));
				TmpL = (MaxL >> i);
				Assert.True(TmpL++ == (MaxL >> i));
				Assert.True(TmpL == (HalfL >> (i - 1)));
			}
			Assert.True(new uint256(0xbedc77e27940a7UL) + 0xee8d836fce66fbUL == new uint256(0xbedc77e27940a7UL + 0xee8d836fce66fbUL));
			TmpL = new uint256(0xbedc77e27940a7UL);
			TmpL += 0xee8d836fce66fbUL;
			Assert.True(TmpL == new uint256(0xbedc77e27940a7UL + 0xee8d836fce66fbUL));
			TmpL -= 0xee8d836fce66fbUL;
			Assert.True(TmpL == 0xbedc77e27940a7UL);
			TmpL = R1L;
			Assert.True(++TmpL == R1L + 1);

			Assert.True(R1L - (-R2L) == R1L + R2L);
			Assert.True(R1L - (-OneL) == R1L + OneL);
			Assert.True(R1L - OneL == R1L + (-OneL));
			for(int i = 1 ; i < 256 ; ++i)
			{
				Assert.True((MaxL >> i) - (-OneL) == (HalfL >> (i - 1)));
				Assert.True((HalfL >> (i - 1)) - OneL == (MaxL >> i));
				TmpL = (HalfL >> (i - 1));
				Assert.True(TmpL-- == (HalfL >> (i - 1)));
				Assert.True(TmpL == (MaxL >> i));
				TmpL = (HalfL >> (i - 1));
				Assert.True(--TmpL == (MaxL >> i));
			}
			TmpL = R1L;
			Assert.True(--TmpL == R1L - 1);

			// 160-bit; copy-pasted
			uint160 TmpS = 0;
			Assert.True(R1S + R2S == new uint160(R1LplusR2L));
			TmpS += R1S;
			Assert.True(TmpS == R1S);
			TmpS += R2S;
			Assert.True(TmpS == R1S + R2S);
			Assert.True(OneS + MaxS == ZeroS);
			Assert.True(MaxS + OneS == ZeroS);
			for(int i = 1 ; i < 160 ; ++i)
			{
				Assert.True((MaxS >> i) + OneS == (HalfS >> (i - 1)));
				Assert.True(OneS + (MaxS >> i) == (HalfS >> (i - 1)));
				TmpS = (MaxS >> i);
				TmpS += OneS;
				Assert.True(TmpS == (HalfS >> (i - 1)));
				TmpS = (MaxS >> i);
				TmpS += 1;
				Assert.True(TmpS == (HalfS >> (i - 1)));
				TmpS = (MaxS >> i);
				Assert.True(TmpS++ == (MaxS >> i));
				Assert.True(TmpS == (HalfS >> (i - 1)));
			}
			Assert.True(new uint160(0xbedc77e27940a7UL) + 0xee8d836fce66fbUL == new uint160(0xbedc77e27940a7UL + 0xee8d836fce66fbUL));
			TmpS = new uint160(0xbedc77e27940a7UL);
			TmpS += 0xee8d836fce66fbUL;
			Assert.True(TmpS == new uint160(0xbedc77e27940a7UL + 0xee8d836fce66fbUL));
			TmpS -= 0xee8d836fce66fbUL;
			Assert.True(TmpS == 0xbedc77e27940a7UL);
			TmpS = R1S;
			Assert.True(++TmpS == R1S + 1);

			Assert.True(R1S - (-R2S) == R1S + R2S);
			Assert.True(R1S - (-OneS) == R1S + OneS);
			Assert.True(R1S - OneS == R1S + (-OneS));
			for(int i = 1 ; i < 160 ; ++i)
			{
				Assert.True((MaxS >> i) - (-OneS) == (HalfS >> (i - 1)));
				Assert.True((HalfS >> (i - 1)) - OneS == (MaxS >> i));
				TmpS = (HalfS >> (i - 1));
				Assert.True(TmpS-- == (HalfS >> (i - 1)));
				Assert.True(TmpS == (MaxS >> i));
				TmpS = (HalfS >> (i - 1));
				Assert.True(--TmpS == (MaxS >> i));
			}
			TmpS = R1S;
			Assert.True(--TmpS == R1S - 1);
		}

		[Fact]
		[Trait("Core", "Core")]
		public void getmaxcoverage()
		{
			// ~R1L give a base_uint<256>
			Assert.True((~~R1L >> 10) == (R1L >> 10));
			Assert.True((~~R1S >> 10) == (R1S >> 10));
			Assert.True((~~R1L << 10) == (R1L << 10));
			Assert.True((~~R1S << 10) == (R1S << 10));
			Assert.True(!(~~R1L < R1L));
			Assert.True(!(~~R1S < R1S));
			Assert.True(~~R1L <= R1L);
			Assert.True(~~R1S <= R1S);
			Assert.True(!(~~R1L > R1L));
			Assert.True(!(~~R1S > R1S));
			Assert.True(~~R1L >= R1L);
			Assert.True(~~R1S >= R1S);
			Assert.True(!(R1L < ~~R1L));
			Assert.True(!(R1S < ~~R1S));
			Assert.True(R1L <= ~~R1L);
			Assert.True(R1S <= ~~R1S);
			Assert.True(!(R1L > ~~R1L));
			Assert.True(!(R1S > ~~R1S));
			Assert.True(R1L >= ~~R1L);
			Assert.True(R1S >= ~~R1S);

			Assert.True(~~R1L + R2L == R1L + ~~R2L);
			Assert.True(~~R1S + R2S == R1S + ~~R2S);
			Assert.True(~~R1L - R2L == R1L - ~~R2L);
			Assert.True(~~R1S - R2S == R1S - ~~R2S);
			Assert.True(~R1L != R1L);
			Assert.True(R1L != ~R1L);
			Assert.True(~R1S != R1S);
			Assert.True(R1S != ~R1S);

			CHECKBITWISEOPERATOR("NegR1", "R2", '|');
			CHECKBITWISEOPERATOR("NegR1", "R2", '^');
			CHECKBITWISEOPERATOR("NegR1", "R2", '&');
			CHECKBITWISEOPERATOR("R1", "NegR2", '|');
			CHECKBITWISEOPERATOR("R1", "NegR2", '^');
			CHECKBITWISEOPERATOR("R1", "NegR2", '&');
		}
		[Fact]
		[Trait("Core", "Core")]
		public void comparison()
		{
			uint256 TmpL;
			for(int i = 0 ; i < 256 ; ++i)
			{
				TmpL = OneL << i;
				Assert.True(TmpL >= ZeroL && TmpL > ZeroL && ZeroL < TmpL && ZeroL <= TmpL);
				Assert.True(TmpL >= 0 && TmpL > 0 && 0 < TmpL && 0 <= TmpL);
				TmpL |= R1L;
				Assert.True(TmpL >= R1L);
				Assert.True((TmpL == R1L) != (TmpL > R1L));
				Assert.True((TmpL == R1L) || !(TmpL <= R1L));
				Assert.True(R1L <= TmpL);
				Assert.True((R1L == TmpL) != (R1L < TmpL));
				Assert.True((TmpL == R1L) || !(R1L >= TmpL));
				Assert.True(!(TmpL < R1L));
				Assert.True(!(R1L > TmpL));
			}
			uint160 TmpS;
			for(int i = 0 ; i < 160 ; ++i)
			{
				TmpS = OneS << i;
				Assert.True(TmpS >= ZeroS && TmpS > ZeroS && ZeroS < TmpS && ZeroS <= TmpS);
				Assert.True(TmpS >= 0 && TmpS > 0 && 0 < TmpS && 0 <= TmpS);
				TmpS |= R1S;
				Assert.True(TmpS >= R1S);
				Assert.True((TmpS == R1S) != (TmpS > R1S));
				Assert.True((TmpS == R1S) || !(TmpS <= R1S));
				Assert.True(R1S <= TmpS);
				Assert.True((R1S == TmpS) != (R1S < TmpS));
				Assert.True((TmpS == R1S) || !(R1S >= TmpS));
				Assert.True(!(TmpS < R1S));
				Assert.True(!(R1S > TmpS));
			}
		}

		[Fact]
		[Trait("Core", "Core")]
		public void bitwiseOperators()
		{
			CHECKBITWISEOPERATOR("R1", "R2", '|');
			CHECKBITWISEOPERATOR("R1", "R2", '^');
			CHECKBITWISEOPERATOR("R1", "R2", '&');
			CHECKBITWISEOPERATOR("R1", "Zero", '|');
			CHECKBITWISEOPERATOR("R1", "Zero", '^');
			CHECKBITWISEOPERATOR("R1", "Zero", '&');
			CHECKBITWISEOPERATOR("R1", "Max", '|');
			CHECKBITWISEOPERATOR("R1", "Max", '^');
			CHECKBITWISEOPERATOR("R1", "Max", '&');
			CHECKBITWISEOPERATOR("Zero", "R1", '|');
			CHECKBITWISEOPERATOR("Zero", "R1", '^');
			CHECKBITWISEOPERATOR("Zero", "R1", '&');
			CHECKBITWISEOPERATOR("Max", "R1", '|');
			CHECKBITWISEOPERATOR("Max", "R1", '^');
			CHECKBITWISEOPERATOR("Max", "R1", '&');

			//Do not test in C#, the assigment is automatically transformed into bitwise previously tested
			//CHECKASSIGNMENTOPERATOR("R1", "R2", '|');
			//CHECKASSIGNMENTOPERATOR("R1", "R2", '^');
			//CHECKASSIGNMENTOPERATOR("R1", "R2", '&');
			//CHECKASSIGNMENTOPERATOR("R1", "Zero", '|');
			//CHECKASSIGNMENTOPERATOR("R1", "Zero", '^');
			//CHECKASSIGNMENTOPERATOR("R1", "Zero", '&');
			//CHECKASSIGNMENTOPERATOR("R1", "Max", '|');
			//CHECKASSIGNMENTOPERATOR("R1", "Max", '^');
			//CHECKASSIGNMENTOPERATOR("R1", "Max", '&');
			//CHECKASSIGNMENTOPERATOR("Zero", "R1", '|');
			//CHECKASSIGNMENTOPERATOR("Zero", "R1", '^');
			//CHECKASSIGNMENTOPERATOR("Zero", "R1", '&');
			//CHECKASSIGNMENTOPERATOR("Max", "R1", '|');
			//CHECKASSIGNMENTOPERATOR("Max", "R1", '^');
			//CHECKASSIGNMENTOPERATOR("Max", "R1", '&');

			uint256 TmpL = 0UL;
			uint160 TmpS = 0UL;
			ulong Tmp64 = 0xe1db685c9a0b47a2UL;
			TmpL = R1L;
			TmpL |= Tmp64;
			Assert.True(TmpL == (R1L | new uint256(Tmp64)));
			TmpS = R1S;
			TmpS |= Tmp64;
			Assert.True(TmpS == (R1S | new uint160(Tmp64)));
			TmpL = R1L;
			TmpL |= 0;
			Assert.True(TmpL == R1L);
			TmpS = R1S;
			TmpS |= 0;
			Assert.True(TmpS == R1S);
			TmpL ^= 0;
			Assert.True(TmpL == R1L);
			TmpS ^= 0;
			Assert.True(TmpS == R1S);
			TmpL ^= Tmp64;
			Assert.True(TmpL == (R1L ^ new uint256(Tmp64)));
			TmpS ^= Tmp64;
			Assert.True(TmpS == (R1S ^ new uint160(Tmp64)));
		}


		private void CHECKBITWISEOPERATOR(string a, string b, char op)
		{
			var map = new object[][]{
								new object[]{'|',"BitwiseOr",new Func<byte,byte,byte>((aa,bb)=>(byte)(aa | bb))},
								new object[]{'^',"ExclusiveOr",new Func<byte,byte,byte>((aa,bb)=>(byte)(aa ^ bb))},
								new object[]{'&',"BitwiseAnd",new Func<byte,byte,byte>((aa,bb)=>(byte)(aa & bb))},
							}.ToDictionary(k => (char)k[0], k => new
							{
								Name = (string)k[1],
								ByteFunc = (Func<byte, byte, byte>)k[2]
							});


			var aL = (uint256)this.GetType().GetField(a + "L", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
			var aS = (uint160)this.GetType().GetField(a + "S", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
			var aArray = (byte[])this.GetType().GetField(a + "Array", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

			var bL = (uint256)this.GetType().GetField(b + "L", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
			var bS = (uint160)this.GetType().GetField(b + "S", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);
			var bArray = (byte[])this.GetType().GetField(b + "Array", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(this);

			var mS = typeof(uint160).GetMethod("op_" + map[op].Name);
			var mL = typeof(uint256).GetMethod("op_" + map[op].Name);


			byte[] arr = new byte[32];
			for(int i = 0 ; i < arr.Length ; i++)
			{
				arr[i] = map[op].ByteFunc(aArray[i], bArray[i]);
			}

			var actual = (uint256)mL.Invoke(null, new object[] { aL, bL });
			var expected = new uint256(arr);
			Assert.True(expected == actual);

			arr = new byte[20];
			for(int i = 0 ; i < arr.Length ; i++)
			{
				arr[i] = map[op].ByteFunc(aArray[i], bArray[i]);
			}

			var actual1 = (uint160)mS.Invoke(null, new object[] { aS, bS });
			var expected1 = new uint160(arr);
			Assert.True(expected1 == actual1);
		}

		[Fact]
		[Trait("Core", "Core")]
		public void basics()
		{
			//Assert.True(new uint256("ababdc10a3").ToString().EndsWith("0" + "ababdc10a3"));

			Assert.True(1 == 0 + 1);
			// constructor uint256(vector<char>):
			Assert.True(R1L.ToString() == ArrayToString(R1Array));
			Assert.True(R1S.ToString() == ArrayToString(R1Array.Take(20).ToArray()));
			Assert.True(R2L.ToString() == ArrayToString(R2Array));
			Assert.True(R2S.ToString() == ArrayToString(R2Array.Take(20).ToArray()));
			Assert.True(ZeroL.ToString() == ArrayToString(ZeroArray));
			Assert.True(ZeroS.ToString() == ArrayToString(ZeroArray.Take(20).ToArray()));
			Assert.True(OneL.ToString() == ArrayToString(OneArray));
			Assert.True(OneS.ToString() == ArrayToString(OneArray.Take(20).ToArray()));
			Assert.True(MaxL.ToString() == ArrayToString(MaxArray));
			Assert.True(MaxS.ToString() == ArrayToString(MaxArray.Take(20).ToArray()));
			Assert.True(OneL.ToString() != ArrayToString(ZeroArray));
			Assert.True(OneS.ToString() != ArrayToString(ZeroArray.Take(20).ToArray()));


			Assert.True(R1L != R2L && R1S != R2S);
			Assert.True(ZeroL != OneL && ZeroS != OneS);
			Assert.True(OneL != ZeroL && OneS != ZeroS);
			Assert.True(MaxL != ZeroL && MaxS != ZeroS);
			Assert.True(~MaxL == ZeroL && ~MaxS == ZeroS);
			Assert.True(((R1L ^ R2L) ^ R1L) == R2L);
			Assert.True(((R1S ^ R2S) ^ R1S) == R2S);

			ulong Tmp64 = 0xc4dab720d9c7acaaUL;
			for(int i = 0 ; i < 256 ; ++i)
			{
				Assert.True(ZeroL != (OneL << i));
				Assert.True((OneL << i) != ZeroL);
				Assert.True(R1L != (R1L ^ (OneL << i)));
				Assert.True(((new uint256(Tmp64) ^ (OneL << i)) != Tmp64));
			}

			Assert.True(ZeroL == (OneL << 256));

			for(int i = 0 ; i < 160 ; ++i)
			{
				Assert.True(ZeroS != (OneS << i));
				Assert.True((OneS << i) != ZeroS);
				Assert.True(R1S != (R1S ^ (OneS << i)));
				Assert.True(((new uint160(Tmp64) ^ (OneS << i)) != Tmp64));
			}
			Assert.True(ZeroS == (OneS << 256));

			Assert.True(new uint256("0x" + R1L.ToString()) == R1L);
			Assert.True(new uint256("0x" + R2L.ToString()) == R2L);
			Assert.True(new uint256("0x" + ZeroL.ToString()) == ZeroL);
			Assert.True(new uint256("0x" + OneL.ToString()) == OneL);
			Assert.True(new uint256("0x" + MaxL.ToString()) == MaxL);
			Assert.True(new uint256(R1L.ToString()) == R1L);
			Assert.True(new uint256("   0x" + R1L.ToString() + "   ") == R1L);
			Assert.True(new uint256("") == ZeroL);
			Assert.True(R1L == new uint256(R1ArrayHex));
			Assert.True(new uint256(R1L) == R1L);
			Assert.True((new uint256(R1L ^ R2L) ^ R2L) == R1L);
			Assert.True(new uint256(ZeroL) == ZeroL);
			Assert.True(new uint256(OneL) == OneL);


			Assert.True(new uint160("0x" + R1S.ToString()) == R1S);
			Assert.True(new uint160("0x" + R2S.ToString()) == R2S);
			Assert.True(new uint160("0x" + ZeroS.ToString()) == ZeroS);
			Assert.True(new uint160("0x" + OneS.ToString()) == OneS);
			Assert.True(new uint160("0x" + MaxS.ToString()) == MaxS);
			Assert.True(new uint160(R1S.ToString()) == R1S);
			Assert.True(new uint160("   0x" + R1S.ToString() + "   ") == R1S);
			Assert.True(new uint160("") == ZeroS);
			Assert.True(R1S == new uint160(R1ArrayHex));

			Assert.True(new uint160(R1S) == R1S);
			Assert.True((new uint160(R1S ^ R2S) ^ R2S) == R1S);
			Assert.True(new uint160(ZeroS) == ZeroS);
			Assert.True(new uint160(OneS) == OneS);

			// uint64_t constructor
			Assert.True((R1L & new uint256("0xffffffffffffffff")) == new uint256(R1LLow64));
			Assert.True(ZeroL == new uint256(0));
			Assert.True(OneL == new uint256(1));
			Assert.True(new uint256("0xffffffffffffffff") == new uint256(0xffffffffffffffffUL));
			Assert.True((R1S & new uint160("0xffffffffffffffff")) == new uint160(R1LLow64));
			Assert.True(ZeroS == new uint160(0));
			Assert.True(OneS == new uint160(1));
			Assert.True(new uint160("0xffffffffffffffff") == new uint160(0xffffffffffffffffUL));

			// Assignment (from base_uint)
			uint256 tmpL = ~ZeroL;
			Assert.True(tmpL == ~ZeroL);
			tmpL = ~OneL;
			Assert.True(tmpL == ~OneL);
			tmpL = ~R1L;
			Assert.True(tmpL == ~R1L);
			tmpL = ~R2L;
			Assert.True(tmpL == ~R2L);
			tmpL = ~MaxL;
			Assert.True(tmpL == ~MaxL);
			uint160 tmpS = ~ZeroS;
			Assert.True(tmpS == ~ZeroS);
			tmpS = ~OneS;
			Assert.True(tmpS == ~OneS);
			tmpS = ~R1S;
			Assert.True(tmpS == ~R1S);
			tmpS = ~R2S;
			Assert.True(tmpS == ~R2S);
			tmpS = ~MaxS;
			Assert.True(tmpS == ~MaxS);

			// Wrong length must crash, probably a bug
			Assert.Throws<FormatException>(() => Assert.True(new uint256(OneArray.Take(31).ToArray()) == 0));
			Assert.Throws<FormatException>(() => new uint256(OneArray.Take(20).ToArray()) == 0);
			Assert.Throws<FormatException>(() => new uint160(OneArray.Take(32).ToArray()) == 0);
			Assert.Throws<FormatException>(() => new uint160(OneArray.Take(19).ToArray()) == 0);
		}
	}
}
