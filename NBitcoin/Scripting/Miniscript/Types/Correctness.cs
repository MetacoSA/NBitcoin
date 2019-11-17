using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NBitcoin.Scripting.Miniscript.Types
{
	[DebuggerDisplay("{" + nameof(DebugPrint) + "()}")]
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

		internal string DebugPrint()
			=> $"Input: {Input.DebugPrint()}; Base: {Base.ToString()}";

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
			=> new Correctness(Base.B, Input.Zero, true, true);

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

		public override bool TryCastAlt(out Correctness result, List<FragmentPropertyException> fragmentPropertyExceptions)
		{
			result = null;
			if (this.Base != Miniscript.Base.B)
			{
				fragmentPropertyExceptions.Add(FragmentPropertyException.ChildBase1(Base));
				return false;
			}

			result =
				new Correctness(
					Base.W,
					Input.Any,
					DisSatisfiable,
					Unit
				);
			return true;
		}

		public override bool TryCastSwap(out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (this.Base != Base.B)
			{
				error.Add(FragmentPropertyException.ChildBase1(Base));
				return false;
			}
			if (Input != Input.One && Input != Input.OneNonZero)
			{
				error.Add(FragmentPropertyException.SwapNoneOne());
				return false;
			}

			result = new Correctness(
				Base.W,
				Input.Any,
				DisSatisfiable,
				Unit
			);
			return true;
		}

		public override bool TryCastCheck(out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Base != Base.K)
			{
				error.Add(FragmentPropertyException.ChildBase1(Base));
				return false;
			}
			result = new Correctness(
				Miniscript.Base.B,
				Input,
				DisSatisfiable,
				true
			);
			return true;
		}

		public override bool TryCastDupIf(out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (this.Base != Base.V)
			{
				error.Add(FragmentPropertyException.ChildBase1(Base));
				return false;
			}

			if (this.Input != Input.Zero)
			{
				error.Add(FragmentPropertyException.NonZeroDupIf());
				return false;
			}
			result = new Correctness(
				Base.B,
				Input.OneNonZero,
				true,
				true
			);
			return true;
		}

		public override bool TryCastVerify(out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (this.Base != Base.B)
			{
				error.Add(FragmentPropertyException.ChildBase1(Base));
				return false;
			}
			result = new Correctness(
				Base.V,
				Input,
				false, false
			);
			return true;
		}

		public override bool TryCastNonZero(out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (this.Base != Base.B)
			{
				error.Add(FragmentPropertyException.ChildBase1(Base));
				return false;
			}
			if (Input != Input.OneNonZero && Input != Input.AnyNonZero)
			{
				error.Add(FragmentPropertyException.NonZeroZero());
				return false;
			}

			result =
				new Correctness(
					Base.B,
				Input,
				true,
				Unit
				);
			return true;
		}

		public override bool TryCastZeroNotEqual(out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (this.Base != Base.B)
			{
				error.Add(FragmentPropertyException.ChildBase1(Base));
				return false;
			}
			result = new Correctness(
				Base.B,
				Input,
				DisSatisfiable,
				true
			);
			return true;
		}

		public override bool TryCastTrue(out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (this.Base != Base.V)
			{
				error.Add(FragmentPropertyException.ChildBase1(Base));
				return false;
			}
			result =
			new Correctness(
				Base.B,
				Input,
				false,
				true
			);
			return true;
		}

		public override bool TryCastOrIFalse(out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (this.Base != Base.B)
			{
				error.Add(FragmentPropertyException.ChildBase1(Base));
				return false;
			}
			result =
			new Correctness(
				Base.B,
				(Input == Input.Zero ? Input.One : Input.Any),
				true,
				Unit
			);
			return true;
		}

		public override bool TryAndB(Correctness l, Correctness r, out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (l.Base != Base.B || r.Base != Base.W)
			{
				error.Add(FragmentPropertyException.ChildBase2(r.Base, l.Base));
				return false;
			}
			result = new Correctness(
				Miniscript.Base.B,
				(l.Input == Input.Zero && r.Input == Input.Zero)
					? Input.Zero
					:
					((l.Input == Input.Zero && r.Input == Input.One)
					 || l.Input == Input.One || r.Input == Input.Zero)
						? Input.One
						:
						(l.Input == Input.Zero && r.Input == Input.OneNonZero)
						|| (l.Input == Input.OneNonZero && r.Input == Input.Zero)
							? Input.OneNonZero
							:
							((l.Input == Input.AnyNonZero) || (l.Input == Input.Zero && r.Input == Input.AnyNonZero))
								?
								Input.AnyNonZero
								:
								(Input.Any),
				(l.DisSatisfiable && r.DisSatisfiable),
				true
			);
			return true;
		}

		public override bool TryAndV(Correctness l, Correctness r, out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			var newBase =
				(l.Base == Base.V && r.Base == Base.B) ? Base.B :
				(l.Base == Base.V && r.Base == Base.K) ? Base.K :
				(l.Base == Base.V && r.Base == Base.V) ? Base.V : Miniscript.Base.W;
			if (newBase == Base.W)
			{
				error.Add(FragmentPropertyException.ChildBase2(l.Base, r.Base));
				return false;
			}
			result =
				new Correctness(
					newBase,
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
			return true;
		}

		public override bool TryOrB(Correctness l, Correctness r, out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (!l.DisSatisfiable)
			{
				error.Add(FragmentPropertyException.LeftNotDissatisfiable());
				return false;
			}

			if (!r.DisSatisfiable)
			{
				error.Add(FragmentPropertyException.RightNotDissatisfiable());
				return false;
			}

			if (l.Base != Base.B || r.Base != Base.W)
			{
				error.Add(FragmentPropertyException.ChildBase2(l.Base, r.Base));
				return false;
			}

			result = new Correctness(
				Miniscript.Base.B,
				(l.Input == Miniscript.Input.Zero && r.Input == Miniscript.Input.Zero) ? Miniscript.Input.Zero :
				(l.Input == Miniscript.Input.Zero && r.Input == Miniscript.Input.One) ||
					(l.Input == Miniscript.Input.One && r.Input == Miniscript.Input.Zero) ||
					(l.Input == Miniscript.Input.Zero && r.Input == Miniscript.Input.OneNonZero) ||
					(l.Input == Miniscript.Input.OneNonZero && r.Input == Miniscript.Input.Zero) ? Miniscript.Input.One :
				Miniscript.Input.Any,
				true,
				true
				);
			return true;
		}

		public override bool TryOrD(Correctness l, Correctness r, out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (!l.DisSatisfiable)
			{
				error.Add(FragmentPropertyException.LeftNotDissatisfiable());
				return false;
			}

			if (!l.Unit)
			{
				error.Add(FragmentPropertyException.LeftNotUnit());
				return false;
			}

			if (l.Base != Base.B || r.Base != Base.B)
			{
				error.Add(FragmentPropertyException.ChildBase2(l.Base, r.Base));
				return false;
			}

			result = new Correctness(
				Miniscript.Base.B,
				(l.Input == Miniscript.Input.Zero && r.Input == Miniscript.Input.Zero) ? Miniscript.Input.Zero :
					(l.Input == Miniscript.Input.One && r.Input == Miniscript.Input.Zero) ||
					(l.Input == Miniscript.Input.OneNonZero && r.Input == Miniscript.Input.Zero) ? Miniscript.Input.One :
				Miniscript.Input.Any,
				r.DisSatisfiable,
				r.Unit
				);
			return true;
		}

		public override bool TryOrC(Correctness l, Correctness r, out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (!l.DisSatisfiable)
			{
				error.Add(FragmentPropertyException.LeftNotDissatisfiable());
				return false;
			}

			if (!l.Unit)
			{
				error.Add(FragmentPropertyException.LeftNotUnit());
				return false;
			}

			if (l.Base != Base.B || r.Base != Base.V)
			{
				error.Add(FragmentPropertyException.ChildBase2(l.Base, r.Base));
				return false;
			}

			result =
				new Correctness(
					Base.V,
					Miniscript.Input.Any,
					false,
					false
				);
			return true;
		}

		public override bool TryOrI(Correctness l, Correctness r, out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			var nextBase =
				(l.Base == Miniscript.Base.B && r.Base == Miniscript.Base.B) ? Miniscript.Base.B :
				(l.Base == Miniscript.Base.V && r.Base == Miniscript.Base.V) ? Miniscript.Base.V :
				(l.Base == Miniscript.Base.K && r.Base == Miniscript.Base.K) ? Miniscript.Base.K :
				Miniscript.Base.W;
			if (nextBase == Base.W)
			{
				error.Add(FragmentPropertyException.ChildBase2(l.Base, r.Base));
				return false;
			}
			result = new Correctness(
				nextBase,
				(l.Input == Miniscript.Input.Zero && r.Input == Miniscript.Input.Zero)
					? Miniscript.Input.One
					: Miniscript.Input.Any,
				(l.DisSatisfiable || r.DisSatisfiable),
				l.Unit && r.Unit
			);
			return true;
		}

		public override bool TryAndOr(Correctness a, Correctness b, Correctness c, out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			if (!a.DisSatisfiable)
			{
				error.Add(FragmentPropertyException.LeftNotDissatisfiable());
				return false;
			}

			if (!a.Unit)
			{
				error.Add(FragmentPropertyException.LeftNotUnit());
				return false;
			}

			var nextBase =
				(a.Base == Base.B && b.Base == Base.B && c.Base == Base.B) ? Miniscript.Base.B :
				(a.Base == Base.B && b.Base == Base.K && c.Base == Base.K) ? Base.K :
				(a.Base == Base.B && b.Base == Base.V && c.Base == Base.V) ? Base.V :
				Base.W;
			if (nextBase == Base.W)
			{
				error.Add(FragmentPropertyException.ChildBase3(a.Base, b.Base, c.Base));
				return false;
			}
			result = new Correctness(
				nextBase,
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
			return true;
		}

		public override bool TryThreshold(int k, int n, SubCk ck, out Correctness result, List<FragmentPropertyException> error)
		{
			result = null;
			var isN = k == n;
			for (int i = 0; i < n; i++)
			{
				if (!ck(i, out var subType, error))
					return false;
				if (i == 0)
				{
					isN &= subType.Input == Input.OneNonZero || subType.Input == Input.AnyNonZero;
					if (subType.Base != Base.B)
					{
						error.Add(FragmentPropertyException.ThresholdBase(0U, subType.Base));
						return false;
					}
				}
				else
				{
					if (subType.Base != Miniscript.Base.W)
					{
						error.Add(FragmentPropertyException.ThresholdBase((uint) n, subType.Base));
						return false;
					}
				}

				if (!subType.Unit)
				{
					error.Add(FragmentPropertyException.ThresholdNonUnit((uint) n));
					return false;
				}

				if (!subType.DisSatisfiable)
				{
					error.Add(FragmentPropertyException.ThresholdDissat((uint) n));
					return false;
				}
			}
			result = new Correctness(
				Base.B,
				(isN ? Input.AnyNonZero : Miniscript.Input.Any),
				true,
				true
				);
			return true;
		}
	}
}
