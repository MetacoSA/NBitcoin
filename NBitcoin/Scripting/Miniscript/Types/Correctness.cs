using System;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Scripting.Miniscript.Types
{
	public class Correctness : IProperty<Correctness>
	{
		public Base Base;
		public Input Input;
		public bool DisSatisfiable;
		/// <summary>
		/// Whether the fragment's "nonzero' output on satisfaction is
		/// always the constant 1.
		/// </summary>
		public bool Unit;

		public bool IsSubtype(Correctness other) =>
			(this.Base == other.Base) &&
			(this.Input.IsSubtype(other.Input)) &&
			(!(!(this.DisSatisfiable) && other.DisSatisfiable)) &&
			(!(!(this.Unit) && other.Unit));

		public void SanityChecks()
		{
			throw new NotImplementedException();
		}

		public Correctness FromTrue()
		{
			throw new NotImplementedException();
		}

		public Correctness FromFalse()
		{
			throw new NotImplementedException();
		}

		public Correctness FromPk()
		{
			throw new NotImplementedException();
		}

		public Correctness FromPkH()
		{
			throw new NotImplementedException();
		}

		public Correctness FromMulti(int k, int pkLength)
		{
			throw new NotImplementedException();
		}

		public Correctness FromAfter(uint time)
		{
			throw new NotImplementedException();
		}

		public Correctness FromOlder(uint time)
		{
			throw new NotImplementedException();
		}

		public Correctness FromHash()
		{
			throw new NotImplementedException();
		}

		public Correctness FromSha256()
		{
			throw new NotImplementedException();
		}

		public Correctness FromHash256()
		{
			throw new NotImplementedException();
		}

		public Correctness FromRipemd160()
		{
			throw new NotImplementedException();
		}

		public Correctness FromHash160()
		{
			throw new NotImplementedException();
		}

		public Correctness CastAlt()
		{
			throw new NotImplementedException();
		}

		public Correctness CastSwap()
		{
			throw new NotImplementedException();
		}

		public Correctness CastCheck()
		{
			throw new NotImplementedException();
		}

		public Correctness CastDupIf()
		{
			throw new NotImplementedException();
		}

		public Correctness CastVerify()
		{
			throw new NotImplementedException();
		}

		public Correctness CastNonZero()
		{
			throw new NotImplementedException();
		}

		public Correctness CastZeroNotEqual()
		{
			throw new NotImplementedException();
		}

		public Correctness CastTrue()
		{
			throw new NotImplementedException();
		}

		public Correctness CastOrIFalse()
		{
			throw new NotImplementedException();
		}

		public Correctness CastUnLikely()
		{
			throw new NotImplementedException();
		}

		public Correctness CastLikely()
		{
			throw new NotImplementedException();
		}

		public Correctness AndB(Correctness left, Correctness right)
		{
			throw new NotImplementedException();
		}

		public Correctness AndV(Correctness left, Correctness right)
		{
			throw new NotImplementedException();
		}

		public Correctness AndN(Correctness left, Correctness right)
		{
			throw new NotImplementedException();
		}

		public Correctness OrB(Correctness left, Correctness right)
		{
			throw new NotImplementedException();
		}

		public Correctness OrD(Correctness left, Correctness right)
		{
			throw new NotImplementedException();
		}

		public Correctness OrC(Correctness left, Correctness right)
		{
			throw new NotImplementedException();
		}

		public Correctness OrI(Correctness left, Correctness right)
		{
			throw new NotImplementedException();
		}

		public Correctness AndOr(Correctness a, Correctness b, Correctness c)
		{
			throw new NotImplementedException();
		}

		public Correctness Threshold(int k, int n, Func<uint, Correctness> subCk)
		{
			throw new NotImplementedException();
		}
	}
}
