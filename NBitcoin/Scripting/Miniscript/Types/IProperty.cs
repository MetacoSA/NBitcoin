using System;
using System.Linq;

namespace NBitcoin.Scripting.Miniscript.Types
{
	public interface IProperty<T> where T: IProperty<T>
	{
		void SanityChecks();
		T FromTrue();
		T FromFalse();
		T FromPk();
		T FromPkH();
		T FromMulti(int k, int pkLength);
		T FromAfter(uint time);
		T FromOlder(uint time);
		T FromHash();
		T FromSha256();
		T FromHash256();
		T FromRipemd160();
		T FromHash160();

		T CastAlt();
		T CastSwap();
		T CastCheck();
		T CastDupIf();
		T CastVerify();
		T CastNonZero();
		T CastZeroNotEqual();
		T CastTrue();
		T CastOrIFalse();
		T CastUnLikely();
		T CastLikely();
		T AndB(T left, T right);
		T AndV(T left, T right);
		T AndN(T left, T right);
		T OrB(T left, T right);
		T OrD(T left, T right);
		T OrC(T left, T right);
		T OrI(T left, T right);
		T AndOr(T a, T b, T c);
		T Threshold(int k, int n, Func<uint, T> subCk);
	}

	public enum ErrorKind
	{
		/// <summary>
		/// Relative or absolute timelock had a time value of 0;
		/// </summary>
		ZeroTime,

		/// <summary>
		/// Passed a `z` argument to a `d` wrapper when `z` was expected
		/// </summary>
		NonZeroDupIf,

		/// <summary>
		/// Multisignature or threshold policy had a `k` value of 0
		/// </summary>
		ZeroThreshold,

		/// <summary>
		/// Multisignature or threshold policy has a `k` value in excess of the number of
		/// sub-fragments
		/// </summary>
		OverThreshold,

		/// <summary>
		/// Attempted to construct a disjunction (or `andor`) for which none of the child node were strong
		/// This means that a 3rd party could produce a satisfaction for any branch, meaning
		/// that no matter which one an honest signer chooses, it is possible to malleate the transaction.
		/// </summary>
		NoStrongChild,

		/// <summary>
		/// Many fragments (all disjunctions except `or_i` as well as
		/// `andor`) require their left child be dissatisfiable.
		/// </summary>
		LeftNotDissatisfiable,

		/// <summary>
		/// `or_b` requires its right child be dissatisfiable
		/// </summary>
		RightNotDissatisfiable,

		/// <summary>
		/// Tried to use the `s:` modifier on a fragment that takes more
		/// than one input.
		/// </summary>
		SwapNonOne,
		/// <summary>
		/// Tried to use `s:` modifier on a fragment that takes more than one input
		/// </summary>
		NonZeroZero,

		/// <summary>
		/// Many fragments require their left child to be a unit. This
		/// was not the case.
		/// </summary>
		LeftNotUnit,

		/// <summary>
		/// Attempted to construct a wrapper, but the child had an invalid type
		/// </summary>
		ChildBase1,

		/// <summary>
		/// Attempted to construct a conjunction or disjunction
		/// </summary>
		ChildBase2,

		/// <summary>
		/// Attempted to construct an `andor` but the fragments
		/// children were of invalid types
		/// </summary>
		ChildBase3,

		/// <summary>
		/// The nth child of a threshold fragment had an invalid type (the
		/// first must be `B` and the rest `W`s)
		/// </summary>
		ThresholdBase,

		/// <summary>
		/// The nth child of a threshold fragment did not have a unique satisfaction
		/// </summary>
		ThresholdDissat,

		/// <summary>
		/// The nth child of a threshold fragment did not a unit.
		/// </summary>
		ThresholdNonUnit,

		/// <summary>
		/// Insufficiently many children of a threshold fragment were strong
		/// </summary>
		ThresholdNotStrong
	}

	class TypeCheckException<TPk, TPKh> : Exception
		where TPk : class, IMiniscriptKey<TPKh>, new()
		where TPKh : class, IMiniscriptKeyHash, new()
	{
		public readonly Terminal<TPk, TPKh> Fragment;
		public readonly ErrorKind Kind;
		public readonly int[] Items;
		public TypeCheckException(Terminal<TPk, TPKh> fragment, ErrorKind kind)
			: this(fragment, kind, new int[] { })
		{}

		public TypeCheckException(Terminal<TPk, TPKh> fragment, ErrorKind kind, int[] items):
			base($"Error: {kind.ToString("G")}. Fragment: {fragment}, items: {items.Select(i => i.ToString()).Aggregate((str, i) => $"{str}, {i}")}")

		{
			Fragment = fragment;
			Kind = kind;
			Items = items;
		}

		public TypeCheckException(string msg): base(msg) {}
		public TypeCheckException(string msg, Exception innerException): base(msg, innerException) {}
	}

}
