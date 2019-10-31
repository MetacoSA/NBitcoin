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
		where TPk : IMiniscriptKey<TPKh>
		where TPKh : IMiniscriptKeyHash
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

	public static class PropertyExtensions
	{
		/// <summary>
		/// Compute the type of a fragment, given a function to look up
		/// the types of its children, if available and relevant for the
		/// given fragment.
		/// </summary>
		/// <param name="prop"></param>
		/// <param name="fragment"></param>
		/// <param name="child"></param>
		/// <typeparam name="T"></typeparam>
		/// <exception cref="TypeCheckException"></exception>
		/// <returns></returns>
		internal static T TypeCheck<T, TPk, TPKh>(this T prop, Terminal<TPk, TPKh> fragment, Func<int, T> child)
			where T : class, IProperty<T>
			where TPk : IMiniscriptKey<TPKh>
			where TPKh : IMiniscriptKeyHash
		{
			var res = prop.TypeCheckCore(fragment, child);
			res.SanityChecks();
			return res;
		}

		internal static T TypeCheck<T, TPk, TPKh>(this T prop, Terminal<TPk, TPKh> fragment)
			where T : class, IProperty<T>
			where TPk : IMiniscriptKey<TPKh>
			where TPKh : IMiniscriptKeyHash
			=> prop.TypeCheck(fragment, (_) => null);

		private static T TypeCheckCore<T, TPk, TPKh>(this T prop, Terminal<TPk, TPKh> fragment,Func<int, T> child)
			where T : class, IProperty<T>
			where TPk : IMiniscriptKey<TPKh>
			where TPKh : IMiniscriptKeyHash
		{
			T GetChild(Terminal<TPk, TPKh> sub, int n)
				{
					try
					{
						return child(n);
					}
					catch
					{
						return prop.TypeCheck(sub, _ => null);
					}
				}
			switch (fragment.Tag)
			{
				case Terminal<TPk, TPKh>.Tags.True:
					return prop.FromTrue();
				case Terminal<TPk, TPKh>.Tags.False:
					return prop.FromFalse();
			}

			switch (fragment)
			{
				case Terminal<TPk, TPKh>.Pk self:
					return prop.FromPk();
				case Terminal<TPk, TPKh>.PkH self:
					return prop.FromPkH();
				case Terminal<TPk, TPKh>.ThreshM self:
					if (self.Item1 == 0)
					{
						throw new TypeCheckException<TPk, TPKh>(fragment, ErrorKind.ZeroThreshold);
					}

					if (self.Item1 > self.Item2.Length)
					{
						throw new TypeCheckException<TPk, TPKh>(
							fragment,
							ErrorKind.OverThreshold,
							new int[] {self.Item1, self.Item2.Length});
					}
					return prop.FromMulti(self.Item1, self.Item2.Length);
				case Terminal<TPk, TPKh>.After self:
					if (self.Item == 0)
					{
						throw new TypeCheckException<TPk, TPKh>(fragment, ErrorKind.ZeroTime);
					}

					return prop.FromAfter(self.Item);
				case Terminal<TPk, TPKh>.Older self:
					if (self.Item == 0)
					{
						throw new TypeCheckException<TPk, TPKh>(fragment, ErrorKind.ZeroTime);
					}

					return prop.FromOlder(self.Item);
				case Terminal<TPk, TPKh>.Sha256 self:
					return prop.FromSha256();
				case Terminal<TPk, TPKh>.Hash256 self:
					return prop.FromHash256();
				case Terminal<TPk, TPKh>.Ripemd160 self:
					return prop.FromRipemd160();
				case Terminal<TPk, TPKh>.Alt self:
					return GetChild(self.Item.Node, 0).CastAlt();
				case Terminal<TPk, TPKh>.Swap self:
					return GetChild(self.Item.Node, 0).CastSwap();
				case Terminal<TPk, TPKh>.Check self:
					return GetChild(self.Item.Node, 0).CastCheck();
				case Terminal<TPk, TPKh>.DupIf self:
					return GetChild(self.Item.Node, 0).CastDupIf();
				case Terminal<TPk, TPKh>.Verify self:
					return GetChild(self.Item.Node, 0).CastVerify();
				case Terminal<TPk, TPKh>.NonZero self:
					return GetChild(self.Item.Node, 0).CastNonZero();
				case Terminal<TPk, TPKh>.AndB self:
					var andBL = GetChild(self.Item1.Node, 0);
					var andBR = GetChild(self.Item2.Node, 1);
					return prop.AndB(andBL, andBR);
				case Terminal<TPk, TPKh>.AndV self:
					var andVL = GetChild(self.Item1.Node, 0);
					var andVR = GetChild(self.Item2.Node, 1);
					return prop.AndV(andVL, andVR);
				case Terminal<TPk, TPKh>.OrB self:
					var orBL = GetChild(self.Item1.Node, 0);
					var orBR = GetChild(self.Item2.Node, 1);
					return prop.OrB(orBL, orBR);
				case Terminal<TPk, TPKh>.OrD self:
					var orDL = GetChild(self.Item1.Node, 0);
					var orDR = GetChild(self.Item2.Node, 1);
					return prop.OrD(orDL, orDR);
				case Terminal<TPk, TPKh>.OrC self:
					var orCL = GetChild(self.Item1.Node, 0);
					var orCR = GetChild(self.Item2.Node, 1);
					return prop.OrC(orCL, orCR);
				case Terminal<TPk, TPKh>.OrI self:
					var orIL = GetChild(self.Item1.Node, 0);
					var orIR = GetChild(self.Item2.Node, 1);
					return prop.OrC(orIL, orIR);
				case Terminal<TPk, TPKh>.AndOr self:
					var a = GetChild(self.Item1.Node, 0);
					var b = GetChild(self.Item2.Node, 1);
					var c = GetChild(self.Item3.Node, 2);
					return prop.AndOr(a, b, c);
				case Terminal<TPk, TPKh>.Thresh self:
					if (self.Item1 == 0)
					{
						throw new TypeCheckException<TPk, TPKh>(fragment, ErrorKind.ZeroThreshold);
					}

					if (self.Item1 > self.Item2.Length)
					{
						throw new TypeCheckException<TPk, TPKh>(fragment, ErrorKind.OverThreshold);
					}
					return
						prop.Threshold(self.Item1, self.Item2.Length, (n) =>
							GetChild(self.Item2[n].Node, (int)n));
			}
			throw new Exception("Unreachable!");;
		}
	}
}
