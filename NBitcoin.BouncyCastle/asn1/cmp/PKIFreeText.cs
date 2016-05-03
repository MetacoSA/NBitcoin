using System;
using System.Collections;

using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Asn1.Cmp
{
	public class PkiFreeText
		: Asn1Encodable
	{
		internal Asn1Sequence strings;

		public static PkiFreeText GetInstance(
			Asn1TaggedObject	obj,
			bool				isExplicit)
		{
			return GetInstance(Asn1Sequence.GetInstance(obj, isExplicit));
		}

		public static PkiFreeText GetInstance(
			object obj)
		{
			if (obj is PkiFreeText)
			{
				return (PkiFreeText)obj;
			}
			else if (obj is Asn1Sequence)
			{
				return new PkiFreeText((Asn1Sequence)obj);
			}

            throw new ArgumentException("Unknown object in factory: " + Platform.GetTypeName(obj), "obj");
		}

		public PkiFreeText(
			Asn1Sequence seq)
		{
			foreach (object o in seq)
			{
				if (!(o is DerUtf8String))
				{
					throw new ArgumentException("attempt to insert non UTF8 STRING into PkiFreeText");
				}
			}

			this.strings = seq;
		}

		public PkiFreeText(
			DerUtf8String p)
		{
			strings = new DerSequence(p);
		}

		/**
		 * Return the number of string elements present.
		 *
		 * @return number of elements present.
		 */
		[Obsolete("Use 'Count' property instead")]
		public int Size
		{
			get { return strings.Count; }
		}

		public int Count
		{
			get { return strings.Count; }
		}

		/**
		 * Return the UTF8STRING at index.
		 *
		 * @param index index of the string of interest
		 * @return the string at index.
		 */
		public DerUtf8String this[int index]
		{
			get { return (DerUtf8String) strings[index]; }
		}

		[Obsolete("Use 'object[index]' syntax instead")]
		public DerUtf8String GetStringAt(
			int index)
		{
			return this[index];
		}

		/**
		 * <pre>
		 * PkiFreeText ::= SEQUENCE SIZE (1..MAX) OF UTF8String
		 * </pre>
		 */
		public override Asn1Object ToAsn1Object()
		{
			return strings;
		}
	}
}
