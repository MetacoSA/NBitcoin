using System;
using System.Diagnostics;
using NBitcoin.Scripting.Miniscript.Policy;
using Dissat = NBitcoin.Scripting.Miniscript.Types.Dissat;
using Malleability = NBitcoin.Scripting.Miniscript.Types.Malleability;

namespace NBitcoin.Scripting.Miniscript.Types
{
	public class MiniscriptFragmentType : IProperty<MiniscriptFragmentType>
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
			Debug.Assert(!this.Correctness.DisSatisfiable || this.Malleability.Dissat != Dissat.None);
			Debug.Assert(this.Malleability.Dissat == Dissat.None || this.Correctness.Base != Base.V);
			Debug.Assert(this.Malleability.Safe || this.Correctness.Base != Base.K);
			Debug.Assert(this.Malleability.NonMalleable || this.Correctness.Input != Input.Zero);
		}

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

		public override MiniscriptFragmentType CastAlt()
			=>
			new MiniscriptFragmentType(
				Correctness.CastAlt(),
				Malleability.CastAlt());

		public override MiniscriptFragmentType CastSwap()
			=> new MiniscriptFragmentType(
				Correctness.CastSwap(),
				Malleability.CastSwap());

		public override MiniscriptFragmentType CastCheck()
			=> new MiniscriptFragmentType(
				Correctness.CastCheck(),
				Malleability.CastCheck());

		public override MiniscriptFragmentType CastDupIf()
			=> new MiniscriptFragmentType(
				Correctness.CastDupIf(),
				Malleability.CastDupIf());

		public override MiniscriptFragmentType CastVerify()
			=> new MiniscriptFragmentType(
				Correctness.CastVerify(),
				Malleability.CastVerify());
		public override MiniscriptFragmentType CastNonZero()
			=>
			new MiniscriptFragmentType(
				Correctness.CastNonZero(),
				Malleability.CastNonZero());

		public override MiniscriptFragmentType CastZeroNotEqual()
			=>
			new MiniscriptFragmentType(
				Correctness.CastZeroNotEqual(),
				Malleability.CastZeroNotEqual());

		public override MiniscriptFragmentType CastTrue()
			=>
			new MiniscriptFragmentType(
				Correctness.CastTrue(),
				Malleability.CastTrue());

		public override MiniscriptFragmentType CastOrIFalse()
			=>
			new MiniscriptFragmentType(
				Correctness.CastOrIFalse(),
				Malleability.CastOrIFalse());

		public override MiniscriptFragmentType CastUnLikely()
			=>
			new MiniscriptFragmentType(
				Correctness.CastUnLikely(),
				Malleability.CastUnLikely());

		public override MiniscriptFragmentType CastLikely()
			=>
			new MiniscriptFragmentType(
				Correctness.CastLikely(),
				Malleability.CastLikely());

		public override MiniscriptFragmentType AndB(MiniscriptFragmentType left, MiniscriptFragmentType right)
			=>
			new MiniscriptFragmentType(
				new Correctness().AndB(left.Correctness, right.Correctness),
				new Malleability().AndB(left.Malleability, right.Malleability));

		public override MiniscriptFragmentType AndV(MiniscriptFragmentType left, MiniscriptFragmentType right)
			=>
			new MiniscriptFragmentType(
				new Correctness().AndV(left.Correctness, right.Correctness),
				new Malleability().AndV(left.Malleability, right.Malleability));


		public override MiniscriptFragmentType OrB(MiniscriptFragmentType left, MiniscriptFragmentType right)
			=>
			new MiniscriptFragmentType(
				new Correctness().OrB(left.Correctness, right.Correctness),
				new Malleability().OrB(left.Malleability, right.Malleability));

		public override MiniscriptFragmentType OrD(MiniscriptFragmentType left, MiniscriptFragmentType right)
			=>
			new MiniscriptFragmentType(
				new Correctness().OrD(left.Correctness, right.Correctness),
				new Malleability().OrD(left.Malleability, right.Malleability));

		public override MiniscriptFragmentType OrC(MiniscriptFragmentType left, MiniscriptFragmentType right)
			=>
			new MiniscriptFragmentType(
				new Correctness().OrC(left.Correctness, right.Correctness),
				new Malleability().OrC(left.Malleability, right.Malleability));

		public override MiniscriptFragmentType OrI(MiniscriptFragmentType left, MiniscriptFragmentType right)
			=>
			new MiniscriptFragmentType(
				new Correctness().OrI(left.Correctness, right.Correctness),
				new Malleability().OrI(left.Malleability, right.Malleability));

		public override MiniscriptFragmentType AndOr(MiniscriptFragmentType a, MiniscriptFragmentType b, MiniscriptFragmentType c)
			=>
			new MiniscriptFragmentType(
				new Correctness().AndOr(a.Correctness, b.Correctness, c.Correctness),
				new Malleability().AndOr(a.Malleability, b.Malleability, c.Malleability));

		public override MiniscriptFragmentType Threshold(int k, int n, Func<int, MiniscriptFragmentType> func)
			=>
				new MiniscriptFragmentType(
					new Correctness().Threshold(k, n, i => func(i).Correctness),
					new Malleability().Threshold(k, n , i => func(i).Malleability));
	}
}
