using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using NBitcoin.Crypto;

namespace NBitcoin.Altcoins
{
    public class ForkIdTransaction : Transaction, IHasForkId
	{
		public ForkIdTransaction(uint forkId, bool supportSegwit, ConsensusFactory consensusFactory)
		{
			_ForkId = forkId;
			_SupportSegwit = supportSegwit;
			_Factory = consensusFactory;
		}

		ConsensusFactory _Factory;
		public override ConsensusFactory GetConsensusFactory()
		{
			return _Factory;
		}

		private readonly bool _SupportSegwit;
		public bool SupportSegwit
		{
			get
			{
				return _SupportSegwit;
			}
		}

		private readonly uint _ForkId;
		public uint ForkId
		{
			get
			{
				return _ForkId;
			}
		}

		public override uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, TxOut spentOutput, HashVersion sigversion, PrecomputedTransactionData precomputedTransactionData)
		{
			uint nForkHashType = (uint)nHashType;
			if(UsesForkId(nHashType))
				nForkHashType |= ForkId << 8;

			if((SupportSegwit && sigversion == HashVersion.WitnessV0) || UsesForkId(nHashType))
			{
				if (spentOutput?.Value == null || spentOutput.Value == TxOut.NullMoney)
					throw new ArgumentException("The output being signed with the amount must be provided", nameof(spentOutput));
				uint256 hashPrevouts = uint256.Zero;
				uint256 hashSequence = uint256.Zero;
				uint256 hashOutputs = uint256.Zero;

				if((nHashType & SigHash.AnyoneCanPay) == 0)
				{
					hashPrevouts = precomputedTransactionData == null ?
								   GetHashPrevouts() : precomputedTransactionData.HashPrevouts;
				}

				if((nHashType & SigHash.AnyoneCanPay) == 0 && ((uint)nHashType & 0x1f) != (uint)SigHash.Single && ((uint)nHashType & 0x1f) != (uint)SigHash.None)
				{
					hashSequence = precomputedTransactionData == null ?
								   GetHashSequence() : precomputedTransactionData.HashSequence;
				}

				if(((uint)nHashType & 0x1f) != (uint)SigHash.Single && ((uint)nHashType & 0x1f) != (uint)SigHash.None)
				{
					hashOutputs = precomputedTransactionData == null ?
									GetHashOutputs() : precomputedTransactionData.HashOutputs;
				}
				else if(((uint)nHashType & 0x1f) == (uint)SigHash.Single && nIn < this.Outputs.Count)
				{
					BitcoinStream ss = CreateHashWriter(sigversion);
					ss.ReadWrite(this.Outputs[nIn]);
					hashOutputs = GetHash(ss);
				}

				BitcoinStream sss = CreateHashWriter(sigversion);
				// Version
				sss.ReadWrite(this.Version);
				// Input prevouts/nSequence (none/all, depending on flags)
				sss.ReadWrite(hashPrevouts);
				sss.ReadWrite(hashSequence);
				// The input being signed (replacing the scriptSig with scriptCode + amount)
				// The prevout may already be contained in hashPrevout, and the nSequence
				// may already be contain in hashSequence.
				sss.ReadWrite(Inputs[nIn].PrevOut);
				sss.ReadWrite(scriptCode);
				sss.ReadWrite(spentOutput.Value.Satoshi);
				sss.ReadWrite(Inputs[nIn].Sequence);
				// Outputs (none/one/all, depending on flags)
				sss.ReadWrite(hashOutputs);
				// Locktime
				sss.ReadWriteStruct(LockTime);
				// Sighash type
				sss.ReadWrite(nForkHashType);

				return GetHash(sss);
			}




			if(nIn >= Inputs.Count)
			{
				return uint256.One;
			}

			var hashType = nHashType & (SigHash)31;

			// Check for invalid use of SIGHASH_SINGLE
			if(hashType == SigHash.Single)
			{
				if(nIn >= Outputs.Count)
				{
					return uint256.One;
				}
			}

			var scriptCopy = scriptCode.Clone();
			scriptCode = scriptCopy.FindAndDelete(OpcodeType.OP_CODESEPARATOR);

			var txCopy = GetConsensusFactory().CreateTransaction();
			txCopy.FromBytes(this.ToBytes());
			//Set all TxIn script to empty string
			foreach(var txin in txCopy.Inputs)
			{
				txin.ScriptSig = new Script();
			}
			//Copy subscript into the txin script you are checking
			txCopy.Inputs[nIn].ScriptSig = scriptCopy;

			if(hashType == SigHash.None)
			{
				//The output of txCopy is set to a vector of zero size.
				txCopy.Outputs.Clear();

				//All other inputs aside from the current input in txCopy have their nSequence index set to zero
				foreach(var input in txCopy.Inputs.Where((x, i) => i != nIn))
					input.Sequence = 0;
			}
			else if(hashType == SigHash.Single)
			{
				//The output of txCopy is resized to the size of the current input index+1.
				txCopy.Outputs.RemoveRange(nIn + 1, txCopy.Outputs.Count - (nIn + 1));
				//All other txCopy outputs aside from the output that is the same as the current input index are set to a blank script and a value of (long) -1.
				for(var i = 0; i < txCopy.Outputs.Count; i++)
				{
					if(i == nIn)
						continue;
					txCopy.Outputs[i] = new TxOut();
				}
				//All other txCopy inputs aside from the current input are set to have an nSequence index of zero.
				foreach(var input in txCopy.Inputs.Where((x, i) => i != nIn))
					input.Sequence = 0;
			}


			if((nHashType & SigHash.AnyoneCanPay) != 0)
			{
				//The txCopy input vector is resized to a length of one.
				var script = txCopy.Inputs[nIn];
				txCopy.Inputs.Clear();
				txCopy.Inputs.Add(script);
				//The subScript (lead in by its length as a var-integer encoded!) is set as the first and only member of this vector.
				txCopy.Inputs[0].ScriptSig = scriptCopy;
			}


			//Serialize TxCopy, append 4 byte hashtypecode
			var stream = CreateHashWriter(sigversion);
			txCopy.ReadWrite(stream);
			stream.ReadWrite(nForkHashType);
			return GetHash(stream);
		}

		private bool UsesForkId(SigHash nHashType)
		{
			return ((uint)nHashType & 0x40u) != 0;
		}

		private static uint256 GetHash(BitcoinStream stream)
		{
			var preimage = ((HashStream)stream.Inner).GetHash();
			stream.Inner.Dispose();
			return preimage;
		}

		internal override uint256 GetHashOutputs()
		{
			uint256 hashOutputs;
			BitcoinStream ss = CreateHashWriter(HashVersion.WitnessV0);
			foreach(var txout in Outputs)
			{
				ss.ReadWrite(txout);
			}
			hashOutputs = GetHash(ss);
			return hashOutputs;
		}

		internal override uint256 GetHashSequence()
		{
			uint256 hashSequence;
			BitcoinStream ss = CreateHashWriter(HashVersion.WitnessV0);
			foreach(var input in Inputs)
			{
				ss.ReadWrite(input.Sequence);
			}
			hashSequence = GetHash(ss);
			return hashSequence;
		}

		internal override uint256 GetHashPrevouts()
		{
			uint256 hashPrevouts;
			BitcoinStream ss = CreateHashWriter(HashVersion.WitnessV0);
			foreach(var input in Inputs)
			{
				ss.ReadWrite(input.PrevOut);
			}
			hashPrevouts = GetHash(ss);
			return hashPrevouts;
		}
	}
}
