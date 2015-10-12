using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class FeeRate
	{
		private readonly Money _FeePerK;
		/// <summary>
		/// Fee per KB
		/// </summary>
		public Money FeePerK
		{
			get
			{
				return _FeePerK;
			}
		}
		public FeeRate(Money feePerK)
		{
			if(feePerK == null)
				throw new ArgumentNullException("feePerK");
			_FeePerK = feePerK;
		}

		public FeeRate(Money feeFaid, int size)
		{
			if(feeFaid == null)
				throw new ArgumentNullException("feeFaid");
			if(size > 0)
				_FeePerK = feeFaid * 1000 / size;
			else
				_FeePerK = 0;
		}

		/// <summary>
		/// Get fee for the size
		/// </summary>
		/// <param name="size">Size in bytes</param>
		/// <returns></returns>
		public Money GetFee(int size)
		{
			Money nFee = _FeePerK.Satoshi * size / 1000;
			if(nFee == 0 && _FeePerK.Satoshi > 0)
				nFee = _FeePerK.Satoshi;
			return nFee;
		}
		public Money GetFee(Transaction tx)
		{
			return GetFee(tx.GetSerializedSize());
		}

		public override string ToString()
		{
			return String.Format("{0} BTC/kB", _FeePerK.ToString());
		}
	}
}
