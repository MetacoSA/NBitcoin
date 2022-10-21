#nullable enable
using System;

namespace NBitcoin.Protocol
{
	[Payload("feefilter")]
	public class FeeFilterPayload : Payload
	{
		public FeeFilterPayload()
		{
			_feeRate = FeeRate.Zero;
		}

		private FeeRate _feeRate;
		public FeeRate FeeRate
		{
			get
			{
				return _feeRate;
			}
			set
			{
				if (value is null)
					throw new ArgumentNullException(nameof(value));
				_feeRate = value;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			if (stream.Serializing)
			{
				stream.ReadWrite(_feeRate.FeePerK.Satoshi);
			}
			else
			{
				long v = 0;
				stream.ReadWrite(ref v);
				_feeRate = new FeeRate(Money.Satoshis(v), 1000);
			}
		}

		public override string ToString()
		{
			return base.ToString() + " : " + FeeRate;
		}
	}
}
