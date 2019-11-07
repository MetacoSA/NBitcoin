using System;
using System.Linq;

namespace NBitcoin.Scripting.Miniscript.Types
{

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
		ThresholdNotStrong,

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
	class FragmentPropertyException : Exception
	{
		public readonly ErrorKind Kind;
		public FragmentPropertyException(string msg, Exception innerException): base(msg, innerException) {}

		internal FragmentPropertyException(ErrorKind kind, string msg, string fragment = null) : base($"Error in {fragment}\n {msg} . Kind:{kind.ToString("G")}")
		{
			Kind = kind;
		}

		public static FragmentPropertyException LeftNotUnit(string fragment = null) =>
			new FragmentPropertyException (ErrorKind.LeftNotUnit, "this fragment requires left to be unit. But it was not.", fragment);

		public static FragmentPropertyException  ChildBase1(Base b, string fragment = null) =>
			new FragmentPropertyException (ErrorKind.ChildBase1, $"Attempted to construct a wrapper. But the child had an invalid type {b.ToString("G")}", fragment);
		public static FragmentPropertyException  ChildBase2(Base l, Base r, string fragment = null) =>
			new FragmentPropertyException (
				ErrorKind.ChildBase2,
				$"Attempted to construct a wrapper. But the child had an invalid type. left: {l.ToString("G")}. right: {r.ToString("G")}",
				fragment
				);

		public static FragmentPropertyException  ChildBase3(Base a, Base b, Base c, string fragment = null) =>
			new FragmentPropertyException (ErrorKind.ChildBase3, $"Attempted to construct a wrapper. But the child had an invalid type. a: {a.ToString("G")}. b: {b.ToString("G")}. c: {c.ToString("G")}", fragment);

		public static FragmentPropertyException  SwapNoneOne(string fragment = null) =>
			new FragmentPropertyException (ErrorKind.SwapNonOne, "Tried to use the s: modifier for the fragment takes more than one output.", fragment);

		public static FragmentPropertyException  NonZeroDupIf(string fragment = null) =>
			new FragmentPropertyException (ErrorKind.NonZeroDupIf, $"Passed a z argument to a d wrapper when `z` was expected.", fragment);

		public static FragmentPropertyException  NonZeroZero(string fragment = null) =>
			new FragmentPropertyException (ErrorKind.NonZeroZero, "Tried to use `s:` modifier on a fragment that takes more than one input", fragment);
		public static FragmentPropertyException  LeftNotDissatisfiable(string fragment = null) =>
			new FragmentPropertyException (ErrorKind.LeftNotDissatisfiable, "Left child must be dissatisfiable", fragment);
		public static FragmentPropertyException  RightNotDissatisfiable(string fragment = null) =>
			new FragmentPropertyException (ErrorKind.RightNotDissatisfiable, "Right child must be dissatisfiable.", fragment);

		public static FragmentPropertyException  ThresholdBase(uint i, Base b, string fragment = null) =>
			new FragmentPropertyException (ErrorKind.ThresholdBase,
				$"the {i}th value of the threshold fragment had an invalid type (first must be `B` and rest must be `W`.)", fragment);

		public static FragmentPropertyException  ThresholdNonUnit(uint n, string fragment = null) =>
			new FragmentPropertyException (ErrorKind.ThresholdNonUnit, $"the {n} th fragment of the threshold was not the unit", fragment);

		public static FragmentPropertyException  ThresholdDissat(uint n, string fragment = null) =>
			new FragmentPropertyException(ErrorKind.ThresholdDissat, $"the {n}th child of the threshold did not have unique satisfaction", fragment);

		public static FragmentPropertyException ZeroThreshold(string fragment = null) =>
			new FragmentPropertyException(ErrorKind.ZeroThreshold, "Multisignature or threshold policy had a `k` value of 0", fragment);

		public static FragmentPropertyException OverThreshold(string fragment = null) =>
			new FragmentPropertyException(
				ErrorKind.OverThreshold,
				"Multisignature or threshold policy has a `k` value in excess of the number of sub-fragments",
				fragment);
		public static FragmentPropertyException ZeroTime(string fragment = null) =>
			new FragmentPropertyException(
				ErrorKind.ZeroTime,
				"Relative or absolute timelock had a time value of 0",
				fragment);
	}
}
