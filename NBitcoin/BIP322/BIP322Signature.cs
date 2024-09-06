﻿#if HAS_SPAN
#nullable enable
using NBitcoin.DataEncoders;
using NBitcoin.Crypto;
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
		/// Check if the PSBT is a PSBT created by <see cref="NBitcoin.BitcoinAddress.CreateBIP322PSBT(string, uint, uint, uint, Coin[])"/>
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
		/// Create a BIP322Signature from a signed PSBT initially created by <see cref="CreatePSBT(BitcoinAddress, string, uint, uint, uint, Coin[])"/>
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
			if (!IsValidPSBT(psbt) || txin is null)
				throw new ArgumentException("This PSBT isn't BIP322 compatible", nameof(psbt));
			
			if (signatureType == SignatureType.Legacy)
			{
				throw new ArgumentException("SignatureType.Legacy isn't supported for this operation", nameof(signatureType));
			}

			psbt.Settings.ScriptVerify = BIP322ScriptVerify;
			psbt = psbt.Finalize();

			if (signatureType == SignatureType.Simple)
			{
				var witness = txin.FinalScriptWitness;
				if (witness is null)
					throw new ArgumentException("This PSBT isn't signed with segwith, SignatureType.Simple is not compatible", nameof(signatureType));
				return new BIP322Signature.Simple(witness, psbt.Network);
			}
			else //if (signatureType == SignatureType.Full)
				return new BIP322Signature.Full(psbt.ExtractTransaction(), psbt.Network);
		}

		private static string TAG = "BIP0322-signed-message";

		private static byte[] BITCOIN_SIGNED_MESSAGE_HEADER_BYTES =>
			Encoding.UTF8.GetBytes("Bitcoin Signed Message:\n");
		public static uint256 CreateMessageHash(string message, bool legacy = false)
		{
			var bytes = Encoding.UTF8.GetBytes(message);
			if (legacy)
			{
				var ms = new MemoryStream();
				ms.WriteByte((byte)BITCOIN_SIGNED_MESSAGE_HEADER_BYTES.Length);
				ms.Write(BITCOIN_SIGNED_MESSAGE_HEADER_BYTES, 0, BITCOIN_SIGNED_MESSAGE_HEADER_BYTES.Length);

				var size = new VarInt((ulong)message.Length).ToBytes();
				ms.Write(size, 0, size.Length);
				ms.Write(bytes, 0, bytes.Length);
				return Hashes.DoubleSHA256(ms.ToArray());
			}
			else
			{
				using Secp256k1.SHA256 sha = new Secp256k1.SHA256();
				sha.InitializeTagged(TAG);
				sha.Write(bytes);
				return new uint256(sha.GetHash(), false);
			}
		}

		/// <summary>
		/// This PSBT represent the to_sign transaction along with the to_spend one as the non_witness_utxo of the first input.
		/// Users can take this PSBT, sign it, then call <see cref="FromPSBT(PSBT, SignatureType)"/> to create the signature.
		/// </summary>
		/// <param name="bitcoinAddress"></param>
		/// <param name="message"></param>
		/// <param name="version"></param>
		/// <param name="lockTime"></param>
		/// <param name="sequence"></param>
		/// <param name="fundProofOutputs"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static PSBT CreatePSBT(
			BitcoinAddress bitcoinAddress,
			string message,
			uint version = 0, uint lockTime = 0, uint sequence = 0, Coin[]? fundProofOutputs = null)
		{
			var messageHash = CreateMessageHash(message);

			var toSpend = bitcoinAddress.Network.CreateTransaction();
			toSpend.Version = 0;
			toSpend.LockTime = 0;
			toSpend.Inputs.Add(new TxIn(new OutPoint(uint256.Zero, 0xFFFFFFFF), new Script(OpcodeType.OP_0, Op.GetPushOp(messageHash.ToBytes(false))))
			{
				Sequence = 0,
				WitScript = WitScript.Empty,
			});
			toSpend.Outputs.Add(new TxOut(Money.Zero, bitcoinAddress.ScriptPubKey));
			var toSpendTxId = toSpend.GetHash();
			var toSign = bitcoinAddress.Network.CreateTransaction();
			toSign.Version = version;
			toSign.LockTime = lockTime;
			toSign.Inputs.Add(new TxIn(new OutPoint(toSpendTxId, 0))
			{
				Sequence = sequence
			});
			fundProofOutputs ??= fundProofOutputs ?? Array.Empty<Coin>();

			foreach (var input in fundProofOutputs)
			{
				toSign.Inputs.Add(new TxIn(input.Outpoint, Script.Empty)
				{
					Sequence = sequence,
				});
			}
			toSign.Outputs.Add(new TxOut(Money.Zero, new Script(OpcodeType.OP_RETURN)));
			var psbt = PSBT.FromTransaction(toSign, bitcoinAddress.Network);
			psbt.Settings.AutomaticUTXOTrimming = false;
			psbt.Settings.ScriptVerify = BIP322ScriptVerify;
			psbt.AddTransactions(toSpend);
			psbt.AddCoins(fundProofOutputs);
			return psbt;
		}

		internal static readonly ScriptVerify BIP322ScriptVerify = ScriptVerify.ConstScriptCode
						   | ScriptVerify.LowS
						   | ScriptVerify.StrictEnc
						   | ScriptVerify.NullFail
						   | ScriptVerify.MinimalData
						   | ScriptVerify.CleanStack
						   | ScriptVerify.P2SH
						   | ScriptVerify.Witness
						   | ScriptVerify.Taproot
						   | ScriptVerify.MinimalIf;

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
			else if (TryParseTransaction(bytes, network, out var tx))
			{
				if (tx.Inputs.Count != 1 || tx.Outputs.Count != 1)
					return false;
				if (!Full.IsValid(tx))
					return false;
				result = new Full(tx, network);
			}
			else if (TryParseWitScript(bytes, out var witScript))
			{
				result = new Simple(witScript, network);
			}
			else
			{
				return false;
			}
			return true;
		}

		private static bool TryParseWitScript(byte[] bytes, [MaybeNullWhen(false)] out WitScript witScript)
		{
			try
			{
				witScript = new WitScript(bytes);
				var bytes2 = witScript.ToBytes();
				if (!Utils.ArrayEqual(bytes, bytes2))
				{
					witScript = null;
					return false;
				}
				return true;
			}
			catch
			{
				witScript = null;
				return false;
			}
		}

		private static bool TryParseTransaction(byte[] bytes, Network network, [MaybeNullWhen(false)] out Transaction tx)
		{			
			try
			{
				tx = Transaction.Load(bytes, network);
				var bytes2 = tx.ToBytes();
				if (!Utils.ArrayEqual(bytes, bytes2))
				{
					tx = null;
					return false;
				}
				return true;
			}
			catch
			{
				tx = null;
				return false;
			}
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
