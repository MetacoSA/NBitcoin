#if HAS_SPAN
#nullable enable
using NBitcoin.Secp256k1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
	public class TaprootFullPubKey : TaprootPubKey
	{
		public static TaprootFullPubKey Create(TaprootInternalPubKey internalKey, uint256? merkleRoot)
		{
			if (internalKey is null)
				throw new ArgumentNullException(nameof(internalKey));
			Span<byte> tweak32 = stackalloc byte[32];
			ComputeTapTweak(internalKey, merkleRoot, tweak32);
			var outputKey = internalKey.pubkey.AddTweak(tweak32).ToXOnlyPubKey(out var parity);
			return new TaprootFullPubKey(outputKey, parity, internalKey, merkleRoot);
		}

		private TaprootFullPubKey(ECXOnlyPubKey outputKey, bool outputParity, TaprootInternalPubKey internalKey, uint256? merkleRoot) : base(outputKey)
		{
			OutputKey = new TaprootPubKey(outputKey);
			OutputKeyParity = outputParity;
			InternalKey = internalKey;
			MerkleRoot = merkleRoot;
		}

		internal static void ComputeTapTweak(TaprootInternalPubKey internalKey, uint256? merkleRoot, Span<byte> tweak32)
		{
			using Secp256k1.SHA256 sha = new Secp256k1.SHA256();
			sha.InitializeTagged("TapTweak");
			internalKey.pubkey.WriteToSpan(tweak32);
			sha.Write(tweak32);
			if (merkleRoot is uint256)
			{
				merkleRoot.ToBytes(tweak32);
				sha.Write(tweak32);
			}
			sha.GetHash(tweak32);
		}

		public bool OutputKeyParity { get; }
		public uint256? MerkleRoot { get; }
		public TaprootInternalPubKey InternalKey { get; }
		public TaprootPubKey OutputKey { get; }

		public bool CheckTapTweak(TaprootInternalPubKey internalPubKey)
		{
			return this.CheckTapTweak(internalPubKey, MerkleRoot, OutputKeyParity);
		}
	}
}
#endif
