#if HAS_SPAN
#nullable enable
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.BIP322
{
	public enum SignatureType
	{
		Legacy,
		Simple,
		Full
	}
	public abstract class BIP322Signature
	{
		public Network Network { get; }

		protected BIP322Signature(Network network)
		{
			if (network is null)
				throw new ArgumentNullException(nameof(network));
			Network = network;
		}
		public static BIP322Signature Parse(string str, Network network)
		{
			if (TryParse(str, network, out var r))
				return r;
			throw new FormatException("Parsing error for expected BIP322 signature");
		}

		/// <summary>
		/// Check if the PSBT is a PSBT created by <see cref="NBitcoin.BitcoinAddress.CreateBIP322PSBT(string, bool, uint, uint, uint, Coin[])"/>
		/// </summary>
		/// <param name="psbt"></param>
		/// <returns></returns>
		public static bool IsValidPSBT(PSBT psbt)
		{
			var txin = psbt.Inputs.FirstOrDefault();
			var toSpend = txin?.NonWitnessUtxo;
			var txout = toSpend?.Outputs.FirstOrDefault();
			if (toSpend is null || txout is null || txin is null ||
				toSpend.Inputs.Count != 1 || !toSpend.IsCoinBase ||
				toSpend.Outputs.Count != 1 || toSpend.Outputs[0].Value != Money.Zero ||
				toSpend.Version != 0 || toSpend.LockTime != LockTime.Zero ||
				!WitScript.IsNullOrEmpty(toSpend.Inputs[0].WitScript) ||
				toSpend.Inputs[0].ScriptSig.Length != 34 ||
				toSpend.Inputs[0].ScriptSig._Script[0] != 0 ||
				toSpend.Inputs[0].ScriptSig._Script[1] != 32)
				return false;
			return true;
		}

		/// <summary>
		/// Create a BIP322Signature from a signed PSBT initially created by <see cref="NBitcoin.BitcoinAddress.CreateBIP322PSBT(string, bool, uint, uint, uint, Coin[])"/>
		/// </summary>
		/// <param name="psbt">The signed PSBT</param>
		/// <param name="signatureType">The type of signature (<see cref="NBitcoin.BIP322.SignatureType.Legacy"/>> isn't supported)</param>
		/// <returns>The Simple of Full signature</returns>
		/// <exception cref="ArgumentNullException"></exception>
		/// <exception cref="ArgumentException"></exception>
		public static BIP322Signature FromPSBT(PSBT psbt, SignatureType signatureType)
		{
			if (psbt is null)
				throw new ArgumentNullException(nameof(psbt));
			psbt = psbt.Clone();
			var txin = psbt.Inputs.FirstOrDefault();
			var toSpend = txin?.NonWitnessUtxo;
			var txout = toSpend?.Outputs.FirstOrDefault();
			if (!IsValidPSBT(psbt))
				throw new ArgumentException("This PSBT isn't BIP322 compatible", nameof(psbt));
			
			if (signatureType == SignatureType.Legacy)
			{
				throw new ArgumentException("SignatureType.Legacy isn't supported for this operation", nameof(signatureType));
			}

			psbt = psbt.Finalize();

			if (signatureType == SignatureType.Simple)
			{
				var witness = txin.FinalScriptWitness!;
				if (witness is null)
					throw new ArgumentException("This PSBT isn't signed with segwith, SignatureType.Simple is not compatible", nameof(signatureType));
				return new BIP322Signature.Simple(witness, psbt.Network);
			}
			else //if (signatureType == SignatureType.Full)
				return new BIP322Signature.Full(psbt.ExtractTransaction(), psbt.Network);
		}
		public static bool TryParse(string str, Network network, [MaybeNullWhen(false)] out BIP322Signature result)
		{
			result = null;
			if (str is null)
				throw new ArgumentNullException(nameof(str));
			byte[] bytes;
			try
			{
				bytes = Encoders.Base64.DecodeData(str);
			}
			catch (FormatException)
			{
				return false;
			}
			return TryCreate(bytes, network, out result);
		}
		public static bool TryCreate(byte[] bytes, Network network, [MaybeNullWhen(false)] out BIP322Signature result)
		{
			result = null;
			if (bytes is null)
				throw new ArgumentNullException(nameof(bytes));
			if (network is null)
				throw new ArgumentNullException(nameof(network));
			if (bytes.Length == 65 && bytes[0] >= 27)
			{
				int recid = (bytes[0] - 27) & 3;
				bool compressed = ((bytes[0] - 27) & 4) != 0;
				result = new Legacy(compressed, new CompactSignature(recid, bytes[1..]), network);
			}
			else if (bytes.Length > 0 && bytes[0] == 0)
			{
				Transaction tx;
				try
				{
					tx = Transaction.Load(bytes, network);
				}
				catch
				{
					return false;
				}
				if (tx.Inputs.Count != 1 || tx.Outputs.Count != 1)
					return false;
				if (!Full.IsValid(tx))
					return false;
				result = new Full(tx, network);
			}
			else
			{
				WitScript witScript;
				try
				{
					witScript = new WitScript(bytes);
				}
				catch
				{
					return false;
				}
				result = new Simple(witScript, network);
			}
			return true;
		}

		public class Legacy : BIP322Signature
		{
			internal Legacy(bool compressed, CompactSignature compactSignature, Network network) : base(network)
			{
				if (compactSignature is null)
					throw new ArgumentNullException(nameof(compactSignature));
				Compressed = compressed;
				CompactSignature = compactSignature;
			}

			public bool Compressed { get; }
			public CompactSignature CompactSignature { get; }

			public override byte[] ToBytes()
			{
				var b = new byte[65];
				b[0] = (byte)(27 + CompactSignature.RecoveryId + (Compressed ? 4 : 0));
				Array.Copy(CompactSignature.Signature, 0, b, 1, 64);
				return b;
			}
		}
		public class Simple : BIP322Signature
		{
			public Simple(WitScript witnessScript, Network network) : base(network)
			{
				if (witnessScript is null)
					throw new ArgumentNullException(nameof(witnessScript));
				this.WitnessScript = witnessScript;
			}
			public WitScript WitnessScript { get; }
			public override byte[] ToBytes()
			{
				return WitnessScript.ToBytes();
			}
		}
		public class Full : BIP322Signature
		{
			public Full(Transaction signedTransaction, Network network) : base(network)
			{
				if (signedTransaction is null)
					throw new ArgumentNullException(nameof(signedTransaction));
				if (!IsValid(signedTransaction))
					throw new ArgumentException("This isn't a valid BIP0322 to_sign transaction", nameof(signedTransaction));
				SignedTransaction = signedTransaction;
				FundProofs = signedTransaction.Inputs.Skip(1).ToArray();
			}

			public TxIn[] FundProofs { get; }

			public Transaction SignedTransaction { get; }
			static Script OpReturn = new Script("OP_RETURN");
			internal static bool IsValid(Transaction tx) =>
				   tx.Outputs.Count == 1 &&
				   tx.Inputs.Count > 0 &&
				   tx.Inputs[0].Sequence == 0 &&
				   tx.Outputs[0].Value == Money.Zero &&
				   tx.Outputs[0].ScriptPubKey == OpReturn;

			public override byte[] ToBytes()
			{
				return SignedTransaction.ToBytes();
			}
		}

		public abstract byte[] ToBytes();
		public string ToBase64() => Encoders.Base64.EncodeData(ToBytes());

		public override bool Equals(object? obj) => obj is BIP322Signature o && ToBase64().Equals(o.ToBase64());
		public static bool operator ==(BIP322Signature? a, BIP322Signature? b) => a is null ? b is null : a.Equals(b);
		public static bool operator !=(BIP322Signature? a, BIP322Signature? b) => !(a == b);
		public override int GetHashCode() => ToBase64().GetHashCode();
	}
}
#endif
