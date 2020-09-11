#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Secp256k1
{
#if SECP256K1_LIB
	public
#endif
	class Context
	{
		static readonly Lazy<Context> _Instance = new Lazy<Context>(CreateInstance, true);
		static Context CreateInstance()
		{
			return new Context();
		}
		public static Context Instance => _Instance.Value;

		public ECMultContext EcMultContext { get; }
		public ECMultGenContext EcMultGenContext { get; }

		public Context() : this(null, null)
		{
			
		}
		public Context(ECMultContext? ctx, ECMultGenContext? genCtx)
		{
			EcMultContext = ctx ?? ECMultContext.Instance;
			EcMultGenContext = genCtx ?? ECMultGenContext.Instance;
		}

		public ECPrivKey CreateECPrivKey(in Scalar scalar)
		{
			return new ECPrivKey(scalar, this, true);
		}
		public ECPrivKey CreateECPrivKey(ReadOnlySpan<byte> b32)
		{
			return new ECPrivKey(b32, this);
		}
		public bool TryCreateECPrivKey(ReadOnlySpan<byte> b32, out ECPrivKey? key)
		{
			var s = new Scalar(b32, out var overflow);
			if (overflow != 0 || s.IsZero)
			{
				key = null;
				return false;
			}
			key = new ECPrivKey(s, this, false);
			return true;
		}
		public bool TryCreateECPrivKey(in Scalar s, out ECPrivKey? key)
		{
			if (s.IsOverflow || s.IsZero)
			{
				key = null;
				return false;
			}
			key = new ECPrivKey(s, this, false);
			return true;
		}

		public bool TryCreateXOnlyPubKey(ReadOnlySpan<byte> input32, out ECXOnlyPubKey? pubkey)
		{
			return ECXOnlyPubKey.TryCreate(input32, this, out pubkey);
		}

		public ECXOnlyPubKey CreateXOnlyPubKey(ReadOnlySpan<byte> input32)
		{
			if (!TryCreateXOnlyPubKey(input32, out var pubkey) || pubkey is null)
				throw new FormatException("Invalid xonly pubkey");
			return pubkey;
		}

		public bool TryCreatePubKey(ReadOnlySpan<byte> input, out ECPubKey? pubkey)
		{
			return ECPubKey.TryCreate(input, this, out _, out pubkey);
		}
		public bool TryCreatePubKey(ReadOnlySpan<byte> input, out bool compressed, out ECPubKey? pubkey)
		{
			return ECPubKey.TryCreate(input, this, out compressed, out pubkey);
		}
		public bool TryCreatePrivKeyFromDer(ReadOnlySpan<byte> input, out ECPrivKey? privkey)
		{
			return ECPrivKey.TryCreateFromDer(input, this, out privkey);
		}
	}
}
#nullable restore
#endif
