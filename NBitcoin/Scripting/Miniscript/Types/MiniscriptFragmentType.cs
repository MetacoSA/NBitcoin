using System;
using System.Collections.Generic;
using System.Diagnostics;
using NBitcoin.Scripting.Miniscript.Policy;
using Dissat = NBitcoin.Scripting.Miniscript.Types.Dissat;
using Malleability = NBitcoin.Scripting.Miniscript.Types.Malleability;

namespace NBitcoin.Scripting.Miniscript.Types
{
	[DebuggerDisplay("{" + nameof(DebugPrint) + "()}")]
	internal class MiniscriptFragmentType : IProperty<MiniscriptFragmentType>, IEquatable<MiniscriptFragmentType>
	{
		internal readonly Correctness Correctness;

		internal readonly Malleability Malleability;

		private MiniscriptFragmentType(Correctness correctness, Malleability malleability)
		{
			Correctness = correctness;
			Malleability = malleability;
		}

		public MiniscriptFragmentType() {}

		public bool IsSubtype(MiniscriptFragmentType other) =>
			Correctness.IsSubtype(other.Correctness) &&
			Malleability.IsSubtype(other.Malleability);

		public override void SanityChecks()
		{
			Debug.Assert(
				(!this.Correctness.DisSatisfiable || this.Malleability.Dissat != Dissat.None) &&
				(this.Malleability.Dissat == Dissat.None || this.Correctness.Base != Base.V) &&
				(this.Malleability.Safe || this.Correctness.Base != Base.K) &&
				(this.Malleability.NonMalleable || this.Correctness.Input != Input.Zero), this.DebugPrint());
		}

		internal string DebugPrint()
			=> $"Correctness: {Correctness.DebugPrint()}. is safe?: {Malleability.Safe}. is non malleable?: {Malleability.NonMalleable}";

		public override MiniscriptFragmentType FromTrue()
			=>
			new MiniscriptFragmentType(
				new Correctness().FromTrue(),
				new Malleability().FromTrue());

		public override MiniscriptFragmentType FromFalse() =>
			new MiniscriptFragmentType(
				new Correctness().FromFalse(),
				new Malleability().FromFalse());

		public override MiniscriptFragmentType FromPk()
			=>
			new MiniscriptFragmentType(
				new Correctness().FromPk(),
				new Malleability().FromPk());

		public override MiniscriptFragmentType FromPkH()
			=>
			new MiniscriptFragmentType(new Correctness().FromPkH(), new Malleability().FromPkH());

		public override MiniscriptFragmentType FromMulti(int k, int pkLength)
			=>
			new MiniscriptFragmentType(
				new Correctness().FromMulti(k, pkLength),
				new Malleability().FromMulti(k, pkLength));

		public override MiniscriptFragmentType FromTime(uint time) =>
			new MiniscriptFragmentType(
				new Correctness().FromTime(time),
				new Malleability().FromTime(time)
				);

		public override MiniscriptFragmentType FromAfter(uint time)
			=>
			new MiniscriptFragmentType(
				new Correctness().FromAfter(time),
				new Malleability().FromAfter(time));

		public override MiniscriptFragmentType FromOlder(uint time)
			=> new MiniscriptFragmentType(
				new Correctness().FromOlder(time),
				new Malleability().FromOlder(time));

		public override MiniscriptFragmentType FromHash()
			=>

			new MiniscriptFragmentType(
				new Correctness().FromHash(),
				new Malleability().FromHash());

		public override MiniscriptFragmentType FromSha256()
			=>
			new MiniscriptFragmentType(
				new Correctness().FromSha256(),
				new Malleability().FromSha256());

		public override MiniscriptFragmentType FromHash256()
			=>
			new MiniscriptFragmentType(
				new Correctness().FromHash256(),
				new Malleability().FromHash256());

		public override MiniscriptFragmentType FromRipemd160()
			=>
			new MiniscriptFragmentType(
				new Correctness().FromRipemd160(),
				new Malleability().FromRipemd160());

		public override MiniscriptFragmentType FromHash160()
			=>
			new MiniscriptFragmentType(
				new Correctness().FromHash160(),
				new Malleability().FromHash160());

		public override bool TryCastAlt(out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Correctness.TryCastAlt(out var corr, error) && Malleability.TryCastAlt(out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}

		public override bool TryCastSwap(out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Correctness.TryCastSwap(out var corr, error) && Malleability.TryCastSwap(out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}

		public override bool TryCastCheck(out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Correctness.TryCastCheck(out var corr, error) && Malleability.TryCastCheck(out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}
		public override bool TryCastDupIf(out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Correctness.TryCastDupIf(out var corr, error) && Malleability.TryCastDupIf(out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}

		public override bool TryCastVerify(out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Correctness.TryCastVerify(out var corr, error) && Malleability.TryCastVerify(out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}

		public override bool TryCastNonZero(out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Correctness.TryCastNonZero(out var corr, error) && Malleability.TryCastNonZero(out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}

		public override bool TryCastZeroNotEqual(out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Correctness.TryCastZeroNotEqual(out var corr, error) && Malleability.TryCastZeroNotEqual(out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}


		public override bool TryCastTrue(out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Correctness.TryCastTrue(out var corr, error) && Malleability.TryCastTrue(out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}

		public override bool TryCastOrIFalse(out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Correctness.TryCastOrIFalse(out var corr, error) && Malleability.TryCastOrIFalse(out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}
		public override bool TryCastUnLikely(out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Correctness.TryCastUnLikely(out var corr, error) && Malleability.TryCastUnLikely(out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}

		public override bool TryCastLikely(out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (Correctness.TryCastLikely(out var corr, error) && Malleability.TryCastLikely(out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}


		public override bool TryAndB(MiniscriptFragmentType left, MiniscriptFragmentType right,
			out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (new Correctness().TryAndB(left.Correctness, right.Correctness,out var corr, error)
			    && new Malleability().TryAndB(left.Malleability, right.Malleability , out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}

		public override bool TryAndV(MiniscriptFragmentType left, MiniscriptFragmentType right, out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (new Correctness().TryAndV(left.Correctness, right.Correctness,out var corr, error)
			    && new Malleability().TryAndV(left.Malleability, right.Malleability , out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}


		public override bool TryOrB(MiniscriptFragmentType left, MiniscriptFragmentType right, out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (new Correctness().TryOrB(left.Correctness, right.Correctness,out var corr, error)
			    && new Malleability().TryOrB(left.Malleability, right.Malleability , out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}

		public override bool TryOrD(MiniscriptFragmentType left, MiniscriptFragmentType right, out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (new Correctness().TryOrD(left.Correctness, right.Correctness,out var corr, error)
			    && new Malleability().TryOrD(left.Malleability, right.Malleability , out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}


		public override bool TryOrC(MiniscriptFragmentType left, MiniscriptFragmentType right, out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (new Correctness().TryOrC(left.Correctness, right.Correctness,out var corr, error)
			    && new Malleability().TryOrC(left.Malleability, right.Malleability , out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}


		public override bool TryOrI(MiniscriptFragmentType left, MiniscriptFragmentType right, out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (new Correctness().TryOrI(left.Correctness, right.Correctness,out var corr, error)
			    && new Malleability().TryOrI(left.Malleability, right.Malleability , out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}


		public override bool TryAndOr(MiniscriptFragmentType a, MiniscriptFragmentType b, MiniscriptFragmentType c, out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			if (new Correctness().TryAndOr(a.Correctness, b.Correctness,c.Correctness, out var corr, error)
			    && new Malleability().TryAndOr(a.Malleability, b.Malleability, c.Malleability, out var mall, error))
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}

			return false;
		}



		public override bool TryThreshold(int k, int n, SubCk subCk,
			out MiniscriptFragmentType result, List<FragmentPropertyException> error)
		{
			result = null;
			IProperty<Correctness>.SubCk getSubCorrectness =
				(int i, out Correctness subCorr, List<FragmentPropertyException> list) =>
				{
					subCorr = null;
					if (subCk(i, out var subResult, error))
					{
						subCorr = subResult.Correctness;
						return true;
					}
					return false;
				};
			IProperty<Malleability>.SubCk getSubMalleability =
				(int i, out Malleability subCorr, List<FragmentPropertyException> list) =>
				{
					subCorr = null;
					if (subCk(i, out var subResult, error))
					{
						subCorr = subResult.Malleability;
						return true;
					}
					return false;
				};
			if (new Correctness().TryThreshold(k, n, getSubCorrectness, out var corr, error)
			    && new Malleability().TryThreshold(k, n, getSubMalleability, out var mall, error)
			)
			{
				result = new MiniscriptFragmentType(corr, mall);
				return true;
			}
			return false;
		}


		public override bool Equals(object obj) => Equals(obj as MiniscriptFragmentType);
		public bool Equals(MiniscriptFragmentType other)
			=> (!(other is null))
				&& (Correctness.Base == other.Correctness.Base)
				&& (Correctness.Input == other.Correctness.Input)
				&& (Correctness.DisSatisfiable == other.Correctness.DisSatisfiable)
				&& (Correctness.Unit == other.Correctness.Unit)
				&& (Malleability.Dissat == other.Malleability.Dissat)
				&& (Malleability.NonMalleable == other.Malleability.NonMalleable)
				&& (Malleability.Safe == other.Malleability.Safe);

		public override int GetHashCode()
		{
			int num = 0;
			num = -1640531527 + Correctness.Base.GetHashCode() + ((num << 6) + (num >> 2));
			num = -1640531527 + Correctness.Input.GetHashCode() + ((num << 6) + (num >> 2));
			num = -1640531527 + Correctness.DisSatisfiable.GetHashCode() + ((num << 6) + (num >> 2));
			num = -1640531527 + Correctness.Unit.GetHashCode() + ((num << 6) + (num >> 2));
			num = -1640531527 + Malleability.Dissat.GetHashCode() + ((num << 6) + (num >> 2));
			num = -1640531527 + Malleability.NonMalleable.GetHashCode() + ((num << 6) + (num >> 2));
			num = -1640531527 + Malleability.Safe.GetHashCode() + ((num << 6) + (num >> 2));
			return num;
		}
	}
}
