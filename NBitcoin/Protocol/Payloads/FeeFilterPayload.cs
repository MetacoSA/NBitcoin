namespace NBitcoin.Protocol
{
	[Payload("feefilter")]
	public class FeeFilterPayload : Payload
	{
		public FeeFilterPayload()
		{
			_feeRate = 0;
		}

		private ulong _feeRate;
		public FeeRate FeeRate
		{
			get
			{
				return new FeeRate(new Money(_feeRate));
			}
			set
			{
				_feeRate = (ulong)value.FeePerK.Satoshi;
			}
		}

		public override void ReadWriteCore(BitcoinStream stream)
		{
			stream.ReadWrite(ref _feeRate);
		}

		public override string ToString()
		{
			return base.ToString() + " : " + FeeRate;
		}
	}
}
