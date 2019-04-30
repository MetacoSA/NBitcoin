using System;
using SignatureProvider = System.Func<NBitcoin.PubKey, NBitcoin.TransactionSignature>;
using PreimageProvider = System.Func<NBitcoin.uint256, NBitcoin.uint256>;
using System.Linq;
using System.Collections.Generic;

namespace NBitcoin.Miniscript
{
	public static class AstElemSatisfyExtension
	{
		/// <summary>
		/// compute witness item so that the script can pass the verifycation.
		/// Or throw `SatisfyException` if it is impossible.
		/// </summary>
		/// <param name="signatureProvider">Should return null if it can not find according signature</param>
		/// <param name="preimageProvider">Should return null if it can not find according preimage</param>
		/// <param name="age"></param>
		/// <returns></returns>
		public static byte[][] Satisfy(
			this AstElem ast,
			SignatureProvider signatureProvider = null,
			PreimageProvider preimageProvider = null,
			uint? age = null
			)
			=> SatisfyCore(ast, signatureProvider, preimageProvider, age).ToArray();

		private static List<byte[]>  SatisfyCore(
			AstElem ast,
			SignatureProvider signatureProvider = null,
			PreimageProvider preimageProvider = null,
			uint? age = null
			)
		{
			switch (ast)
			{
				case AstElem.Pk self:
					return new List<byte[]> { SatisfyCheckSig(self.Item1, signatureProvider) };
				case AstElem.PkV self:
					return new List<byte[]> { SatisfyCheckSig(self.Item1, signatureProvider) };
				case AstElem.PkQ self:
					return new List<byte[]> { SatisfyCheckSig(self.Item1, signatureProvider) };
				case AstElem.PkW self:
					return new List<byte[]> { SatisfyCheckSig(self.Item1, signatureProvider) };
				case AstElem.Multi self:
					return SatisfyCheckMultiSig(self.Item1, self.Item2, signatureProvider);
				case AstElem.MultiV self:
					return SatisfyCheckMultiSig(self.Item1, self.Item2, signatureProvider);
				case AstElem.TimeT self:
					return SatisfyCSV(self.Item1, age);
				case AstElem.TimeV self:
					return SatisfyCSV(self.Item1, age);
				case AstElem.TimeF self:
					return SatisfyCSV(self.Item1, age);
				case AstElem.Time self:
					SatisfyCSV(self.Item1, age);
					return new List<byte[]> { new[] { (byte)1u } };
				case AstElem.TimeW self:
					SatisfyCSV(self.Item1, age);
					return new List<byte[]> { new [] { (byte)1u } };
				case AstElem.HashT self:
					return SatisfyHashEqual(self.Item1, preimageProvider);
				case AstElem.HashV self:
					return SatisfyHashEqual(self.Item1, preimageProvider);
				case AstElem.HashW self:
					return SatisfyHashEqual(self.Item1, preimageProvider);
				case AstElem.True self:
					return SatisfyCore(self.Item1, signatureProvider, preimageProvider, age);
				case AstElem.Wrap self:
					return SatisfyCore(self.Item1, signatureProvider, preimageProvider, age);
				case AstElem.Likely self:
					var retLikely = SatisfyCore(self.Item1, signatureProvider, preimageProvider, age);
					retLikely.Add(new byte[] {(byte)1u }) ;
					return retLikely;
				case AstElem.Unlikely self:
					var retUnlikely = SatisfyCore(self.Item1, signatureProvider, preimageProvider, age);
					retUnlikely.Add(new byte[] {(byte)1u }) ;
					return retUnlikely;
				case AstElem.AndCat self:
					var retAndCat = SatisfyCore(self.Item1, signatureProvider, preimageProvider, age);
					retAndCat.AddRange(SatisfyCore(self.Item2, signatureProvider, preimageProvider, age));
					return retAndCat;
				case AstElem.AndBool self:
					var retAndBool = SatisfyCore(self.Item1, signatureProvider, preimageProvider, age);
					retAndBool.ToList().AddRange(SatisfyCore(self.Item2, signatureProvider, preimageProvider, age));
					return retAndBool;
				case AstElem.AndCasc self:
					var retAndCasc = SatisfyCore(self.Item1, signatureProvider, preimageProvider, age);
					retAndCasc.ToList().AddRange(Satisfy(self.Item2, signatureProvider, preimageProvider, age));
					return retAndCasc;
				case AstElem.OrBool self:
					return SatisfyParallelOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age);
				case AstElem.OrCasc self:
					return SatisfyCascadeOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age);
				case AstElem.OrCont self:
					return SatisfyCascadeOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age);
				case AstElem.OrKey self:
					return SatisfySwitchOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age);
				case AstElem.OrKeyV self:
					return SatisfySwitchOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age);
				case AstElem.OrIf self:
					return SatisfySwitchOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age);
				case AstElem.OrIfV self:
					return SatisfySwitchOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age);
				case AstElem.OrNotIf self:
					return SatisfySwitchOr(self.Item2, self.Item1, signatureProvider, preimageProvider, age);
				case AstElem.Thresh self:
					return SatisfyThreshold(self.Item1, self.Item2, signatureProvider, preimageProvider, age);
				case AstElem.ThreshV self:
					return SatisfyThreshold(self.Item1, self.Item2, signatureProvider, preimageProvider, age);
			}

			throw new Exception("Unreachable!");
		}

		private static List<byte[]> Dissatisfy(AstElem ast)
		{
			switch (ast)
			{
				case AstElem.Pk _:
				case AstElem.PkW _:
				case AstElem.TimeW _:
				case AstElem.HashW _:
					return new List<byte[]> { new byte[0] };
				case AstElem.Multi self:
					var retmulti = new List<byte[]> { new byte[0] };
					retmulti.Add(new byte[] { (byte)(self.Item1 + 1u) });
					return retmulti;
				case AstElem.True self:
					return Dissatisfy(self.Item1);
				case AstElem.Wrap self:
					return Dissatisfy(self.Item1);
				case AstElem.Likely _:
					return new List<byte[]> { new byte[] {1}};
				case AstElem.Unlikely _:
					return new List<byte[]> { new byte[0] };
				case AstElem.AndBool self:
					var retAndBool = Dissatisfy(self.Item2);
					retAndBool.AddRange(Dissatisfy(self.Item1));
					return retAndBool;
				case AstElem.AndCasc self:
					return Dissatisfy(self.Item1);
				case AstElem.OrBool self:
					var retOrBool = Dissatisfy(self.Item2);
					retOrBool.AddRange(Dissatisfy(self.Item1));
					return retOrBool;
				case AstElem.OrCasc self:
					var retOrCasc = Dissatisfy(self.Item2);
					retOrCasc.AddRange(Dissatisfy(self.Item1));
					return retOrCasc;
				case AstElem.OrIf self:
					var retOrIf = Dissatisfy(self.Item1);
					retOrIf.Add(new byte[0]);
					return retOrIf;
				case AstElem.OrNotIf self:
					var retOrNotIf = Dissatisfy(self.Item2);
					retOrNotIf.Add(new byte[0]);
					return retOrNotIf;
				case AstElem.Thresh self:
					var retThresh = new List<byte[]> { };
					foreach (var sub in self.Item2.Reverse())
						retThresh.AddRange(Dissatisfy(sub));
					return retThresh;
			}
			throw new Exception($"Unreachable! There is no way to dissatisfy {ast}");
		}

		private static byte[] SatisfyCheckSig(PubKey pk, SignatureProvider signatureProvider)
		{
			if (signatureProvider == null)
				throw new SatisfyException("Can not satisfy This AST without SignatureProvider! It contains pk()");
			var ret = signatureProvider(pk);
			if (ret == null)
				throw new SatisfyException($"Unable to provide signature for pubkey {pk}");
			return ret.ToBytes();
		}

		private static List<byte[]> SatisfyCheckMultiSig(uint m, PubKey[] pks, SignatureProvider signatureProvider)
		{
			if (signatureProvider == null)
				throw new SatisfyException("Can not satisfy This AST without SignatureProvider! It contains multi()");
			var sigs = new List<byte[]> { };
			var errors = new List<Exception> { };
			foreach (var pk in pks)
			{
				byte[] sig;
				try
				{
					sig = SatisfyCheckSig(pk, signatureProvider);
				}
				catch (SatisfyException e)
				{
					errors.Add(e);
					continue;
				}
				sigs.Add(sig);
			}
			if (sigs.Count < m)
				throw new SatisfyException("Failed to satisfy multisig", new AggregateException(errors));
			sigs = sigs.Count > m ? sigs.Skip(sigs.Count - (int)m).ToList() : sigs;
			var ret = new List<byte[]> { new byte[0] };
			ret.AddRange(sigs);
			return ret;
		}

		private static uint SatisfyCost(List<byte[]> ss)
			=> (uint)ss.Select(s => s.Length + 1).Sum();

		private static List<byte[]> Flatten(List<List<byte[]>> v)
			=> v.Aggregate((lAcc, l) => { lAcc.AddRange(l); return lAcc; });
		private static List<byte[]> SatisfyThreshold(
			uint k, AstElem[] subAsts,
			SignatureProvider signatureProvider,
			PreimageProvider preimageProvider,
			uint? age
			)
		{
			if (k == 0)
				return new List<byte[]> { };
			var ret = new List<List<byte[]>> { };
			var retDissatisfied = new List<List<byte[]>> { };
			var errors = new List<Exception> { };
			int satisfiedN = 0;
			foreach (var sub in subAsts.Reverse())
			{
				var dissat = Dissatisfy(sub);
				try
				{
					var satisfiedItem = SatisfyCore(sub, signatureProvider, preimageProvider, age);
					ret.Add(satisfiedItem);
					satisfiedN++;
				}
				catch(SatisfyException ex)
				{
					ret.Add(dissat);
					errors.Add(ex);
				}
				retDissatisfied.Add(dissat);
			}
			if (satisfiedN < k)
				throw new SatisfyException(
					$"Failed to satisfy {k} sub expression. Only {satisfiedN} are satisfied",
					new AggregateException(errors)
				);
			if (satisfiedN == k)
				return Flatten(ret);

			// if we have more satisfactions than needed, throw away the extras, choosing
			// the ones that would yield the biggest savings.
			var indices = new List<int> { };
			for (int i = 0; i < subAsts.Length; i++)
				indices.Add(i);
			var sortedIndices = indices.OrderBy(i => SatisfyCost(retDissatisfied[i]) - SatisfyCost(retDissatisfied[i]));
			foreach (int i in sortedIndices.Take(satisfiedN - (int)k))
				ret[i] = retDissatisfied[i];
			return Flatten(ret);
		}

		private static List<byte[]> SatisfySwitchOr(
			AstElem l, AstElem r,
			SignatureProvider signatureProvider,
			PreimageProvider preimageProvider,
			uint? age
			)
		{
			List<byte[]> lSat = null;
			List<byte[]> rSat = null;
			SatisfyException leftEx = null;
			SatisfyException rightEx = null;
			try
			{
				lSat = SatisfyCore(l, signatureProvider, preimageProvider, age);
			}
			catch (SatisfyException ex)
			{
				leftEx = ex;
			}
			try
			{
				rSat = SatisfyCore(r, signatureProvider, preimageProvider, age);
			}
			catch (SatisfyException ex)
			{
				rightEx = ex;
			}

			if (leftEx != null && rightEx != null)
			{
				throw new SatisfyException($"Failed to satisfy neither {l} nor {r}", new AggregateException(new[] { leftEx, rightEx }));
			}
			if (leftEx == null && rightEx != null)
			{
				lSat.Add(new byte[] { 1 });
				return lSat;
			}
			if (leftEx != null && rightEx == null)
			{
				rSat.Add(new byte[0] );
				return rSat;
			}
			else
			{
				if (SatisfyCost(lSat) + 2 <= SatisfyCost(rSat) + 1)
				{
					lSat.Add(new byte[] {1});
					return lSat;
				}
				else
				{
					rSat.Add(new byte[0]);
					return rSat;
				}
			}
		}

		private static List<byte[]> SatisfyCascadeOr(
			AstElem l, AstElem r,
			SignatureProvider signatureProvider,
			PreimageProvider preimageProvider,
			uint? age 
			)
		{
			List<byte[]> lSat = null;
			List<byte[]> rSat = null;
			SatisfyException leftEx = null;
			SatisfyException rightEx = null;
			try
			{
				lSat = SatisfyCore(l, signatureProvider, preimageProvider, age);
			}
			catch (SatisfyException ex)
			{
				leftEx = ex;
			}
			try
			{
				rSat = SatisfyCore(r, signatureProvider, preimageProvider, age);
			}
			catch (SatisfyException ex)
			{
				rightEx = ex;
			}

			if (leftEx != null && rightEx != null)
			{
				throw new SatisfyException($"Failed to satisfy neither {l} nor {r}", new AggregateException(new[] { leftEx, rightEx }));
			}
			if (leftEx == null && rightEx != null)
			{
				return lSat;
			}
			if (leftEx != null && rightEx == null)
			{
				var lDissat = Dissatisfy(l);
				rSat.AddRange(lDissat);
				return rSat;
			}
			else
			{
				var lDissat = Dissatisfy(l);

				if (SatisfyCost(lSat) <= SatisfyCost(rSat) + SatisfyCost(lDissat))
				{
					return lSat;
				}
				else
				{
					rSat.AddRange(lDissat);
					return rSat;
				}
			}
		}

		private static List<byte[]> SatisfyParallelOr(
			AstElem l, AstElem r,
			SignatureProvider signatureProvider,
			PreimageProvider preimageProvider,
			uint? age
			)
		{
			List<byte[]> lSat = null;
			List<byte[]> rSat = null;
			SatisfyException leftEx = null;
			SatisfyException rightEx = null;
			try
			{
				lSat = SatisfyCore(l, signatureProvider, preimageProvider, age);
			}
			catch (SatisfyException ex)
			{
				leftEx = ex;
			}
			try
			{
				rSat = SatisfyCore(r, signatureProvider, preimageProvider, age);
			}
			catch (SatisfyException ex)
			{
				rightEx = ex;
			}

			if (leftEx == null && rightEx == null)
			{
				var lDissat = Dissatisfy(l);
				var rDissat = Dissatisfy(r);
				if (SatisfyCost(lSat) + SatisfyCost(rSat) <= SatisfyCost(rSat) + SatisfyCost(lDissat))
				{
					rDissat.AddRange(lSat);
					return rDissat;
				}
				else
				{
					rSat.AddRange(lDissat);
					return rSat;
				}
			}
			else if (leftEx != null && rightEx == null)
			{
				var rDissat = Dissatisfy(r);
				rDissat.AddRange(lSat);
				return rDissat;
			}
			else if (leftEx == null && rightEx != null)
			{
				var lDissat = Dissatisfy(l);
				rSat.AddRange(lDissat);
				return rSat;
			}
			else
			{
				throw new SatisfyException($"Failed to satisfy neither {l} nor {r}", new AggregateException(new[] { leftEx, rightEx }));
			}
		}

		private static List<byte[]> SatisfyHashEqual(uint256 hash, PreimageProvider preimageProvider)
		{
			if (preimageProvider == null)
				throw new SatisfyException("Can not satisfy this AST without PreimageProvider!");
		}
		private static List<byte[]> SatisfyCSV(uint timelock, uint? age)
		{
			if (age == null)
				throw new SatisfyException("Please provide current time");
			if (age >= timelock)
				return new List<byte[]> { };
			else
				throw new SatisfyException("Locktime not met. ");
		}
	}
}