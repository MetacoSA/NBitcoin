using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin
{
	public class BytesComparer : Comparer<byte[]>
	{

		private static readonly BytesComparer _Instance = new BytesComparer();
		public static BytesComparer Instance
		{
			get
			{
				return _Instance;
			}
		}
		private BytesComparer()
		{

		}
		public override int Compare(byte[] x, byte[] y)
		{
			var len = Math.Min(x.Length, y.Length);
			for (var i = 0; i < len; i++)
			{
				var c = x[i].CompareTo(y[i]);
				if (c != 0)
				{
					return c;
				}
			}

			return x.Length.CompareTo(y.Length);
		}
#if HAS_SPAN
		public int Compare(ReadOnlySpan<byte> x, ReadOnlySpan<byte> y)
		{
			var len = Math.Min(x.Length, y.Length);
			for (var i = 0; i < len; i++)
			{
				var c = x[i].CompareTo(y[i]);
				if (c != 0)
				{
					return c;
				}
			}

			return x.Length.CompareTo(y.Length);
		}
#endif
	}
}
