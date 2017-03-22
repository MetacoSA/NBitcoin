using System;

namespace NBitcoin
{
	public enum BIP9Deployments : int
	{
		TestDummy,
		CSV,
		Segwit
	}

	public class BIP9DeploymentsParameters
	{
		public BIP9DeploymentsParameters(int bit, DateTimeOffset startTime, DateTimeOffset timeout)
		{
			Bit = bit;
			StartTime = startTime;
			Timeout = timeout;
		}

		public BIP9DeploymentsParameters(int bit, long startTime, long timeout)
			: this(bit, (DateTimeOffset) Utils.UnixTimeToDateTime(startTime), Utils.UnixTimeToDateTime(timeout))
		{

		}
		public int Bit
		{
			get;
			private set;
		}
		public DateTimeOffset StartTime
		{
			get;
			private set;
		}
		public DateTimeOffset Timeout
		{
			get;
			private set;
		}
	}
}
