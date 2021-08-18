using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NBitcoin.Altcoins.Elements
{
#pragma warning disable CS0618 // Type or member is obsolete

	public class DynaFedParamEntry:IBitcoinSerializable
	{
		// Determines how these entries are serialized and stored
		// 0 -> Null. Only used for proposed parameter "null votes"
		// 1 -> Pruned. Doesn't have non-signblockscript data. That elided data
		// is committed to in m_elided_root, and validated against chainstate.
		// 2 -> Full. Typically only consensus-legal at epoch start.
		byte SerializeType; // Determines how it is serialized, defaults to null
		Script SignBlockScript = new Script();
		uint SignBlockWitnessLimit; // Max block signature witness serialized size
		Script FedPegProgram = new Script(); // The "scriptPubKey" of the fedpegscript
		Script FedPegScript = new Script(); // The witnessScript for witness v0 or undefined otherwise.
		// No consensus meaning to the particular bytes, currently we interpret as PAK keys, details in pak.h
		List<byte[]> ExtensionSpace = new List<byte[]>();
		uint256 ElidedRoot; // non-zero only when m_serialize_type == 1
		public bool IsEmpty =>
			SerializeType == 0 && SignBlockScript == Script.Empty && SignBlockWitnessLimit == 0 &&
			FedPegProgram == Script.Empty && FedPegScript == Script.Empty && !ExtensionSpace.Any();


		public void ReadWrite(BitcoinStream stream)
		{
				stream.ReadWrite(ref SerializeType);
				switch (SerializeType)
				{
					case 0:
						/* Null entry, used to signal "no vote" proposal */
						break;
					case 1:
						stream.ReadWrite(ref SignBlockScript);
						stream.ReadWrite(ref SignBlockWitnessLimit);
						stream.ReadWrite(ref ElidedRoot);
						break;
					case 2:
						stream.ReadWrite(ref SignBlockScript);
						stream.ReadWrite(ref SignBlockWitnessLimit);
						stream.ReadWrite(ref FedPegProgram);
						stream.ReadWrite(ref FedPegScript);
						stream.ReadWriteListBytes(ref ExtensionSpace);
						break;
					default:
						throw new FormatException("Invalid consensus parameter entry type");
				}
		}
	}
	public class DynaFedParams:IBitcoinSerializable
	{
		public DynaFedParamEntry Current = new DynaFedParamEntry();
		public DynaFedParamEntry Proposed= new DynaFedParamEntry();

		public bool IsNull => Current.IsEmpty && Proposed.IsEmpty;

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref Current);
			stream.ReadWrite(ref Proposed);

		}
	}

	public class ElementsBlock<TNetwork> : Block
	{
		public ElementsBlock(ElementsBlockHeader<TNetwork> header) : base(header)
		{
		}
		public override ConsensusFactory GetConsensusFactory()
		{
			return ElementsConsensusFactory<TNetwork>.Instance;
		}
	}

	public class ElementsBlockHeader<TNetwork> : BlockHeader
	{
		// HF bit to detect dynamic federation blocks
		private  const int DYNAFED_HF_MASK = 1 << 31;


		private void Serialize(BitcoinStream stream)
		{
			var fAllowWitness = stream.TransactionOptions.HasFlag(TransactionOptions.Witness) && stream.ProtocolCapabilities.SupportWitness;
			// Detect dynamic federation block serialization using "HF bit",
			// or the signed bit which is invalid in Bitcoin
			var isDynamic = false;
			int nVersion = this.nVersion;
			if (!DynaFedParams.IsNull)
			{
				nVersion |= DYNAFED_HF_MASK;
				isDynamic = true;
			}

			stream.ReadWrite(ref nVersion);

			if (isDynamic)
			{
				stream.ReadWrite(ref hashPrevBlock);
				stream.ReadWrite(ref hashMerkleRoot);
				stream.ReadWrite(ref nTime);
				stream.ReadWrite(ref _nHeight);
				stream.ReadWrite(ref DynaFedParams);
				// We do not serialize witness for hashes, or weight calculation
				if (stream.Type != SerializationType.Hash && fAllowWitness)
				{
					SignBlockWitness.WriteToStream(stream);
				}
			}
			else
			{

				stream.ReadWrite(ref hashPrevBlock);
				stream.ReadWrite(ref hashMerkleRoot);
				stream.ReadWrite(ref nTime);
				if (ElementsParams<TNetwork>.BlockHeightInHeader)
				{
					stream.ReadWrite(ref _nHeight);
				}

				if (ElementsParams<TNetwork>.SignedBlocks)
				{
					stream.ReadWrite(ref _Proof);
				}
				else
				{
					stream.ReadWrite(ref nBits);
					stream.ReadWrite(ref nNonce);
				}
			}
		}

		private void UnSerialize(BitcoinStream stream)
		{
			var fAllowWitness = stream.TransactionOptions.HasFlag(TransactionOptions.Witness) && stream.ProtocolCapabilities.SupportWitness;

			// Detect dynamic federation block serialization using "HF bit",
			// or the signed bit which is invalid in Bitcoin
			var isDynamic = false;
			int version = 0;
			stream.ReadWrite(ref version);
			isDynamic = version < 0;
			this.nVersion = ~DYNAFED_HF_MASK & version;

			if (isDynamic)
			{
				stream.ReadWrite(ref hashPrevBlock);
				stream.ReadWrite(ref hashMerkleRoot);
				stream.ReadWrite(ref nTime);
				stream.ReadWrite(ref _nHeight);
				stream.ReadWrite(ref DynaFedParams);
				// We do not serialize witness for hashes, or weight calculation
				if (stream.Type != SerializationType.Hash && fAllowWitness)
				{
					SignBlockWitness = WitScript.Load(stream);
				}
			}
			else
			{

				stream.ReadWrite(ref hashPrevBlock);
				stream.ReadWrite(ref hashMerkleRoot);
				stream.ReadWrite(ref nTime);
				if (ElementsParams<TNetwork>.BlockHeightInHeader)
				{
					stream.ReadWrite(ref _nHeight);
				}

				if (ElementsParams<TNetwork>.SignedBlocks)
				{
					stream.ReadWrite(ref _Proof);
				}
				else
				{
					stream.ReadWrite(ref nBits);
					stream.ReadWrite(ref nNonce);
				}
			}
		}
		// ELEMENTS: we give explicit serialization methods so that we can
		//  mask in the dynafed bit and to selectively embed the blocktime
		public override void ReadWrite(BitcoinStream stream)
		{
			if (stream.Serializing)
			{
				Serialize(stream);
			}
			else
			{
				UnSerialize(stream);
			}
		}
		public override bool IsNull =>ElementsParams<TNetwork>.SignedBlocks ? Proof.IsNull && DynaFedParams.IsNull : base.IsNull;

		protected internal override void SetNull()
		{
			base.SetNull();
			this.nVersion = 0;
			_nHeight = 0;
			_Proof = new BlockProof();
			DynaFedParams = new DynaFedParams();
		}

		uint _nHeight;
		public int Height
		{
			get
			{
				return checked((int)_nHeight);
			}
			set
			{
				_nHeight = checked((uint)value);
			}
		}

		BlockProof _Proof;
		public BlockProof Proof
		{
			get
			{
				return _Proof;
			}
			set
			{
				_Proof = value;
			}
		}

		public DynaFedParams DynaFedParams = new DynaFedParams();

		public WitScript SignBlockWitness = WitScript.Empty;
	}

	public class BlockProof : IBitcoinSerializable
	{
		public BlockProof()
		{
			Challenge = Script.Empty;
			Solution = Script.Empty;
		}

		Script _Challenge;
		public Script Challenge
		{
			get
			{
				return _Challenge;
			}
			set
			{
				_Challenge = value;
			}
		}


		Script _Solution;
		public Script Solution
		{
			get
			{
				return _Solution;
			}
			set
			{
				_Solution = value;
			}
		}

		public bool IsNull => Challenge == Script.Empty;

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _Challenge);
			if (stream.Type != SerializationType.Hash)
				stream.ReadWrite(ref _Solution);
		}
	}

#pragma warning restore CS0618 // Type or member is obsolete
}
