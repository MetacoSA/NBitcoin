#if !NO_RECORDS
#nullable enable
using NBitcoin.Crypto;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Scripting
{
	public record SatisfactionRequirement
	{
		public static Preimage Create(MiniscriptNode.Value.HashValue hashValue, FragmentDescriptor descriptor) => new Preimage(hashValue, descriptor);
		public static Signature CreateSignature(MiniscriptNode.Value.PubKeyValue pubkeyValue, FragmentDescriptor descriptor) => new Signature(pubkeyValue, descriptor);

		public record Signature(MiniscriptNode.Value.PubKeyValue PubKeyValue, FragmentDescriptor Descriptor) : SatisfactionRequirement
		{
		}
		public record Preimage(MiniscriptNode.Value.HashValue HashValue, FragmentDescriptor HashDescriptor) : SatisfactionRequirement
		{
			public bool Verify(byte[] preimage)
			{
				var hash = CalculateHash(preimage);
				return StructuralComparisons.StructuralEqualityComparer.Equals(hash, HashValue.Hash);
			}
			public byte[] CalculateHash(byte[] preimage)
			{
				if (HashDescriptor == FragmentDescriptor.sha256)
					return Hashes.SHA256(preimage);
				if (HashDescriptor == FragmentDescriptor.hash256)
					return Hashes.DoubleSHA256RawBytes(preimage, 0, preimage.Length);
				if (HashDescriptor == FragmentDescriptor.ripemd160)
					return Hashes.RIPEMD160(preimage);
				if (HashDescriptor == FragmentDescriptor.hash160)
					return Hashes.Hash160RawBytes(preimage, 0, preimage.Length);
				else
					throw new NotSupportedException(HashDescriptor.Name);
			}
		}
	}
}
#endif
