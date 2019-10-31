using System;
using System.Diagnostics;
using NBitcoin.Scripting.Miniscript.Policy;
using Dissat = NBitcoin.Scripting.Miniscript.Types.Dissat;
using Malleability = NBitcoin.Scripting.Miniscript.Types.Malleability;

namespace NBitcoin.Scripting.Miniscript.Types
{
	public class MiniscriptFragmentType : IProperty<MiniscriptFragmentType>
	{
		public readonly Correctness Correctness;

		internal readonly Malleability Malleability;

		private MiniscriptFragmentType(Correctness correctness, Malleability malleability)
		{
			Correctness = correctness;
			Malleability = malleability;
		}

		public MiniscriptFragmentType() {}

		public bool IsSubtype(MiniscriptFragmentType other) =>
			this.Correctness.IsSubtype(other.Correctness) &&
			this.Malleability.IsSubtype(other.Malleability);

		public void SanityChecks()
		{
			Debug.Assert(!this.Correctness.DisSatisfiable || this.Malleability.Dissat != Dissat.None);
			Debug.Assert(this.Malleability.Dissat == Dissat.None || this.Correctness.Base != Base.K);
			Debug.Assert(this.Malleability.Safe || this.Correctness.Base != Base.K);
			Debug.Assert(this.Malleability.NonMalleable || this.Correctness.Input != Input.Zero);
		}

		public MiniscriptFragmentType FromTrue()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(cor.FromTrue(), mal.FromTrue());
		}

		public MiniscriptFragmentType FromFalse()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(cor.FromFalse(), mal.FromFalse());
		}

		public MiniscriptFragmentType FromPk()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(cor.FromPk(), mal.FromPk());
		}

		public MiniscriptFragmentType FromPkH()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(cor.FromPkH(), mal.FromPkH());
		}

		public MiniscriptFragmentType FromMulti(int k, int pkLength)
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(cor.FromMulti(k, pkLength), mal.FromMulti(k, pkLength));
		}

		public MiniscriptFragmentType FromAfter(uint time)
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.FromAfter(time),
				mal.FromAfter(time));
		}

		public MiniscriptFragmentType FromOlder(uint time)
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.FromOlder(time),
				mal.FromOlder(time));
		}

		public MiniscriptFragmentType FromHash()
		{

			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.FromHash(),
				mal.FromHash());
		}

		public MiniscriptFragmentType FromSha256()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.FromSha256(),
				mal.FromSha256());
		}

		public MiniscriptFragmentType FromHash256()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.FromHash256(),
				mal.FromHash256());
		}

		public MiniscriptFragmentType FromRipemd160()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.FromRipemd160(),
				mal.FromRipemd160());
		}

		public MiniscriptFragmentType FromHash160()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.FromHash160(),
				mal.FromHash160());
		}

		public MiniscriptFragmentType CastAlt()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.CastAlt(),
				mal.CastAlt());
		}

		public MiniscriptFragmentType CastSwap()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.CastSwap(),
				mal.CastSwap());
		}

		public MiniscriptFragmentType CastCheck()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.CastCheck(),
				mal.CastCheck());
		}

		public MiniscriptFragmentType CastDupIf()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.CastDupIf(),
				mal.CastDupIf());
		}

		public MiniscriptFragmentType CastVerify()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.CastVerify(),
				mal.CastVerify());
		}

		public MiniscriptFragmentType CastNonZero()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.CastNonZero(),
				mal.CastNonZero());
		}

		public MiniscriptFragmentType CastZeroNotEqual()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.CastZeroNotEqual(),
				mal.CastZeroNotEqual());
		}

		public MiniscriptFragmentType CastTrue()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.CastTrue(),
				mal.CastTrue());
		}

		public MiniscriptFragmentType CastOrIFalse()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.CastOrIFalse(),
				mal.CastOrIFalse());
		}

		public MiniscriptFragmentType CastUnLikely()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.CastUnLikely(),
				mal.CastUnLikely());
		}

		public MiniscriptFragmentType CastLikely()
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.CastLikely(),
				mal.CastLikely());
		}

		public MiniscriptFragmentType AndB(MiniscriptFragmentType left, MiniscriptFragmentType right)
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.AndB(left.Correctness, right.Correctness),
				mal.AndB(left.Malleability, right.Malleability));
		}

		public MiniscriptFragmentType AndV(MiniscriptFragmentType left, MiniscriptFragmentType right)
		{
			var cor = new Correctness();
			var mal = new Malleability();
			return new MiniscriptFragmentType(
				cor.AndV(left.Correctness, right.Correctness),
				mal.AndV(left.Malleability, right.Malleability));
		}

		public MiniscriptFragmentType AndN(MiniscriptFragmentType left, MiniscriptFragmentType right)
		{
			throw new NotImplementedException();
		}

		public MiniscriptFragmentType OrB(MiniscriptFragmentType left, MiniscriptFragmentType right)
		{
			throw new NotImplementedException();
		}

		public MiniscriptFragmentType OrD(MiniscriptFragmentType left, MiniscriptFragmentType right)
		{
			throw new NotImplementedException();
		}

		public MiniscriptFragmentType OrC(MiniscriptFragmentType left, MiniscriptFragmentType right)
		{
			throw new NotImplementedException();
		}

		public MiniscriptFragmentType OrI(MiniscriptFragmentType left, MiniscriptFragmentType right)
		{
			throw new NotImplementedException();
		}

		public MiniscriptFragmentType AndOr(MiniscriptFragmentType a, MiniscriptFragmentType b, MiniscriptFragmentType c)
		{
			throw new NotImplementedException();
		}

		public MiniscriptFragmentType Threshold(int k, int n, Func<uint, MiniscriptFragmentType> subCk)
		{
			throw new NotImplementedException();
		}
	}
}
