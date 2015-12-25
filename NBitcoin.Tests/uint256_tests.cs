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
		[Fact]
		public void uitn245Tests()
		{
			var v = new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff");
			var v2 = new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff");
			var vless = new uint256("00000000fffffffffffffffffffffffffffffffffffffffffffffffffffffffe");
			var vplus = new uint256("00000001ffffffffffffffffffffffffffffffffffffffffffffffffffffffff");

			Assert.Equal("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff", v.ToString());
			Assert.Equal(new uint256("00000000ffffffffffffffffffffffffffffffffffffffffffffffffffffffff"), v);
			Assert.True(v < vplus);
			Assert.True(v > vless);

			Assert.True(v >= v2);
			Assert.True(v <= v2);
			Assert.False(v < v2);
			Assert.False(v > v2);

			Assert.True(v.ToBytes()[0] == 0xFF);
			Assert.True(v.ToBytes(false)[0] == 0x00);

			AssertEquals(v, new uint256(v.ToBytes()));
			AssertEquals(v, new uint256(v.ToBytes(false), false));
		}

		private void AssertEquals(uint256 a, uint256 b)
		{
			Assert.Equal(a, b);
			Assert.Equal(a.GetHashCode(), b.GetHashCode());
			Assert.True(a == b);
		}
	}
}
