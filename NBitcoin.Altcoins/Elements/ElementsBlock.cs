using System;
using System.Collections.Generic;
using System.Text;

namespace NBitcoin.Altcoins.Elements
{
#pragma warning disable CS0618 // Type or member is obsolete
	public class ElementsBlock : Block
	{
		public ElementsBlock(ElementsBlockHeader header, ConsensusFactory consensusFactory) : base(header)
		{
			ElementsConsensusFactory = consensusFactory;
		}
		public ConsensusFactory ElementsConsensusFactory { get; set; }
		public override ConsensusFactory GetConsensusFactory()
		{
			return ElementsConsensusFactory;
		}
	}

	public class ElementsBlockHeader : BlockHeader
	{
		public override void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref nVersion);
			stream.ReadWrite(ref hashPrevBlock);
			stream.ReadWrite(ref hashMerkleRoot);
			stream.ReadWrite(ref nTime);
			stream.ReadWrite(ref _nHeight);
			if (_nHeight > Int32.MaxValue)
				throw new FormatException("Block with height too high");
			stream.ReadWrite(ref _Proof);
		}

		protected internal override void SetNull()
		{
			base.SetNull();
			_nHeight = 0;
			_Proof = new BlockProof();
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
		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _Challenge);
			if (stream.Type != SerializationType.Hash)
				stream.ReadWrite(ref _Solution);
		}
	}

#pragma warning restore CS0618 // Type or member is obsolete
}
