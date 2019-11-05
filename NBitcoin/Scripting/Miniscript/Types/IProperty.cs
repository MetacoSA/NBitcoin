using System;
using System.Linq;
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

	public enum ErrorKind
	{
		/// <summary>
		/// Relative or absolute timelock had a time value of 0;
		/// </summary>
		ZeroTime,

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
		/// Insufficiently many children of a threshold fragment were strong
		/// </summary>
		ThresholdNotStrong
	}

	internal enum CastErrorKind
	{

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
		/// Tried to use the `s:` modifier on a fragment that takes more
		/// than one input.
		/// </summary>
		SwapNonOne,

		/// <summary>
		/// Passed a `z` argument to a `d` wrapper when `z` was expected
		/// </summary>
		NonZeroDupIf,

		/// <summary>
		/// Tried to use `s:` modifier on a fragment that takes more than one input
		/// </summary>
		NonZeroZero,

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
		/// Many fragments require their left child to be a unit. This
		/// was not the case.
		/// </summary>
		LeftNotUnit,

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

	}

	internal class CastException : InvalidCastException
	{
		internal CastErrorKind Kind;

		private CastException(CastErrorKind e, string msg) : base(
			$"Failed to cast. kind {e.ToString("G")}. msg: {msg} ")
		{
			Kind = e;
		}

		public static CastException LeftNotUnit =
			new CastException(CastErrorKind.LeftNotUnit, "this fragment requires left to be unit. But it was not.");

		public static CastException ChildBase1(Base b) =>
			new CastException(CastErrorKind.ChildBase1, $"Attempted to construct a wrapper. But the child had an invalid type {b.ToString("G")}");
		public static CastException ChildBase2(Base l, Base r) =>
			new CastException(CastErrorKind.ChildBase2, $"Attempted to construct a wrapper. But the child had an invalid type. left: {l.ToString("G")}. right: {r.ToString("G")}");

		public static CastException ChildBase3(Base a, Base b, Base c) =>
			new CastException(CastErrorKind.ChildBase3, $"Attempted to construct a wrapper. But the child had an invalid type. a: {a.ToString("G")}. b: {b.ToString("G")}. c: {c.ToString("G")}");

		public static CastException SwapNoneOne =
			new CastException(CastErrorKind.SwapNonOne, "Tried to use the s: modifier for the fragment takes more than one output.");

		public static CastException NonZeroDupIf =
			new CastException(CastErrorKind.NonZeroDupIf, $"Passed a z argument to a d wrapper when `z` was expected.");

		public static CastException NonZeroZero =
			new CastException(CastErrorKind.NonZeroZero, "Tried to use `s:` modifier on a fragment that takes more than one input");
		public static CastException LeftNotDissatisfiable =
			new CastException(CastErrorKind.LeftNotDissatisfiable, "Left child must be dissatisfiable");
		public static CastException RightNotDissatisfiable =
			new CastException(CastErrorKind.RightNotDissatisfiable, "Right child must be dissatisfiable.");

		public static CastException ThresholdBase(uint i, Base b) =>
			new CastException(CastErrorKind.ThresholdBase,
				$"the {i}th value of the threshold fragment had an invalid type (first must be `B` and rest must be `W`.)");

		public static CastException ThresholdNonUnit(uint n) =>
			new CastException(CastErrorKind.ThresholdNonUnit, "the {n} th fragment of the threshold was not the unit");

		public static CastException ThresholdDissat(uint n) =>
			new CastException(CastErrorKind.ThresholdDissat, $"the {n}th child of the threshold did not have unique satisfaction");
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
