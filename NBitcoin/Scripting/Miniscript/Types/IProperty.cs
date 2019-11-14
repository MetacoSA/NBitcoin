using System;
using System.Collections.Generic;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Scripting.Miniscript.Types
{
	internal abstract class IProperty<T> where T: IProperty<T>
	{
		public virtual void SanityChecks() {}

		# region Casting operation
		public abstract bool TryCastAlt(out T result, List<FragmentPropertyException> error);
		public abstract bool TryCastSwap(out T result, List<FragmentPropertyException> error);
		public abstract bool TryCastCheck(out T result, List<FragmentPropertyException> error);
		public abstract bool TryCastDupIf(out T result, List<FragmentPropertyException> error);
		public abstract bool TryCastVerify(out T result, List<FragmentPropertyException> error);
		public abstract bool TryCastNonZero(out T result, List<FragmentPropertyException> error);
		public abstract bool TryCastZeroNotEqual(out T result, List<FragmentPropertyException> error);
		public abstract bool TryCastTrue(out T result, List<FragmentPropertyException> error);
		public abstract bool TryCastOrIFalse(out T result, List<FragmentPropertyException> error);
		public virtual bool TryCastLikely(out T result, List<FragmentPropertyException> error) => TryCastOrIFalse(out result, error);
		public virtual bool TryCastUnLikely(out T result, List<FragmentPropertyException> error) => TryCastOrIFalse(out result, error);

		#endregion

		#region Constructors

		// Technically These should work as static methods. But since there are no
		// `abstract static` in C#, we create empty instance first and call these methods
		// against those instance.
		public abstract T FromTrue();
		public abstract T FromFalse();
		public abstract T FromPk();
		public abstract T FromPkH();
		public abstract T FromMulti(int k, int pkLength);
		public abstract T FromHash();
		public virtual T FromSha256() => this.FromHash();
		public virtual T FromHash256() => this.FromHash();
		public virtual T FromRipemd160() => this.FromHash();
		public virtual T FromHash160() => this.FromHash();

		public abstract T FromTime(uint time);
		public virtual T FromAfter(uint time) => this.FromTime(time);
		public virtual T FromOlder(uint time) => this.FromTime(time);


		public abstract bool TryAndB(T l, T r, out T result, List<FragmentPropertyException> error);
		public abstract bool TryAndV(T l, T r, out T result, List<FragmentPropertyException> error);
		public virtual bool TryAndN(T l, T r, out T result, List<FragmentPropertyException> error)
			=> TryAndOr(l, r, FromFalse(), out result, error);

		public abstract bool TryOrB(T l, T r, out T result, List<FragmentPropertyException> error);
		public abstract bool TryOrD(T l, T r, out T result, List<FragmentPropertyException> error);
		public abstract bool TryOrC(T l, T r, out T result, List<FragmentPropertyException> error);
		public abstract bool TryOrI(T l, T r, out T result, List<FragmentPropertyException> error);
		public abstract bool TryAndOr(T a, T b, T c, out T result, List<FragmentPropertyException> error);
		public abstract bool TryThreshold(int k, int n, Func<int, T> subCk, out T result, List<FragmentPropertyException> error);
		#endregion
	}
}
