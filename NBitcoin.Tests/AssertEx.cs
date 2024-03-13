using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests
{
	class AssertEx
	{
		[DebuggerHidden]
		internal static void Error(string msg)
		{
			Assert.False(true, msg);
		}
		[DebuggerHidden]
		internal static void Equal<T>(T actual, T expected)
		{
			Assert.Equal(expected, actual);
		}
		[DebuggerHidden]
		internal static void CollectionEquals<T>(T[] actual, T[] expected)
		{
			if (actual.Length != expected.Length)
				Assert.False(true, "Actual.Length(" + actual.Length + ") != Expected.Length(" + expected.Length + ")");

			for (int i = 0; i < actual.Length; i++)
			{
				if (!Object.Equals(actual[i], expected[i]))
					Assert.False(true, "Actual[" + i + "](" + actual[i] + ") != Expected[" + i + "](" + expected[i] + ")");
			}
		}
		[DebuggerHidden]
		internal static void StackEquals(ContextStack<byte[]> stack1, ContextStack<byte[]> stack2)
		{
			var hash1 = stack1.Select(o => Hashes.DoubleSHA256(o)).ToArray();
			var hash2 = stack2.Select(o => Hashes.DoubleSHA256(o)).ToArray();
			AssertEx.CollectionEquals(hash1, hash2);
		}
#if HAS_SPAN
		[DebuggerHidden]
		internal static void EqualBytes(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
		{
			var aa = DataEncoders.Encoders.Hex.EncodeData(expected);
			var bb = DataEncoders.Encoders.Hex.EncodeData(actual);
			Assert.Equal(aa, bb);
		}
		[DebuggerHidden]
		internal static void EqualBytes(string expected, ReadOnlySpan<byte> actual)
		{
			var bb = DataEncoders.Encoders.Hex.EncodeData(actual);
			Assert.Equal(expected.ToLowerInvariant(), bb);
		}
		[DebuggerHidden]
		internal static void EqualBytes(ReadOnlySpan<byte> expected, string actual)
		{
			var aa = DataEncoders.Encoders.Hex.EncodeData(expected);
			Assert.Equal(aa, actual.ToLowerInvariant());
		}
#endif
	}
}
