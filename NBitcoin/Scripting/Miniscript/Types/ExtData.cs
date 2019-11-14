using System;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.Scripting.Miniscript.Types
{

	internal static class Constants
	{
		internal const int MAX_OPS_PER_SCRIPT = 201;
	}

	/// <summary>
	///  Whether a fragment is OK to be used in non-segwit scripts
	/// </summary>
	internal enum LegacySafe
	{
		/// <summary>
		/// The fragment can be used in pre-segwit contexts without concern about malleability
		/// attacks/unbounded 3rd-party fee stuffing. This means it has no `pk_h` constructions
		/// (cannot estimate public key size from a hash) and no `d:`/`or_i` constructions
		/// (cannot control the size of the switch input to `OP_IF`)
		/// </summary>
		LegacySafe,
		/// <summary>
		/// This fragment can only be safely used with Segwit.
		/// </summary>
		SegwitOnly
	}

	/// <summary>
	/// Structure representing the extra type properties of a fragment which are relevant to legacy (pre-segwit)
	/// safety and fee estimation. If a fragment is used in pre-segwit transactions it will only
	/// be malleable but still is correct and sound.
	/// </summary>
	internal class ExtData : IProperty<ExtData>
	{
		/// <summary>
		/// enum sorting whether the fragment is safe to be in used in pre-segwit context
		/// </summary>
		internal readonly LegacySafe LegacySafe;
		/// <summary>
		/// The number of bytes needed to encode its scriptPubKey
		/// </summary>
		internal readonly ulong PkCost;
		/// <summary>
		/// Whether this fragment can be verify-wrapped for free.
		/// </summary>
		internal readonly bool HasVerifyForm;
		/// <summary>
		/// The worst case static (unexecuted) ops-count for this miniscript fragment.
		/// </summary>
		internal readonly ulong OpsCountStatic;
		/// <summary>
		/// The worst case ops-count for satisfying this Miniscript fragment.
		/// </summary>
		internal readonly ulong?  OpsCountSat;
		/// <summary>
		/// The worst case ops-count for dissatisfying this Miniscript fragment.
		/// </summary>
		internal readonly ulong? OpsCountNSat;

		public ExtData() {}

		public ExtData(LegacySafe legacySafe, ulong pkCost, bool hasVerifyForm, ulong opsCountStatic, ulong? opsCountSat, ulong? opsCountNSat)
		{
			LegacySafe = legacySafe;
			PkCost = pkCost;
			HasVerifyForm = hasVerifyForm;
			OpsCountStatic = opsCountStatic;
			OpsCountSat = opsCountSat;
			OpsCountNSat = opsCountNSat;
		}

		public override ExtData FromTrue() =>
			new ExtData(
				LegacySafe.LegacySafe,
				1U,
				false,
				0UL,
				0,
				null
				);

		public override ExtData FromFalse() =>
			new ExtData(
				LegacySafe.LegacySafe,
				1U,
				false,
				0UL,
				null,
				0
				);

		public override ExtData FromPk() =>
			new ExtData(
				LegacySafe.LegacySafe,
				34,
				false,
				0,
				0,
				0
				);

		public override ExtData FromPkH() =>
			new ExtData(
				LegacySafe.SegwitOnly,
				24,
				false,
				3,
				3,
				3
				);

		public override ExtData FromMulti(int k, int n)
		{
			var numCost =
				((k > 16) && (n > 16)) ? 4 :
				(!(k > 16) && (n > 16)) ? 3 :
				((k > 16) && !(n > 16)) ? 3 :
				2;
			return new ExtData(
				LegacySafe.LegacySafe,
				((ulong)numCost + 34UL * (ulong)n + 1UL),
				true,
				1,
				(ulong)n + 1UL,
				(ulong)n + 1UL
				);
		}


		public override ExtData FromHash()
		{
			throw new Exception("Unreachable");
		}

		public override ExtData FromSha256() =>
			new ExtData(
				LegacySafe.LegacySafe,
				33 + 6,
				true,
				4,
				4,
				null
				);

		public override ExtData FromHash256() => FromSha256();

		public override ExtData FromRipemd160() =>
			new ExtData(
				LegacySafe.LegacySafe,
				21 + 6,
				true,
				4,
				4,
				null
				);

		public override ExtData FromHash160() => FromRipemd160();

		public override ExtData FromTime(uint time) =>
			new ExtData(
				LegacySafe.LegacySafe,
				(ulong)Miniscript.Utils.ScriptNumSize(time) + 1UL,
				false,
				1,
				1,
				null
				);

		public override bool TryCastAlt(out ExtData result, List<FragmentPropertyException> error)
		{
			result =
				new ExtData(
					LegacySafe,
					PkCost + 2,
					false,
					OpsCountStatic + 2,
					OpsCountSat + 2,
					OpsCountNSat + 2
				);
			return true;
		}

		public override bool TryCastSwap(out ExtData result, List<FragmentPropertyException> error)
		{
			result =
			new ExtData(
				LegacySafe,
				PkCost + 1,
				HasVerifyForm,
				OpsCountStatic + 1,
				OpsCountSat + 1,
				OpsCountNSat + 1
			);
			return true;
		}

		public override bool TryCastCheck(out ExtData result, List<FragmentPropertyException> error) {
			result =
			new ExtData(
				LegacySafe,
				PkCost + 1,
				true,
				OpsCountStatic + 1,
				OpsCountSat + 1,
				OpsCountNSat + 1
				);
			return true;
		}

		public override bool TryCastDupIf(out ExtData result, List<FragmentPropertyException> error) {
			result =
			new ExtData(
				LegacySafe.SegwitOnly,
				PkCost + 3,
				false,
				OpsCountStatic + 3,
				OpsCountSat + 3,
				OpsCountStatic + 3
				);
			return true;
		}

		public override bool TryCastVerify(out ExtData result, List<FragmentPropertyException> error)
		{
			var verifyCost = this.HasVerifyForm ? 0UL : 1UL;
			result =
			new ExtData(
				LegacySafe,
				PkCost + verifyCost,
				false,
				OpsCountStatic + verifyCost,
				OpsCountSat + verifyCost,
				null
				);
			return true;
		}

		public override bool TryCastNonZero(out ExtData result, List<FragmentPropertyException> error)
		{
			result =
				new ExtData(
					LegacySafe,
					PkCost + 4UL,
					false,
					OpsCountStatic + 4,
					OpsCountSat + 4UL,
					OpsCountStatic + 4
				);
			return true;
		}

		public override bool TryCastZeroNotEqual(out ExtData result, List<FragmentPropertyException> error) {
			result =
			new ExtData(
				LegacySafe,
				PkCost + 1,
				false,
				OpsCountStatic + 1,
				OpsCountSat + 1UL,
				OpsCountNSat + 1UL
				);
			return true;
		}

		public override bool TryCastTrue(out ExtData result, List<FragmentPropertyException> error) {
			result =
			new ExtData(
				LegacySafe,
				PkCost + 1,
				false,
				OpsCountStatic,
				OpsCountSat,
				null
				);
			return true;
		}

		public override bool TryCastOrIFalse(out ExtData result, List<FragmentPropertyException> error)
		{
			throw new Exception("Unreachable!");
		}

		public override bool TryCastUnLikely(out ExtData result, List<FragmentPropertyException> error) {
			result =
			new ExtData(
				LegacySafe,
				PkCost + 4,
				false,
				OpsCountStatic + 3,
				OpsCountSat + 3,
				OpsCountNSat + 3
				);
			return true;
		}

		public override bool TryCastLikely(out ExtData result, List<FragmentPropertyException> error) {
			result =
			new ExtData(
				LegacySafe,
				PkCost + 4,
				false,
				OpsCountStatic + 3,
				OpsCountSat + 3,
				OpsCountStatic + 3
				);
			return true;
		}
		private static LegacySafe LegacySafe2(LegacySafe a, LegacySafe b) =>
			(a == LegacySafe.LegacySafe && b == LegacySafe.LegacySafe) ? LegacySafe.LegacySafe : LegacySafe.SegwitOnly;
		public override bool TryAndB(ExtData l, ExtData r, out ExtData result, List<FragmentPropertyException> error)
		{
			result =
				new ExtData(
					LegacySafe2(l.LegacySafe, r.LegacySafe),
					l.PkCost + r.PkCost + 1,
					false,
					l.OpsCountStatic + r.OpsCountStatic + 1,
					l.OpsCountSat + r.OpsCountSat + 1,
					l.OpsCountNSat + r.OpsCountNSat + 1
				);
			return true;
		}

		public override bool TryAndV(ExtData l, ExtData r, out ExtData result, List<FragmentPropertyException> error)
		{
			result =
			  new ExtData(
				LegacySafe2(l.LegacySafe, r.LegacySafe),
				l.PkCost + r.PkCost,
				r.HasVerifyForm,
				l.OpsCountStatic + r.OpsCountStatic,
				l.OpsCountSat + r.OpsCountSat,
				null
			);
			return true;
		}

		public override bool TryOrB(ExtData left, ExtData right, out ExtData result, List<FragmentPropertyException> error)
		{
			result =
			new ExtData(
				LegacySafe2(left.LegacySafe, right.LegacySafe),
				left.PkCost + right.PkCost + 1,
				false,
				left.OpsCountStatic + right.OpsCountStatic + 1,
				left.OpsCountSat + right.OpsCountSat + 1,
				left.OpsCountNSat + right.OpsCountNSat + 1
			);
			return true;
		}

		public override bool TryOrD(ExtData left, ExtData right, out ExtData result, List<FragmentPropertyException> error)
		{
			var opsCountSat1 = left.OpsCountSat + 3 + right.OpsCountStatic;
			var opsCountSat2 = right.OpsCountSat + left.OpsCountNSat + 3;
			result =
			new ExtData(
				LegacySafe.SegwitOnly,
				left.PkCost + right.PkCost + 3,
				false,
				left.OpsCountStatic + right.OpsCountStatic + 1,
				Nullable.Compare(opsCountSat1, opsCountSat2) > 0 ? opsCountSat1 : opsCountSat2,
				left.OpsCountSat + right.OpsCountNSat + 3
			);
			return true;
		}

		public override bool TryOrC(ExtData left, ExtData right, out ExtData result, List<FragmentPropertyException> error)
		{
			var opsCountSat1 = left.OpsCountSat + 2 + right.OpsCountStatic;
			var opsCountSat2 = right.OpsCountSat + left.OpsCountNSat + 2;

			result =
			new ExtData(
				LegacySafe2(left.LegacySafe, right.LegacySafe),
				left.PkCost + right.PkCost + 2,
				false,
				left.OpsCountStatic + right.OpsCountStatic + 2,
				Nullable.Compare(opsCountSat1, opsCountSat2) > 0 ? opsCountSat1 : opsCountSat2,
					null
			);
			return true;
		}

		public override bool TryOrI(ExtData left, ExtData right, out ExtData result, List<FragmentPropertyException> error)
		{
			var opsCountSat1 = left.OpsCountSat + right.OpsCountStatic + 3;
			var opsCountSat2 = right.OpsCountSat + left.OpsCountStatic + 3;
			var opsCountNSat =
				(left.OpsCountNSat.HasValue && right.OpsCountNSat.HasValue)
					? (Nullable.Compare(left.OpsCountNSat, right.OpsCountNSat) > 0
						  ? left.OpsCountNSat
						  : right.OpsCountNSat) + 3
					: (left.OpsCountNSat.HasValue)
						? left.OpsCountNSat + 3
						: right.OpsCountNSat + 3;
			result =
			new ExtData(
				LegacySafe2(left.LegacySafe, right.LegacySafe),
				left.PkCost + right.PkCost + 3,
				false,
				left.OpsCountStatic + right.OpsCountStatic + 3,
				Nullable.Compare(opsCountSat1, opsCountSat2) > 0 ? opsCountSat1 : opsCountSat2,
				opsCountNSat
				);
			return true;
		}

		public override bool TryAndOr(ExtData a, ExtData b, ExtData c, out ExtData result, List<FragmentPropertyException> error)
		{
			var legacySafe = LegacySafe2(LegacySafe2(a.LegacySafe, b.LegacySafe), c.LegacySafe);
			var opsCountSat1 = a.OpsCountSat + b.OpsCountSat + c.OpsCountStatic + 3;
			var opsCountSat2 = a.OpsCountNSat + b.OpsCountStatic + c.OpsCountSat + 3;
			var opsCountSat = Nullable.Compare(opsCountSat1, opsCountSat2) > 0 ? opsCountSat1 : opsCountSat2;

			var opsCountNSat = c.OpsCountNSat +  b.OpsCountStatic + a.OpsCountNSat + 3;

			result = new ExtData(
				legacySafe,
				a.PkCost + b.PkCost + c.PkCost + 2,
				false,
				a.OpsCountStatic + b.OpsCountStatic + c.OpsCountStatic + 3,
				opsCountSat,
				opsCountNSat
				);
			return true;
		}

		public override bool TryThreshold(int k, int n, Func<int, ExtData> subCk, out ExtData result, List<FragmentPropertyException> error)
		{
			var pkCost = 1UL + (ulong)Utils.ScriptNumSize(k);
			var legacySafe = LegacySafe.LegacySafe;
			var opsCountStatic = 0UL;
			var opsCountSatVec = new List<ulong?>();
			var opsCountNSatSum = 0UL;
			ulong? opsCountNSat = 0UL;
			ulong? opsCountSat = 0UL;
			var satCount = 0;
			for (int i = 0; i < n; i++)
			{
				var sub = subCk(i);
				pkCost += sub.PkCost;
				opsCountStatic += sub.OpsCountStatic;
				if (sub.OpsCountSat.HasValue && sub.OpsCountNSat.HasValue)
				{
					opsCountSatVec.Add(sub.OpsCountSat.Value - sub.OpsCountNSat.Value);
					opsCountNSat = opsCountNSat + sub.OpsCountNSat.Value;
					opsCountNSatSum += sub.OpsCountNSat.Value;
				}
				else if (sub.OpsCountSat.HasValue)
				{
					satCount++;
					opsCountSat = opsCountSat + sub.OpsCountSat.Value;
					opsCountNSat = null;
				}

				legacySafe = LegacySafe2(legacySafe, sub.LegacySafe);
			}

			var remainingSat = k - satCount;
			ulong sum = 0UL;
			if (k < satCount || opsCountSatVec.Count < remainingSat)
				opsCountSat = null;
			else
			{
				opsCountSatVec.Sort();
				opsCountSatVec.Reverse();
				Func<ulong?, double> summer = v => v.Value;
				sum = (ulong)opsCountSatVec.Skip(remainingSat).Sum(summer);
			}
			result = new ExtData(
				legacySafe,
				pkCost + (ulong)n - 1UL,
				true,
				opsCountStatic + ((ulong)n - 1UL) + 1UL,
				opsCountSat + ((ulong)n - 1UL) + 1UL + (sum + opsCountNSatSum),
				opsCountNSat + ((ulong)n - 1UL) + 1UL
				);
			return true;
		}
	}
}
