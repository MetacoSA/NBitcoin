#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Secp256k1.Musig
{
#if SECP256K1_LIB
	public
#else
	internal
#endif
	class MusigPubNonce
	{
		private ECPubKey k1;
		private ECPubKey k2;

#if SECP256K1_LIB
		public
#else
	    internal
#endif
		ECPubKey K1 => k1;
#if SECP256K1_LIB
		public
#else
	    internal
#endif
		ECPubKey K2 => k2;

		internal Context context;
		internal MusigPubNonce(ECPubKey k1, ECPubKey k2)
		{
			this.k1 = k1;
			this.k2 = k2;
			this.context = k1.ctx;
		}

		public static MusigPubNonce Combine(MusigPubNonce[] nonces)
		{
			if (nonces == null)
				throw new ArgumentNullException(nameof(nonces));
			if (nonces.Length is 0)
				throw new ArgumentException(nameof(nonces), "nonces should have at least one element");
			Span<GEJ> summed_nonces = stackalloc GEJ[2];
			secp256k1_musig_sum_nonces(summed_nonces, nonces);
			for (int i = 0; i < 2; i++)
			{
				if (summed_nonces[i].IsInfinity)
					throw new InvalidOperationException("Impossible to combine the given nonces");
			}
			var ctx = nonces[0].context;
			return new MusigPubNonce(new ECPubKey(summed_nonces[0].ToGroupElement(), ctx),
									 new ECPubKey(summed_nonces[1].ToGroupElement(), ctx));
		}

		internal static void secp256k1_musig_sum_nonces(Span<GEJ> summed_nonces, MusigPubNonce[] pubnonces)
		{
			int i;
			summed_nonces[0] = GEJ.Infinity;
			summed_nonces[1] = GEJ.Infinity;

			for (i = 0; i < pubnonces.Length; i++)
			{
				summed_nonces[0] = summed_nonces[0].AddVariable(pubnonces[i].k1.Q);
				summed_nonces[1] = summed_nonces[1].AddVariable(pubnonces[i].k2.Q);
			}
		}

		public MusigPubNonce(Context? context, ReadOnlySpan<byte> in66)
		{
			if (!ECPubKey.TryCreate(in66.Slice(0, 33), context, out _, out var k1) ||
				!ECPubKey.TryCreate(in66.Slice(33, 33), context, out _, out var k2))
				throw new FormatException("Invalid musig pubnonce");
			this.context = context ?? Context.Instance;
			this.k1 = k1;
			this.k2 = k2;
		}

		public void WriteToSpan(Span<byte> out66)
		{
			k1.WriteToSpan(true, out66.Slice(0, 33), out _);
			k2.WriteToSpan(true, out66.Slice(33, 33), out _);
		}

		public byte[] ToBytes()
		{
			byte[] b = new byte[66];
			WriteToSpan(b);
			return b;
		}
	}
}
#endif
