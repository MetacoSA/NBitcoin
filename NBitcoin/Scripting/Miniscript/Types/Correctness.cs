using System;
using System.Diagnostics;

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

		public Correctness() {}

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

		public override void SanityChecks()
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

		public override Correctness FromTrue()
			=> new Correctness(Base.B, Input.Zero, false, true);

		public override Correctness FromFalse()
			=> new Correctness(Base.B, Input.Zero, true, false);

		public override Correctness FromPk()
			=> new Correctness(Base.K, Input.OneNonZero, true, true);

		public override Correctness FromPkH()
			=> new Correctness(Base.K, Input.AnyNonZero, true, true);

		public override Correctness FromMulti(int k, int pkLength)
			=> new Correctness(Base.B, Input.AnyNonZero, true, true);

		public override Correctness FromHash()
			=> new Correctness(Base.B, Input.OneNonZero, true, true);

		public override Correctness FromTime(uint time)
			=> new Correctness(Base.B, Input.Zero, false, false);

		public override Correctness CastAlt()
			=> new Correctness(
				(Base == Base.B ? Base.W : throw FragmentPropertyException.ChildBase1(Base)),
				Input.Any,
				DisSatisfiable,
				Unit
		);

		public override Correctness CastSwap() =>
			new Correctness(
				(Base == Base.B ? Base.W : throw FragmentPropertyException.ChildBase1(Base)),
				(Input == Input.One || Input == Input.OneNonZero ? Input.Any : throw FragmentPropertyException.ChildBase1(Base)),
				DisSatisfiable,
				Unit
				);

		public override Correctness CastCheck() =>
			new Correctness(
				(Base == Base.K ? Base.B : throw FragmentPropertyException.ChildBase1(Base)),
				Input,
				DisSatisfiable,
				true
			);

		public override Correctness CastDupIf() =>
			new Correctness(
				(Base == Base.V ? Base.B : throw FragmentPropertyException.ChildBase1(Base)),
				(Input == Input.Zero ? Input.OneNonZero : throw FragmentPropertyException.NonZeroDupIf()),
				true,
				true
				);

		public override Correctness CastVerify() =>
			new Correctness(
				(Base == Base.B ? Base.V : throw FragmentPropertyException.ChildBase1(Base)),
				Input,
				false,false
				);

		public override Correctness CastNonZero()
		{
			if (Input != Input.OneNonZero && Input != Input.AnyNonZero)
				throw FragmentPropertyException.NonZeroZero();
			return new Correctness(
				(Base == Base.B ? Base.B : throw FragmentPropertyException.ChildBase1(Base)),
				Input,
				true,
				Unit
				);
		}

		public override Correctness CastZeroNotEqual() =>
			new Correctness(
				(Base == Base.B ? Base.B : throw FragmentPropertyException.ChildBase1(Base)),
				Input,
				DisSatisfiable,
				true
				);

		public override Correctness CastTrue() =>
			new Correctness(
				(Base == Base.V ? Base.B : throw FragmentPropertyException.ChildBase1(Base)),
				Input,
				false,
				true
			);

		public override Correctness CastOrIFalse() =>
			new Correctness(
				(Base == Base.B ? Base.B : throw FragmentPropertyException.ChildBase1(Base)),
				(Input == Input.Zero ? Input.One : Input.Any),
				true,
				Unit
				);

		public override Correctness AndB(Correctness l, Correctness r) =>
			new Correctness(
				((l.Base == Base.B && r.Base == Base.W) ? Base.B : throw FragmentPropertyException.ChildBase2(l.Base, r.Base) ),

				(l.Input == Input.Zero && r.Input == Input.Zero) ? Input.Zero :
				((l.Input == Input.Zero && r.Input == Input.One)
					|| l.Input == Input.One || r.Input == Input.Zero) ? Input.One :
				(l.Input == Input.Zero && r.Input == Input.OneNonZero)
					|| (l.Input == Input.OneNonZero && r.Input == Input.Zero) ? Input.OneNonZero :
				((l.Input == Input.AnyNonZero) || (l.Input == Input.Zero && r.Input == Input.AnyNonZero)) ? Input.AnyNonZero :
				(Input.Any),

				(l.DisSatisfiable && r.DisSatisfiable),
				true
				);

		public override Correctness AndV(Correctness l, Correctness r)
		{
			return new Correctness(
				(l.Base == Base.V && r.Base == Base.B) ? Base.B :
				(l.Base == Base.V && r.Base == Base.K) ? Base.K :
				(l.Base == Base.V && r.Base == Base.V) ? Base.V : throw FragmentPropertyException.ChildBase2(l.Base, r.Base),

				(l.Input == Input.Zero && r.Input == Input.Zero) ? Input.Zero :
				(l.Input == Input.Zero && r.Input == Input.One) ||
					(l.Input == Input.One && r.Input == Input.Zero) ? Input.One :
				(l.Input == Input.Zero && r.Input == Input.OneNonZero) ||
					(l.Input == Input.OneNonZero && r.Input == Input.Zero) ? Input.OneNonZero :
				(l.Input == Input.OneNonZero) ||
					(l.Input == Input.AnyNonZero) ||
					(l.Input == Input.Zero && r.Input == Input.AnyNonZero) ? Input.AnyNonZero :
				Input.Any,
				false,
				r.Unit
				);
		}

		public override Correctness OrB(Correctness l, Correctness r)
		{
			if (!l.DisSatisfiable)
				throw FragmentPropertyException.LeftNotDissatisfiable();
			if (!r.DisSatisfiable)
				throw FragmentPropertyException.RightNotDissatisfiable();
			return new Correctness(
				(l.Base == Base.B && r.Base == Base.W) ? Base.B : throw FragmentPropertyException.ChildBase2(l.Base, r.Base),
				(l.Input == Miniscript.Input.Zero && r.Input == Miniscript.Input.Zero) ? Miniscript.Input.Zero :
				(l.Input == Miniscript.Input.Zero && r.Input == Miniscript.Input.One) ||
					(l.Input == Miniscript.Input.One && r.Input == Miniscript.Input.Zero) ||
					(l.Input == Miniscript.Input.Zero && r.Input == Miniscript.Input.OneNonZero) ||
					(l.Input == Miniscript.Input.OneNonZero && r.Input == Miniscript.Input.Zero) ? Miniscript.Input.One :
				Miniscript.Input.Any,
				true,
				true
				);
		}

		public override Correctness OrD(Correctness l, Correctness r)
		{
			if (!l.DisSatisfiable)
				throw FragmentPropertyException.LeftNotDissatisfiable();
			if (!l.Unit)
				throw FragmentPropertyException.LeftNotUnit();
			return new Correctness(
				(l.Base == Base.B) ? Base.B : throw FragmentPropertyException.ChildBase2(l.Base, r.Base),
				(l.Input == Miniscript.Input.Zero && r.Input == Miniscript.Input.Zero) ? Miniscript.Input.Zero :
					(l.Input == Miniscript.Input.One && r.Input == Miniscript.Input.Zero) ||
					(l.Input == Miniscript.Input.OneNonZero && r.Input == Miniscript.Input.Zero) ? Miniscript.Input.One :
				Miniscript.Input.Any,
				r.DisSatisfiable,
				r.Unit
				);
		}

		public override Correctness OrC(Correctness l, Correctness r)
		{
			if (!l.DisSatisfiable)
				throw FragmentPropertyException.LeftNotDissatisfiable();
			if (!l.Unit)
				throw FragmentPropertyException.LeftNotUnit();

			return
				new Correctness(
					(l.Base == Miniscript.Base.B && r.Base == Miniscript.Base.V)
						? Miniscript.Base.V
						: throw FragmentPropertyException.ChildBase2(l.Base, r.Base),
					Miniscript.Input.Any,
					false,
					false
				);
		}

		public override Correctness OrI(Correctness l, Correctness r) =>
			new Correctness(
				(l.Base == Miniscript.Base.B && r.Base == Miniscript.Base.B) ? Miniscript.Base.B :
				(l.Base == Miniscript.Base.V && r.Base == Miniscript.Base.V) ? Miniscript.Base.V :
				(l.Base == Miniscript.Base.K && r.Base == Miniscript.Base.K) ? Miniscript.Base.K : throw FragmentPropertyException.ChildBase2(l.Base, r.Base),
				(l.Input == Miniscript.Input.Zero && r.Input == Miniscript.Input.Zero) ? Miniscript.Input.One : Miniscript.Input.Any,
				(l.DisSatisfiable || r.DisSatisfiable),
				l.Unit && r.Unit
			);

		public override Correctness AndOr(Correctness a, Correctness b, Correctness c)
		{
			if (!a.DisSatisfiable)
				throw FragmentPropertyException.LeftNotDissatisfiable();
			if (!a.Unit)
				throw FragmentPropertyException.LeftNotUnit();
			return new Correctness(
				(a.Base == Base.B && b.Base == Base.B && c.Base == Base.B) ? Miniscript.Base.B :
					(a.Base == Base.B && b.Base == Base.K && c.Base == Base.K) ? Base.K :
					(a.Base == Base.B && b.Base == Base.V && c.Base == Base.V) ? Base.V :
					throw FragmentPropertyException.ChildBase3(a.Base, b.Base, c.Base),
				(a.Input == Input.Zero && b.Input == Input.Zero && c.Input == Input.Zero) ? Input.Zero :
				(a.Input == Input.Zero && b.Input == Input.One && c.Input == Input.One) ||
					(a.Input  == Input.Zero && b.Input == Input.One && c.Input == Input.OneNonZero) ||
					(a.Input == Input.Zero && b.Input == Input.OneNonZero && c.Input == Input.One) ||
					(a.Input == Input.Zero && b.Input == Input.OneNonZero && c.Input == Input.OneNonZero) ||
					(a.Input == Input.One && b.Input == Input.Zero && c.Input == Input.Zero) ||
					(a.Input == Input.OneNonZero && b.Input == Input.Zero && c.Input == Input.Zero)
					? Input.One :
					Input.Any,
				c.DisSatisfiable,
				b.Unit && c.Unit
				);
		}

		public override Correctness Threshold(int k, int n, Func<int, Correctness> subCk)
		{
			var isN = k == n;
			for (int i = 0; i < n; i++)
			{
				var subType = subCk(i);
				if (i == 0)
				{
					isN &= subType.Input == Input.OneNonZero || subType.Input == Input.AnyNonZero;
					if (subType.Base != Base.B)
						throw FragmentPropertyException.ThresholdBase(0U, subType.Base);
				}
				else
				{
					if (subType.Base != Miniscript.Base.W)
						throw FragmentPropertyException.ThresholdBase((uint)n, subType.Base);
				}
				if (!subType.Unit)
					throw FragmentPropertyException.ThresholdNonUnit((uint)n);
				if (!subType.DisSatisfiable)
					throw FragmentPropertyException.ThresholdDissat((uint)n);
			}
			return new Correctness(
				Base.B,
				(isN ? Input.AnyNonZero : Miniscript.Input.Any),
				true,
				true
				);
		}
	}
}
