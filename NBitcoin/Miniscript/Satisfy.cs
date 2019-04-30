using System;
using SignatureProvider = System.Func<NBitcoin.PubKey, NBitcoin.TransactionSignature>;
using PreimageProvider = System.Func<NBitcoin.uint256, NBitcoin.uint256>;
using System.Linq;
using System.Collections.Generic;

namespace NBitcoin.Miniscript
{
	public partial class AstElem
	{
		/// <summary>
		/// compute witness item so that the script can pass the verifycation.
		/// Or throw `SatisfyException` if it is impossible.
		/// </summary>
		/// <param name="signatureProvider">Should return null if it can not find according signature</param>
		/// <param name="preimageProvider">Should return null if it can not find according preimage</param>
		/// <param name="age"></param>
		/// <returns></returns>
		public byte[][] Satisfy(
			SignatureProvider signatureProvider = null,
			PreimageProvider preimageProvider = null,
			uint? age = null
			)
			{
				var result = new List<byte[]>();
				var errors = new List<SatisfyError>();
				if(!TrySatisfy(signatureProvider, preimageProvider, age, result, errors))
					throw new SatisfyException(errors.ToArray());
				return result.ToArray();
			}

		private bool TrySatisfy(
			SignatureProvider signatureProvider,
			PreimageProvider preimageProvider ,
			uint? age,
			List<byte[]> result,
			List<SatisfyError> errors
			)
		{
			switch (this)
			{
				case AstElem.Pk self:
					return SatisfyCheckSig(self.Item1, signatureProvider, result, errors);
				case AstElem.PkV self:
					return SatisfyCheckSig(self.Item1, signatureProvider, result, errors);
				case AstElem.PkQ self:
					return SatisfyCheckSig(self.Item1, signatureProvider, result, errors);
				case AstElem.PkW self:
					return SatisfyCheckSig(self.Item1, signatureProvider, result, errors);
				case AstElem.Multi self:
					return SatisfyCheckMultiSig(self.Item1, self.Item2, signatureProvider, result, errors);
				case AstElem.MultiV self:
					return SatisfyCheckMultiSig(self.Item1, self.Item2, signatureProvider, result, errors);
				case AstElem.TimeT self:
					return SatisfyCSV(self.Item1, age, errors);
				case AstElem.TimeV self:
					return SatisfyCSV(self.Item1, age, errors);
				case AstElem.TimeF self:
					return SatisfyCSV(self.Item1, age, errors);
				case AstElem.Time self:
					if (SatisfyCSV(self.Item1, age, errors))
					{
						result.Add(new[] { (byte)1u });
						return true;
					}
					return false;
				case AstElem.TimeW self:
					if (SatisfyCSV(self.Item1, age, errors))
					{
						result.Add(new[] { (byte)1u });
						return true;
					}
					return false;
				case AstElem.HashT self:
					return SatisfyHashEqual(self.Item1, preimageProvider, result, errors);
				case AstElem.HashV self:
					return SatisfyHashEqual(self.Item1, preimageProvider, result, errors);
				case AstElem.HashW self:
					return SatisfyHashEqual(self.Item1, preimageProvider, result, errors);
				case AstElem.True self:
					return self.Item1.TrySatisfy(signatureProvider, preimageProvider, age, result, errors);
				case AstElem.Wrap self:
					return self.Item1.TrySatisfy(signatureProvider, preimageProvider, age, result, errors);
				case AstElem.Likely self:
					if (self.Item1.TrySatisfy(signatureProvider, preimageProvider, age, result, errors))
					{
						result.Add(new byte[] { (byte)0u });
						return true;
					}
					return false;
				case AstElem.Unlikely self:
					if (self.Item1.TrySatisfy(signatureProvider, preimageProvider, age, result, errors))
					{
						result.Add(new byte[] { (byte)1u });
						return true;
					}
					return false;
				case AstElem.AndCat self:
					if (self.Item1.TrySatisfy(signatureProvider, preimageProvider, age, result, errors))
						return self.Item2.TrySatisfy(signatureProvider, preimageProvider, age, result, errors);
					return false;
				case AstElem.AndBool self:
					if (self.Item1.TrySatisfy(signatureProvider, preimageProvider, age, result, errors))
						return self.Item2.TrySatisfy(signatureProvider, preimageProvider, age, result, errors);
					return false;
				case AstElem.AndCasc self:
					if (self.Item1.TrySatisfy(signatureProvider, preimageProvider, age, result, errors))
						return self.Item2.TrySatisfy(signatureProvider, preimageProvider, age, result, errors);
					return false;
				case AstElem.OrBool self:
					return SatisfyParallelOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age, result, errors);
				case AstElem.OrCasc self:
					return SatisfyCascadeOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age, result, errors);
				case AstElem.OrCont self:
					return SatisfyCascadeOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age, result, errors);
				case AstElem.OrKey self:
					return SatisfySwitchOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age, result, errors);
				case AstElem.OrKeyV self:
					return SatisfySwitchOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age, result, errors);
				case AstElem.OrIf self:
					return SatisfySwitchOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age, result, errors);
				case AstElem.OrIfV self:
					return SatisfySwitchOr(self.Item1, self.Item2, signatureProvider, preimageProvider, age, result, errors);
				case AstElem.OrNotIf self:
					return SatisfySwitchOr(self.Item2, self.Item1, signatureProvider, preimageProvider, age, result, errors);
				case AstElem.Thresh self:
					return SatisfyThreshold(self.Item1, self.Item2, signatureProvider, preimageProvider, age, result, errors);
				case AstElem.ThreshV self:
					return SatisfyThreshold(self.Item1, self.Item2, signatureProvider, preimageProvider, age, result, errors);
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

		private bool SatisfyCheckSig(
			PubKey pk,
			SignatureProvider signatureProvider,
			List<byte[]> result,
			List<SatisfyError> errors
			)
		{
			if (signatureProvider == null)
			{
				errors.Add(new SatisfyError(SatisfyErrorCode.NoSignatureProvider, this));
				return false;
			}
			var ret = signatureProvider(pk);
			if (ret == null)
			{
				errors.Add(new SatisfyError(SatisfyErrorCode.CanNotProvideSignature, this));
				return false;
			}
			else
			{
				result.Add(ret.ToBytes());
				return true;
			}
		}

		private bool SatisfyCheckMultiSig(
			uint m, PubKey[] pks,
			SignatureProvider signatureProvider,
			List<byte[]> result,
			List<SatisfyError> errors
			)
		{
			if (signatureProvider == null)
			{
				errors.Add(new SatisfyError(SatisfyErrorCode.NoSignatureProvider, this));
				return false;
			}
			var sigs = new List<byte[]> { };
			var localError = new List<SatisfyError>();
			foreach (var pk in pks)
			{
				var sig = new List<byte[]>();
				if (SatisfyCheckSig(pk, signatureProvider, sig, localError))
				{
					sigs.AddRange(sig);
				}
			}
			if (sigs.Count < m)
			{
				errors.AddRange(localError);
				return false;
			}
			sigs = sigs.Count > m ? sigs.Skip(sigs.Count - (int)m).ToList() : sigs;
			result.Add(new byte[0]);
			result.AddRange(sigs);
			return true;
		}

		private uint SatisfyCost(List<byte[]> ss)
			=> (uint)ss.Select(s => s.Length + 1).Sum();

		private List<byte[]> Flatten(List<List<byte[]>> v)
			=> v.Aggregate((lAcc, l) => { lAcc.AddRange(l); return lAcc; });
		private bool SatisfyThreshold(
			uint k, AstElem[] subAsts,
			SignatureProvider signatureProvider,
			PreimageProvider preimageProvider,
			uint? age,
			List<byte[]> result,
			List<SatisfyError> errors
			)
		{
			if (k == 0)
				return true;
			var ret = new List<List<byte[]>> { };
			var localErrors = new List<SatisfyError>();
			var retDissatisfied = new List<List<byte[]>> { };
			int satisfiedN = 0;
			foreach (var sub in subAsts.Reverse())
			{
				var dissat = Dissatisfy(sub);
				var satisfiedItem = new List<byte[]>();
				if (sub.TrySatisfy(signatureProvider, preimageProvider, age, satisfiedItem, localErrors))
				{
					ret.Add(satisfiedItem);
					satisfiedN++;
				}
				else
				{
					ret.Add(dissat);
				}
				retDissatisfied.Add(dissat);
			}
			if (satisfiedN < k)
			{
				errors.Add(new SatisfyError(SatisfyErrorCode.ThresholdNotMet, this, localErrors.ToArray()));
				return false;
			}
			if (satisfiedN == k)
			{
				result.AddRange(Flatten(ret));
				return true;
			}

			// if we have more satisfactions than needed, throw away the extras, choosing
			// the ones that would yield the biggest savings.
			var indices = new List<int> { };
			for (int i = 0; i < subAsts.Length; i++)
				indices.Add(i);
			var sortedIndices = indices.OrderBy(i => SatisfyCost(retDissatisfied[i]) - SatisfyCost(retDissatisfied[i]));
			foreach (int i in sortedIndices.Take(satisfiedN - (int)k))
				ret[i] = retDissatisfied[i];
			result.AddRange(Flatten(ret));
			return true;
		}

		private bool SatisfySwitchOr(
			AstElem l, AstElem r,
			SignatureProvider signatureProvider,
			PreimageProvider preimageProvider,
			uint? age,
			List<byte[]> result,
			List<SatisfyError> errors
			)
		{
			List<byte[]> lSat = new List<byte[]>();
			List<byte[]> rSat = new List<byte[]>();
			List<SatisfyError> leftE = new List<SatisfyError>();
			List<SatisfyError> rightE = new List<SatisfyError>();
			var isLOk = l.TrySatisfy(signatureProvider, preimageProvider, age, lSat, leftE);
			var isROk = r.TrySatisfy(signatureProvider, preimageProvider, age, rSat, rightE);
			if (!isLOk && !isROk)
			{
				leftE.AddRange(rightE);
				errors.Add(new SatisfyError(SatisfyErrorCode.OrExpressionBothNotMet, this, leftE));
				return false;
			}

			else if (isLOk && !isROk)
			{
				lSat.Add(new byte[] { 1 });
				result.AddRange(lSat);
			}
			else if (!isLOk && isROk)
			{
				rSat.Add(new byte[0]);
				result.AddRange(rSat);
			}
			else
			{
				if (SatisfyCost(lSat) + 2 <= SatisfyCost(rSat) + 1)
				{
					lSat.Add(new byte[] { 1 });
					result.AddRange(lSat);
				}
				else
				{
					rSat.Add(new byte[0]);
					result.AddRange(rSat);
				}
			}
			return true;
		}

		private bool SatisfyCascadeOr(
			AstElem l, AstElem r,
			SignatureProvider signatureProvider,
			PreimageProvider preimageProvider,
			uint? age,
			List<byte[]> result,
			List<SatisfyError> errors
			)
		{
			List<byte[]> lSat = new List<byte[]>();
			List<byte[]> rSat = new List<byte[]>();
			List<SatisfyError> leftE = new List<SatisfyError>();
			List<SatisfyError> rightE = new List<SatisfyError>();
			var isLOk = l.TrySatisfy(signatureProvider, preimageProvider, age, lSat, leftE);
			var isROk = r.TrySatisfy(signatureProvider, preimageProvider, age, rSat, rightE);
			if (!isLOk && !isROk)
			{
				leftE.AddRange(rightE);
				errors.Add(new SatisfyError(SatisfyErrorCode.OrExpressionBothNotMet, this, leftE));
				return false;
			}

			else if (isLOk && !isROk)
			{
				result.AddRange(lSat);
			}
			else if (!isLOk && isROk)
			{
				var lDissat = Dissatisfy(l);
				rSat.AddRange(lDissat);
				result.AddRange(rSat);
			}
			else
			{
				var lDissat = Dissatisfy(l);
				if (SatisfyCost(lSat) <= SatisfyCost(rSat) + SatisfyCost(lDissat))
				{
					result.AddRange(lSat);
				}
				else
				{
					rSat.AddRange(lDissat);
					result.AddRange(rSat);
				}
			}
			return true;
		}

		private bool SatisfyParallelOr(
			AstElem l, AstElem r,
			SignatureProvider signatureProvider,
			PreimageProvider preimageProvider,
			uint? age,
			List<byte[]> result,
			List<SatisfyError> errors
			)
		{
			List<byte[]> lSat = new List<byte[]>();
			List<byte[]> rSat = new List<byte[]>();
			List<SatisfyError> leftE = new List<SatisfyError>();
			List<SatisfyError> rightE = new List<SatisfyError>();
			var isLOk = l.TrySatisfy(signatureProvider, preimageProvider, age, lSat, leftE);
			var isROk = r.TrySatisfy(signatureProvider, preimageProvider, age, rSat, rightE);
			if (!isLOk && !isROk)
			{
				leftE.AddRange(rightE);
				errors.Add(new SatisfyError(SatisfyErrorCode.OrExpressionBothNotMet, this, leftE));
				return false;
			}

			else if (isLOk && !isROk)
			{
				var rDissat = Dissatisfy(r);
				rDissat.AddRange(lSat);
				result.AddRange(rDissat);
			}
			else if (!isLOk && isROk)
			{
				var lDissat = Dissatisfy(l);
				rSat.AddRange(lDissat);
				result.AddRange(rSat);
			}
			else
			{
				var lDissat = Dissatisfy(l);
				var rDissat = Dissatisfy(r);
				if (SatisfyCost(lSat) + SatisfyCost(rDissat) <= SatisfyCost(rSat) + SatisfyCost(lDissat))
				{
					rDissat.AddRange(lSat);
					result.AddRange(rDissat);
				}
				else
				{
					rSat.AddRange(lDissat);
					result.AddRange(rSat);
				}
			}

			return true;
		}

		private bool SatisfyHashEqual(
			uint256 hash, PreimageProvider preimageProvider,
			List<byte[]> result,
			List<SatisfyError> errors
			)
		{
			if (preimageProvider == null)
			{
				errors.Add(new SatisfyError(SatisfyErrorCode.NoPreimageProvider, this));
				return false;
			}
			var preImage = preimageProvider(hash);
			if (preImage == null)
			{
				errors.Add(new SatisfyError(SatisfyErrorCode.CanNotProvidePreimage, this));
				return false;
			}

			result.Add(preImage.ToBytes());
			return true;
		}
		private bool SatisfyCSV(
			uint timelock, uint? age,
			List<SatisfyError> errors
			)
		{
			if (age == null)
			{
				errors.Add(new SatisfyError(SatisfyErrorCode.NoAgeProvided, this));
				return false;
			}
			if (age >= timelock)
				return true;
			else
				errors.Add(new SatisfyError(SatisfyErrorCode.LockTimeNotMet, this));
			return false;
		}
	}
}