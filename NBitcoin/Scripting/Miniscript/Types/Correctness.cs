using System;
using System.Diagnostics;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Scripting.Miniscript.Types
{
	internal class Correctness : IProperty<Correctness>
	{
		public readonly Base Base;
		public readonly Input Input;
		public readonly bool DisSatisfiable;
		/// <summary>
		/// Whether the fragment's "nonzero' output on satisfaction is
		/// always the constant 1.
		/// </summary>
		public bool Unit;

		public Correctness(Base @base, Input input, bool disSatisfiable, bool unit)
		{
			Base = @base;
			Input = input;
			DisSatisfiable = disSatisfiable;
			Unit = unit;
		}

		public bool IsSubtype(Correctness other) =>
			(this.Base == other.Base) &&
			(this.Input.IsSubtype(other.Input)) &&
			(!(!(this.DisSatisfiable) && other.DisSatisfiable)) &&
			(!(!(this.Unit) && other.Unit));

		public void SanityChecks()
		{
			if (Base == Base.B) return;
			if (Base == Base.K) Debug.Assert(Unit);
			if (Base == Base.V)
			{
				Debug.Assert(Unit);
				Debug.Assert(DisSatisfiable);
			}

			if (Base == Base.W)
			{
				Debug.Assert(Input != Input.OneNonZero);
				Debug.Assert(Input != Input.AnyNonZero);
			}
		}

		public Correctness FromTrue()
			=> new Correctness(Base.B, Input.Zero, false, true);

		public Correctness FromFalse()
			=> new Correctness(Base.B, Input.Zero, true, false);

		public Correctness FromPk()
			=> new Correctness(Base.K, Input.OneNonZero, true, true);

		public Correctness FromPkH()
			=> new Correctness(Base.K, Input.AnyNonZero, true, true);

		public Correctness FromMulti(int k, int pkLength)
			=> new Correctness(Base.B, Input.AnyNonZero, true, true);

		public Correctness FromAfter(uint time)
			=> new Correctness(Base.B, Input.Zero, false, false);

		public Correctness FromOlder(uint time)
			=> new Correctness(Base.B, Input.Zero, false, false);

		public Correctness FromHash()
			=> new Correctness(Base.B, Input.OneNonZero, true, true);

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
