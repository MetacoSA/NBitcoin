using NBitcoin.Crypto.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NBitcoin.Tests.Crypto
{
    public class BitMathTests
    {
        [Fact]
        [Trait("Crypto", "Crypto")]
        public void BitMath_Should_Return_Is_Postive_Power_Of_2()
        {
            Boolean actual = BitMath.IsPositivePowerOf2(1);
            const Boolean expected = true;

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Crypto", "Crypto")]
        public void BitMath_Should_Return_Is_Not_Postive_Power_Of_2()
        {
            Boolean actual = BitMath.IsPositivePowerOf2(0);
            const Boolean expected = false;

            Assert.Equal(expected, actual);

            actual = BitMath.IsPositivePowerOf2(-1);
            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Crypto", "Crypto")]
        public void BitMath_Should_Return_Count_Of_31()
        {
            //Integer as 32 digit binary number
            Int32 actual = BitMath.CountLeadingZeros(1);
            const Int32 expected = 31;

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Crypto", "Crypto")]
        public void BitMath_Should_Return_Count_Of_28()
        {
            //Integer as 32 digit binary number
            //15 = 1111 in binary, so 32 - 4 = 28
            Int32 actual = BitMath.CountLeadingZeros(15);
            const Int32 expected = 28;

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Crypto", "Crypto")]
        public void BitMath_Shift_Left_Should_Return_3()
        {
            Byte data = 1;
            
            Byte actual = BitMath.ShiftLeft(data, 3);
            const Byte expected = 8; //Math.Pow(2, 3);

            Assert.Equal(expected, actual);
        }

        [Fact]
        [Trait("Crypto", "Crypto")]
        public void BitMath_Should_Swap()
        {
            Int32 left = 1;
            Int32 right = 2;

            BitMath.Swap<Int32>(ref left, ref right);
            const Int32 expectedLeft = 2;
            const Int32 expectedRight = 1;

            Assert.Equal(expectedLeft, left);
            Assert.Equal(expectedRight, right);
        }
    }

    public class CheckTests
    {
        [Fact]
        [Trait("Crypto", "Crypto")]
        public void Check_Should_Throw_Null_Exception()
        {
            Assert.Throws<ArgumentNullException>(() => Check.Null<String>("test", null));
        }
    }
}
