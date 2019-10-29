using System;

namespace NBitcoin.Scripting.Miniscript.Types
{
	/// <summary>
	///  Whether a fragment is OK to be used in non-segwit scripts
	/// </summary>
	internal enum LegacySafe
	{
		LegacySafe,
		SegwitOnly
	}
	internal class ExtData : IProperty<ExtData>
	{
		internal LegacySafe LegacySafe;
		internal UInt64 PkCost;
		internal bool HasVerifyForm;
		internal UInt64 OpsCountStatic;
		internal UInt64?  OpsCountSat;
		internal UInt64? OpsCountNSat;

		public void SanityChecks()
		{
			throw new NotImplementedException();
		}

		public ExtData FromTrue()
		{
			throw new NotImplementedException();
		}

		public ExtData FromFalse()
		{
			throw new NotImplementedException();
		}

		public ExtData FromPk()
		{
			throw new NotImplementedException();
		}

		public ExtData FromPkH()
		{
			throw new NotImplementedException();
		}

		public ExtData FromMulti(int k, int pkLength)
		{
			throw new NotImplementedException();
		}

		public ExtData FromAfter(uint time)
		{
			throw new NotImplementedException();
		}

		public ExtData FromOlder(uint time)
		{
			throw new NotImplementedException();
		}

		public ExtData FromHash()
		{
			throw new NotImplementedException();
		}

		public ExtData FromSha256()
		{
			throw new NotImplementedException();
		}

		public ExtData FromHash256()
		{
			throw new NotImplementedException();
		}

		public ExtData FromRipemd160()
		{
			throw new NotImplementedException();
		}

		public ExtData FromHash160()
		{
			throw new NotImplementedException();
		}

		public ExtData CastAlt()
		{
			throw new NotImplementedException();
		}

		public ExtData CastSwap()
		{
			throw new NotImplementedException();
		}

		public ExtData CastCheck()
		{
			throw new NotImplementedException();
		}

		public ExtData CastDupIf()
		{
			throw new NotImplementedException();
		}

		public ExtData CastVerify()
		{
			throw new NotImplementedException();
		}

		public ExtData CastNonZero()
		{
			throw new NotImplementedException();
		}

		public ExtData CastZeroNotEqual()
		{
			throw new NotImplementedException();
		}

		public ExtData CastTrue()
		{
			throw new NotImplementedException();
		}

		public ExtData CastOrIFalse()
		{
			throw new NotImplementedException();
		}

		public ExtData CastUnLikely()
		{
			throw new NotImplementedException();
		}

		public ExtData CastLikely()
		{
			throw new NotImplementedException();
		}

		public ExtData AndB(ExtData left, ExtData right)
		{
			throw new NotImplementedException();
		}

		public ExtData AndV(ExtData left, ExtData right)
		{
			throw new NotImplementedException();
		}

		public ExtData AndN(ExtData left, ExtData right)
		{
			throw new NotImplementedException();
		}

		public ExtData OrB(ExtData left, ExtData right)
		{
			throw new NotImplementedException();
		}

		public ExtData OrD(ExtData left, ExtData right)
		{
			throw new NotImplementedException();
		}

		public ExtData OrC(ExtData left, ExtData right)
		{
			throw new NotImplementedException();
		}

		public ExtData OrI(ExtData left, ExtData right)
		{
			throw new NotImplementedException();
		}

		public ExtData AndOr(ExtData a, ExtData b, ExtData c)
		{
			throw new NotImplementedException();
		}

		public ExtData Threshold(int k, int n, Func<uint, ExtData> subCk)
		{
			throw new NotImplementedException();
		}
	}
}
