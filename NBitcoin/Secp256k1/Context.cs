#if HAS_SPAN
#nullable enable
using NBitcoin.Secp256k1.Musig;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		public bool TryCreateECPrivKey(ReadOnlySpan<byte> b32, [MaybeNullWhen(false)] out ECPrivKey key)
		{
			return ECPrivKey.TryCreate(b32, this, out key);
		}
		public bool TryCreateECPrivKey(in Scalar s, [MaybeNullWhen(false)] out ECPrivKey key)
		{
			return ECPrivKey.TryCreate(s, this, out key);
		}

		public bool TryCreateXOnlyPubKey(ReadOnlySpan<byte> input32, [MaybeNullWhen(false)] out ECXOnlyPubKey pubkey)
		{
			return ECXOnlyPubKey.TryCreate(input32, this, out pubkey);
		}

		public ECXOnlyPubKey CreateXOnlyPubKey(ReadOnlySpan<byte> input32)
		{
			if (!TryCreateXOnlyPubKey(input32, out var pubkey) || pubkey is null)
				throw new FormatException("Invalid xonly pubkey");
			return pubkey;
		}

		public ECPubKey CreatePubKey(ReadOnlySpan<byte> input)
		{
			return ECPubKey.Create(input, this);
		}
		public bool TryCreatePubKey(ReadOnlySpan<byte> input, [MaybeNullWhen(false)] out ECPubKey pubkey)
		{
			return ECPubKey.TryCreate(input, this, out _, out pubkey);
		}
		public bool TryCreatePubKey(ReadOnlySpan<byte> input, out bool compressed, [MaybeNullWhen(false)] out ECPubKey pubkey)
		{
			return ECPubKey.TryCreate(input, this, out compressed, out pubkey);
		}
		public bool TryCreatePrivKeyFromDer(ReadOnlySpan<byte> input, [MaybeNullWhen(false)] out ECPrivKey privkey)
		{
			return ECPrivKey.TryCreateFromDer(input, this, out privkey);
		}
	}
}
#nullable restore
#endif
