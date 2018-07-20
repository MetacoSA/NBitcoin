using System;
using System.Linq;
using NBitcoin.Crypto;

namespace NBitcoin.Altcoins
{
  	public class BPrivateTransaction : ForkIdTransaction
  	{
		public BPrivateTransaction(ConsensusFactory consensusFactory) : base(42, false, consensusFactory)
		{
			// BTCP is a fork of Zclassic
			// FORKID_BTCP = 42: https://github.com/BTCPrivate/BitcoinPrivate/blob/4031ff02ec7c56bcafa085b01100cbddfcd33ea3/src/script/interpreter.h#L41
			// No Segwit (For now)
		}

		public override uint256 GetSignatureHash(Script scriptCode, int nIn, SigHash nHashType, Money amount, HashVersion sigversion, PrecomputedTransactionData precomputedTransactionData)
		{
			uint nForkHashType = (uint)nHashType;
			if((nForkHashType & (uint)SigHash.ForkId) != 0)
				nForkHashType |= ForkId << 8;

			/* TODO Segwit compat
			if(false && (SupportSegwit && sigversion == HashVersion.Witness) || (nHashType & SigHash.ForkId) != 0)
			{
				if(amount == null)
					throw new ArgumentException("The amount of the output being signed must be provided", "amount");
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
				sss.ReadWrite(amount.Satoshi);
				sss.ReadWrite((uint)Inputs[nIn].Sequence);
				// Outputs (none/one/all, depending on flags)
				sss.ReadWrite(hashOutputs);
				// Locktime
				sss.ReadWriteStruct(LockTime);
				// Sighash type
				sss.ReadWrite((uint)nForkHashType);

				return GetHash(sss);
			}
			*/

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
			stream.ReadWrite((uint)nForkHashType);
			return GetHash(stream);
		}
  	}
}
