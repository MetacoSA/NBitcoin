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
			if(actual.Length != expected.Length)
				Assert.False(true, "Actual.Length(" + actual.Length + ") != Expected.Length(" + expected.Length + ")");

			for(int i = 0 ; i < actual.Length ; i++)
			{
				if(!Object.Equals(actual[i], expected[i]))
					Assert.False(true, "Actual[" + i + "](" + actual[i] + ") != Expected[" + i + "](" + expected[i] + ")");
			}
		}
	}
}
