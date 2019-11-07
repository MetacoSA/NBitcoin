using System;
using NBitcoin.Scripting.Miniscript.Policy;

namespace NBitcoin.Scripting.Miniscript.Types
{
	public abstract class IProperty<T> where T: IProperty<T>
	{
		public virtual void SanityChecks() {}

		# region Casting operation
		public abstract T CastAlt();
		public abstract T CastSwap();
		public abstract T CastCheck();
		public abstract T CastDupIf();
		public abstract T CastVerify();
		public abstract T CastNonZero();
		public abstract T CastZeroNotEqual();
		public abstract T CastTrue();
		public abstract T CastOrIFalse();
		public virtual T CastUnLikely() => CastOrIFalse();
		public virtual T CastLikely() => CastOrIFalse();

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


		public abstract T AndB(T l, T r);
		public abstract T AndV(T l, T r);
		public virtual T AndN(T l, T r) => AndOr(l, r, FromFalse());
		public abstract T OrB(T left, T right);
		public abstract T OrD(T left, T right);
		public abstract T OrC(T left, T right);
		public abstract T OrI(T left, T right);
		public abstract T AndOr(T a, T b, T c);
		public abstract T Threshold(int k, int n, Func<int, T> subCk);
		#endregion
	}
}
