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
#if SECP256K1_LIB
		public
#else
		internal
#endif
		readonly GE K1, K2;

		internal MusigPubNonce(ECPubKey k1, ECPubKey k2)
		{
			this.K1 = k1.Q;
			this.K2 = k2.Q;
		}

		internal MusigPubNonce(GE k1, GE k2)
		{
			this.K1 = k1.IsInfinity ? k1 : k1.NormalizeYVariable();
			this.K2 = k2.IsInfinity ? k2 : k2.NormalizeYVariable();
		}

		public static MusigPubNonce Aggregate(MusigPubNonce[] nonces)
		{
			if (nonces == null)
				throw new ArgumentNullException(nameof(nonces));
			if (nonces.Length is 0)
				throw new ArgumentException(nameof(nonces), "nonces should have at least one element");
			Span<GEJ> summed_nonces = stackalloc GEJ[2];
			summed_nonces[0] = GEJ.Infinity;
			summed_nonces[1] = GEJ.Infinity;

			for (int i = 0; i < nonces.Length; i++)
			{
				summed_nonces[0] = summed_nonces[0].AddVariable(nonces[i].K1);
				summed_nonces[1] = summed_nonces[1].AddVariable(nonces[i].K2);
			}
			return new MusigPubNonce(summed_nonces[0].ToGroupElement(),
									 summed_nonces[1].ToGroupElement());
		}

		public MusigPubNonce(ReadOnlySpan<byte> in66)
		{
			if (!TryParseGE(in66.Slice(0, 33), out var k1) ||
				!TryParseGE(in66.Slice(33, 33), out var k2))
			{
				throw new FormatException("Invalid musig pubnonce");
			}
			this.K1 = k1;
			this.K2 = k2;
		}

		private bool TryParseGE(ReadOnlySpan<byte> pub , out GE ge)
		{
			if (GE.TryParse(pub, out _, out ge))
				return true;
			if (pub.Length == 33 && pub.SequenceCompareTo(stackalloc byte[33]) == 0)
			{
				ge = GE.Infinity;
				return true;
			}
			return false;
		}

		public void WriteToSpan(Span<byte> out66)
		{
			if (K1.IsInfinity)
				out66.Slice(0, 33).Fill(0);
			else
				new ECPubKey(K1, null).WriteToSpan(true, out66.Slice(0, 33), out _);
			if (K2.IsInfinity)
				out66.Slice(33, 33).Fill(0);
			else
				new ECPubKey(K2, null).WriteToSpan(true, out66.Slice(33, 33), out _);
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
