using System;

namespace NBitcoin.Scripting.Miniscript.Policy
{
	public class PolicyException : Exception
	{

		public PolicyException(string msg) : base(msg) {}
		public PolicyException(string msg, Exception inner) : base(msg, inner) {}
		public static PolicyException NonBinaryArgAnd =
			new PolicyException("And policy fragment must take 2 arguments");
		public static PolicyException NonBinaryArgOr =
			new PolicyException("Or policy fragment must take 2 arguments");

		public static PolicyException IncorrectThresh =
			new PolicyException("Threshold k must be greater than 0 and less than n");

		public static PolicyException ZeroTime =
			new PolicyException("Time must be greater than 0;");
		public static PolicyException TimeTooFar =
			new PolicyException("Relative/Absolute time must be less than 2^31");
	}
}
