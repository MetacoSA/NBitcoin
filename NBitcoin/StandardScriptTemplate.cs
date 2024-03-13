#nullable enable
using NBitcoin.Crypto;
using NBitcoin.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin
{
	public class TxNullDataTemplate : ScriptTemplate
	{
		public TxNullDataTemplate(int maxScriptSize)
		{
			MaxScriptSizeLimit = maxScriptSize;
		}
		private static readonly TxNullDataTemplate _Instance = new TxNullDataTemplate(MAX_OP_RETURN_RELAY);
		public static TxNullDataTemplate Instance
		{
			get
			{
				return _Instance;
			}
		}
		public int MaxScriptSizeLimit
		{
			get;
			private set;
		}
		protected override bool FastCheckScriptPubKey(Script scriptPubKey, out bool needMoreCheck)
		{
			var bytes = scriptPubKey.ToBytes(true);
			if (bytes.Length == 0 ||
				bytes[0] != (byte)OpcodeType.OP_RETURN ||
				bytes.Length > MaxScriptSizeLimit)
			{
				needMoreCheck = false;
				return false;
			}
			needMoreCheck = true;
			return true;
		}
		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			return scriptPubKeyOps.Skip(1).All(o => o.PushData != null && !o.IsInvalid);
		}
		public byte[][]? ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			bool needMoreCheck;
			if (!FastCheckScriptPubKey(scriptPubKey, out needMoreCheck))
				return null;
			var ops = scriptPubKey.ToOps().ToArray();
			if (!CheckScriptPubKeyCore(scriptPubKey, ops))
				return null;
			return ops.Skip(1).Select(o => o.PushData).ToArray();
		}

		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script? scriptPubKey, Op[]? scriptPubKeyOps)
		{
			return false;
		}

		public const int MAX_OP_RETURN_RELAY = 83; //! bytes (+1 for OP_RETURN, +2 for the pushdata opcodes)
		public Script GenerateScriptPubKey(params byte[][] data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			Op[] ops = new Op[data.Length + 1];
			ops[0] = OpcodeType.OP_RETURN;
			for (int i = 0; i < data.Length; i++)
			{
				ops[1 + i] = Op.GetPushOp(data[i]);
			}
			var script = new Script(ops);
			if (script.ToBytes(true).Length > MaxScriptSizeLimit)
				throw new ArgumentOutOfRangeException("data", "Data in OP_RETURN should have a maximum size of " + MaxScriptSizeLimit + " bytes");
			return script;
		}
	}

	public class PayToMultiSigTemplateParameters
	{
		public PayToMultiSigTemplateParameters(int signatureCount, PubKey[] pubkeys, byte[][] invalidPubkeys)
		{
			if (pubkeys is null)
				throw new ArgumentNullException(nameof(pubkeys));
			if (invalidPubkeys is null)
				throw new ArgumentNullException(nameof(invalidPubkeys));
			SignatureCount = signatureCount;
			PubKeys = pubkeys;
			InvalidPubKeys = invalidPubkeys;
		}
		[Obsolete("Use PayToMultiSigTemplateParameters(int signatureCount, PubKey[] pubkeys, byte[][] invalidPubkeys) instead")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public PayToMultiSigTemplateParameters()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{

		}
		public int SignatureCount
		{
			get;
			set;
		}
		public PubKey[] PubKeys
		{
			get;
			set;
		}

		public byte[][] InvalidPubKeys
		{
			get;
			set;
		}
	}

	public class PayToTaprootTemplate : ScriptTemplate
	{
		private PayToTaprootTemplate()
		{

		}
		static PayToTaprootTemplate? _Instance;
		public static PayToTaprootTemplate Instance => _Instance ??= new PayToTaprootTemplate();

		[Obsolete("Use pubKey.ScriptPubKey instead")]
		public Script GenerateScriptPubKey(TaprootPubKey pubKey)
		{
			return pubKey.ScriptPubKey;
		}

		public TaprootPubKey? ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			var ok = FastCheckScriptPubKey(scriptPubKey, out _);
			if (!ok)
				return null;
			var arr = scriptPubKey.ToBytes(true);
#if !HAS_SPAN
			var pk = new byte[32];
			Array.Copy(arr, 2, pk, 0, 32);
			if (TaprootPubKey.TryCreate(pk, out var r))
				return r;
			return null;
#else
			if (TaprootPubKey.TryCreate(arr.AsSpan().Slice(2), out var r))
				return r;
			return null;
#endif
		}
		protected override bool FastCheckScriptPubKey(Script scriptPubKey, out bool needMoreCheck)
		{
			needMoreCheck = false;
			return scriptPubKey.Length == 34 && scriptPubKey._Script[0] == 0x51 && scriptPubKey._Script[1] == 32;
		}
		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			return true;
		}
		protected override bool FastCheckScriptSig(Script scriptSig, Script? scriptPubKey, out bool needMoreCheck)
		{
			needMoreCheck = false;
			return true;
		}
		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script? scriptPubKey, Op[]? scriptPubKeyOps)
		{
			throw new NotSupportedException();
		}

		[Obsolete("Use GenerateWitScript(TaprootSignature signature) instead")]
		public Script GenerateScriptSig(TaprootSignature signature)
		{
			return GenerateWitScript(signature);
		}

		public PayToTaprootScriptSigParameters? ExtractWitScriptParameters(WitScript witScript)
		{
			if (witScript == null)
				throw new ArgumentNullException(nameof(witScript));

			if (witScript.PushCount != 1 && witScript.PushCount != 2)
				return null;
			var hasAnex = false;
			if (witScript.PushCount == 2)
			{
				if (!CheckAnnex(witScript[1]))
					return null;
				hasAnex = true;
			}
			if (!TaprootSignature.TryParse(witScript[0], out var sig))
				return null;
			return new PayToTaprootScriptSigParameters(sig, hasAnex ? witScript[1] : null)
			{
				TransactionSignature = sig,
				Annex = hasAnex ? witScript[1] : null,
			};
		}

		private bool CheckAnnex(byte[] annex)
		{
			return annex.Length > 0 && annex[0] == 0x50;
		}

		public WitScript GenerateWitScript(TaprootSignature signature)
		{
			if (signature == null)
				throw new ArgumentNullException(nameof(signature));
			return new WitScript(new[] { signature.ToBytes() });
		}

		public WitScript GenerateWitScript(TaprootSignature signature, byte[] annex)
		{
			if (signature == null)
				throw new ArgumentNullException(nameof(signature));
			if (annex == null)
				return GenerateWitScript(signature);
			if (!CheckAnnex(annex))
				throw new ArgumentException("The first byte of annex must be 0x50", "annex");
			return new WitScript(new[] { signature.ToBytes(), annex });
		}
	}
	public class PayToMultiSigTemplate : ScriptTemplate
	{
		private static readonly PayToMultiSigTemplate _Instance = new PayToMultiSigTemplate();
		public static PayToMultiSigTemplate Instance
		{
			get
			{
				return _Instance;
			}
		}

		public Script GenerateScriptPubKey(int sigCount, params PubKey[] keys)
		{
			return GenerateScriptPubKey(sigCount, false, keys);
		}

		public Script GenerateScriptPubKey(int sigCount, bool sort, params PubKey[] keys)
		{
			if (keys == null)
				throw new ArgumentNullException(nameof(keys));
			if (sort)
				Array.Sort(keys);
			List<Op> ops = new List<Op>();
			var push = Op.GetPushOp(sigCount);
			if (!push.IsSmallUInt)
				throw new ArgumentOutOfRangeException("sigCount should be less or equal to 16");
			ops.Add(push);
			var keyCount = Op.GetPushOp(keys.Length);
			if (!keyCount.IsSmallUInt)
				throw new ArgumentOutOfRangeException("key count should be less or equal to 16");

			foreach (var key in keys)
			{
				ops.Add(Op.GetPushOp(key.ToBytes()));
			}
			ops.Add(keyCount);
			ops.Add(OpcodeType.OP_CHECKMULTISIG);
			return new Script(ops);
		}
		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			var ops = scriptPubKeyOps;
			if (ops.Length < 3)
				return false;

			var sigCount = ops[0].GetInt();
			var keyCount = ops[ops.Length - 2].GetInt();

			if (sigCount == null || keyCount == null)
				return false;
			if (keyCount.Value < 0 || keyCount.Value > 20)
				return false;
			if (sigCount.Value < 0 || sigCount.Value > keyCount.Value)
				return false;
			if (1 + keyCount + 1 + 1 != ops.Length)
				return false;
			for (int i = 1; i < keyCount + 1; i++)
			{
				if (ops[i].PushData == null)
					return false;
			}
			return ops[ops.Length - 1].Code == OpcodeType.OP_CHECKMULTISIG;
		}

		public PayToMultiSigTemplateParameters? ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			bool needMoreCheck;
			if (!FastCheckScriptPubKey(scriptPubKey, out needMoreCheck))
				return null;
			var ops = scriptPubKey.ToOps().ToArray();
			if (!CheckScriptPubKeyCore(scriptPubKey, ops))
				return null;

			//already checked in CheckScriptPubKeyCore
			var sigCount = ops[0].GetInt()!.Value;
			var keyCount = ops[ops.Length - 2].GetInt()!.Value;
			List<PubKey> keys = new List<PubKey>();
			List<byte[]> invalidKeys = new List<byte[]>();
			for (int i = 1; i < keyCount + 1; i++)
			{
				if (PubKey.TryCreatePubKey(ops[i].PushData, out var pk))
				{
					keys.Add(pk);
				}
				else
				{
					invalidKeys.Add(ops[i].PushData);
				}
			}

			return new PayToMultiSigTemplateParameters(sigCount, keys.ToArray(), invalidKeys.ToArray());
		}

		protected override bool FastCheckScriptSig(Script scriptSig, Script? scriptPubKey, out bool needMoreCheck)
		{
			var bytes = scriptSig.ToBytes(true);
			if (bytes.Length == 0 ||
				   bytes[0] != (byte)OpcodeType.OP_0)
			{
				needMoreCheck = false;
				return false;
			}
			needMoreCheck = true;
			return true;
		}

		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script? scriptPubKey, Op[]? scriptPubKeyOps)
		{
			if (!scriptSig.IsPushOnly)
				return false;
			if (scriptSigOps[0].Code != OpcodeType.OP_0)
				return false;
			if (scriptSigOps.Length == 1)
				return false;
			if (!scriptSigOps.Skip(1).All(s => TransactionSignature.ValidLength(s.PushData.Length) || s.Code == OpcodeType.OP_0))
				return false;
			if (scriptPubKeyOps is not null && scriptPubKey is not null)
			{
				if (!CheckScriptPubKeyCore(scriptPubKey, scriptPubKeyOps))
					return false;
				var sigCountExpected = scriptPubKeyOps[0].GetInt();
				if (sigCountExpected == null)
					return false;
				return sigCountExpected == scriptSigOps.Length + 1;
			}
			return true;

		}

		public TransactionSignature?[]? ExtractScriptSigParameters(Script scriptSig)
		{
			bool needMoreCheck;
			if (!FastCheckScriptSig(scriptSig, null, out needMoreCheck))
				return null;
			var ops = scriptSig.ToOps().ToArray();
			if (!CheckScriptSigCore(scriptSig, ops, null, null))
				return null;
			try
			{
				return ops.Skip(1).Select(i => i.Code == OpcodeType.OP_0 ? null : new TransactionSignature(i.PushData)).ToArray();
			}
			catch (FormatException)
			{
				return null;
			}
		}

		public Script GenerateScriptSig(TransactionSignature[] signatures)
		{
			return GenerateScriptSig((IEnumerable<TransactionSignature>)signatures);
		}

		public Script GenerateScriptSig(IEnumerable<TransactionSignature> signatures)
		{
			List<Op> ops = new List<Op>();
			ops.Add(OpcodeType.OP_0);
			foreach (var sig in signatures)
			{
				if (sig == null)
					ops.Add(OpcodeType.OP_0);
				else
					ops.Add(Op.GetPushOp(sig.ToBytes()));
			}
			return new Script(ops);
		}
	}

	public class PayToScriptHashSigParameters
	{
		public PayToScriptHashSigParameters(Script redeemScript, byte[][] pushes)
		{
			if (redeemScript is null)
				throw new ArgumentNullException(nameof(redeemScript));
			if (pushes is null)
				throw new ArgumentNullException(nameof(pushes));
			RedeemScript = redeemScript;
			Pushes = pushes;
		}
		[Obsolete("Use PayToScriptHashSigParameters(Script redeemScript, byte[][] pushes) instead")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public PayToScriptHashSigParameters()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{

		}
		public Script RedeemScript
		{
			get;
			set;
		}
		public byte[][] Pushes
		{
			get;
			set;
		}
		public TransactionSignature?[]? GetMultisigSignatures()
		{
			return PayToMultiSigTemplate.Instance.ExtractScriptSigParameters(new Script(Pushes.Select(p => Op.GetPushOp(p)).ToArray()));
		}

		public PubKey[]? GetMultisigPubKeys()
		{
			return PayToMultiSigTemplate.Instance.ExtractScriptPubKeyParameters(RedeemScript)?.PubKeys;
		}
	}
	//https://github.com/bitcoin/bips/blob/master/bip-0016.mediawiki
	public class PayToScriptHashTemplate : ScriptTemplate
	{
		private static readonly PayToScriptHashTemplate _Instance = new PayToScriptHashTemplate();
		public static PayToScriptHashTemplate Instance
		{
			get
			{
				return _Instance;
			}
		}
		public Script GenerateScriptPubKey(ScriptId scriptId)
		{
			return new Script(
				OpcodeType.OP_HASH160,
				Op.GetPushOp(scriptId.ToBytes()),
				OpcodeType.OP_EQUAL);
		}
		public Script GenerateScriptPubKey(Script scriptPubKey)
		{
			return GenerateScriptPubKey(scriptPubKey.Hash);
		}

		protected override bool FastCheckScriptPubKey(Script scriptPubKey, out bool needMoreCheck)
		{
			var bytes = scriptPubKey.ToBytes(true);
			needMoreCheck = false;
			return
				   bytes.Length == 23 &&
				   bytes[0] == (byte)OpcodeType.OP_HASH160 &&
				   bytes[1] == 0x14 &&
				   bytes[22] == (byte)OpcodeType.OP_EQUAL;
		}
		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			return true;
		}

		public Script GenerateScriptSig(Op[] ops, Script redeemScript)
		{
			var pushScript = Op.GetPushOp(redeemScript._Script);
			return new Script(ops.Concat(new[] { pushScript }));
		}
		public PayToScriptHashSigParameters? ExtractScriptSigParameters(Script scriptSig)
		{
			return ExtractScriptSigParameters(scriptSig, null as Script);
		}
		public PayToScriptHashSigParameters? ExtractScriptSigParameters(Script scriptSig, ScriptId expectedScriptId)
		{
			if (expectedScriptId == null)
				return ExtractScriptSigParameters(scriptSig, null as Script);
			return ExtractScriptSigParameters(scriptSig, expectedScriptId.ScriptPubKey);
		}
		public PayToScriptHashSigParameters? ExtractScriptSigParameters(Script scriptSig, Script? scriptPubKey)
		{
			var ops = scriptSig.ToOps().ToArray();
			var ops2 = scriptPubKey == null ? null : scriptPubKey.ToOps().ToArray();
			if (!CheckScriptSigCore(scriptSig, ops, scriptPubKey, ops2))
				return null;
			return new PayToScriptHashSigParameters(Script.FromBytesUnsafe(ops[ops.Length - 1].PushData), ops.Take(ops.Length - 1).Select(o => o.PushData).ToArray());
		}
		public Script GenerateScriptSig(byte[][]? pushes, Script redeemScript)
		{
			List<Op> ops = new List<Op>();
			if (pushes != null)
			{
				foreach (var push in pushes)
					ops.Add(Op.GetPushOp(push));
			}
			ops.Add(Op.GetPushOp(redeemScript.ToBytes(true)));
			return new Script(ops);
		}

		public Script GenerateScriptSig(TransactionSignature[] signatures, Script redeemScript)
		{
			List<Op> ops = new List<Op>();
			PayToMultiSigTemplate multiSigTemplate = new PayToMultiSigTemplate();
			bool multiSig = multiSigTemplate.CheckScriptPubKey(redeemScript);
			if (multiSig)
				ops.Add(OpcodeType.OP_0);
			foreach (var sig in signatures)
			{
				ops.Add(sig == null ? OpcodeType.OP_0 : Op.GetPushOp(sig.ToBytes()));
			}
			return GenerateScriptSig(ops.ToArray(), redeemScript);
		}

		public Script GenerateScriptSig(ECDSASignature[] signatures, Script redeemScript)
		{
			return GenerateScriptSig(signatures.Select(s => new TransactionSignature(s, SigHash.All)).ToArray(), redeemScript);
		}
		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script? scriptPubKey, Op[]? scriptPubKeyOps)
		{
			var ops = scriptSigOps;
			if (ops.Length == 0)
				return false;
			if (!scriptSig.IsPushOnly)
				return false;
			if (scriptPubKey != null)
			{
				var expectedHash = ExtractScriptPubKeyParameters(scriptPubKey);
				if (expectedHash == null)
					return false;
				if (expectedHash != Script.FromBytesUnsafe(ops[ops.Length - 1].PushData).Hash)
					return false;
			}

			var redeemBytes = ops[ops.Length - 1].PushData;
			if (redeemBytes.Length > 520)
				return false;
			return Script.FromBytesUnsafe(ops[ops.Length - 1].PushData).IsValid;
		}

		public ScriptId? ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			bool needMoreCheck;
			if (!FastCheckScriptPubKey(scriptPubKey, out needMoreCheck))
				return null;
			return new ScriptId(scriptPubKey.ToBytes(true).SafeSubarray(2, 20));
		}

		public Script GenerateScriptSig(PayToScriptHashSigParameters parameters)
		{
			return GenerateScriptSig(parameters.Pushes, parameters.RedeemScript);
		}
	}
	public class PayToPubkeyTemplate : ScriptTemplate
	{
		private static readonly PayToPubkeyTemplate _Instance = new PayToPubkeyTemplate();
		public static PayToPubkeyTemplate Instance
		{
			get
			{
				return _Instance;
			}
		}
		public Script GenerateScriptPubKey(PubKey pubkey)
		{
			return GenerateScriptPubKey(pubkey.ToBytes(true));
		}
		public Script GenerateScriptPubKey(byte[] pubkey)
		{
			return new Script(
					Op.GetPushOp(pubkey),
					OpcodeType.OP_CHECKSIG
				);
		}

		protected override bool FastCheckScriptPubKey(Script scriptPubKey, out bool needMoreCheck)
		{
			needMoreCheck = false;
			return
				 scriptPubKey.Length > 3 &&
				 PubKey.SanityCheck(scriptPubKey.ToBytes(true), 1, scriptPubKey.Length - 2) &&
				 scriptPubKey.ToBytes(true)[scriptPubKey.Length - 1] == 0xac;
		}

		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			return true;
		}

		public Script GenerateScriptSig(ECDSASignature signature)
		{
			return GenerateScriptSig(new TransactionSignature(signature, SigHash.All));
		}
		public Script GenerateScriptSig(TransactionSignature signature)
		{
			return new Script(
				Op.GetPushOp(signature.ToBytes())
				);
		}

		public TransactionSignature? ExtractScriptSigParameters(Script scriptSig)
		{
			var ops = scriptSig.ToOps().ToArray();
			if (!CheckScriptSigCore(scriptSig, ops, null, null))
				return null;

			var data = ops[0].PushData;
			if (!TransactionSignature.ValidLength(data.Length))
				return null;
			try
			{
				return new TransactionSignature(data);
			}
			catch (FormatException)
			{
				return null;
			}
		}

		protected override bool FastCheckScriptSig(Script scriptSig, Script? scriptPubKey, out bool needMoreCheck)
		{
			needMoreCheck = true;
			return (67 + 1 <= scriptSig.Length && scriptSig.Length <= 80 + 2) || scriptSig.Length == 9 + 1;
		}

		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script? scriptPubKey, Op[]? scriptPubKeyOps)
		{
			var ops = scriptSigOps;
			if (ops.Length != 1)
				return false;
			return ops[0].PushData != null && TransactionSignature.IsValid(ops[0].PushData);
		}

		/// <summary>
		/// Extract the public key or null from the script, perform quick check on pubkey
		/// </summary>
		/// <param name="scriptPubKey"></param>
		/// <returns>The public key</returns>
		public PubKey? ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			bool needMoreCheck;
			if (!FastCheckScriptPubKey(scriptPubKey, out needMoreCheck))
				return null;

			if (PubKey.TryCreatePubKey(scriptPubKey.ToBytes(true).SafeSubarray(1, scriptPubKey.Length - 2), out var pk))
				return pk;
			return null;
		}

		/// <summary>
		/// Extract the public key or null from the script
		/// </summary>
		/// <param name="scriptPubKey"></param>
		/// <param name="deepCheck">Whether deep checks are done on public key</param>
		/// <returns>The public key</returns>
		public PubKey? ExtractScriptPubKeyParameters(Script scriptPubKey, bool deepCheck)
		{
			var result = ExtractScriptPubKeyParameters(scriptPubKey);
			if (result == null || !deepCheck)
				return result;
			if (PubKey.TryCreatePubKey(result.ToBytes(true), out var pk))
				return pk;
			return null;
		}

	}

	public class PayToTaprootScriptSigParameters
	{
		public PayToTaprootScriptSigParameters(TaprootSignature transactionSignature, byte[]? annex = null)
		{
			if (transactionSignature is null)
				throw new ArgumentNullException(nameof(transactionSignature));
			TransactionSignature = transactionSignature;
			Annex = annex;
		}
		[Obsolete("Use PayToTaprootScriptSigParameters(TaprootSignature transactionSignature, byte[]? annex = null) instead")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public PayToTaprootScriptSigParameters()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{

		}
		public TaprootSignature TransactionSignature
		{
			get;
			set;
		}
		public byte[]? Annex
		{
			get;
			set;
		}
	}
	public class PayToWitPubkeyHashScriptSigParameters : PayToPubkeyHashScriptSigParameters
	{
		public PayToWitPubkeyHashScriptSigParameters(TransactionSignature? transactionSignature, PubKey pubKey) : base(transactionSignature, pubKey)
		{

		}
		[Obsolete("Use PayToWitPubkeyHashScriptSigParameters(TransactionSignature? transactionSignature, PubKey pubKey) : base(transactionSignature, pubKey) instead")]
		public PayToWitPubkeyHashScriptSigParameters() : base()
		{

		}
		public override IAddressableDestination Hash
		{
			get
			{
				return PublicKey.WitHash;
			}
		}
	}
	public class PayToPubkeyHashScriptSigParameters : IDestination
	{
		public PayToPubkeyHashScriptSigParameters(TransactionSignature? transactionSignature, PubKey pubKey)
		{
			if (pubKey is null)
				throw new ArgumentNullException(nameof(pubKey));
			this.TransactionSignature = transactionSignature;
			this.PublicKey = pubKey;
		}
		[Obsolete("Use PayToPubkeyHashScriptSigParameters(TransactionSignature? transactionSignature, PubKey pubKey) instead")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public PayToPubkeyHashScriptSigParameters()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{

		}
		public TransactionSignature? TransactionSignature
		{
			get;
			set;
		}
		public PubKey PublicKey
		{
			get;
			set;
		}

		public virtual IAddressableDestination Hash
		{
			get
			{
				return PublicKey.Hash;
			}
		}
		#region IDestination Members

		public Script ScriptPubKey
		{
			get
			{
				return Hash.ScriptPubKey;
			}
		}

		#endregion
	}
	public class PayToPubkeyHashTemplate : ScriptTemplate
	{
		private static readonly PayToPubkeyHashTemplate _Instance = new PayToPubkeyHashTemplate();
		public static PayToPubkeyHashTemplate Instance
		{
			get
			{
				return _Instance;
			}
		}
		public Script GenerateScriptPubKey(BitcoinPubKeyAddress address)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));
			return GenerateScriptPubKey(address.Hash);
		}
		public Script GenerateScriptPubKey(PubKey pubKey)
		{
			if (pubKey == null)
				throw new ArgumentNullException(nameof(pubKey));
			return GenerateScriptPubKey(pubKey.Hash);
		}
		public Script GenerateScriptPubKey(KeyId pubkeyHash)
		{
			return new Script(
					OpcodeType.OP_DUP,
					OpcodeType.OP_HASH160,
					Op.GetPushOp(pubkeyHash.ToBytes()),
					OpcodeType.OP_EQUALVERIFY,
					OpcodeType.OP_CHECKSIG
				);
		}

		public Script GenerateScriptSig(TransactionSignature? signature, PubKey publicKey)
		{
			if (publicKey == null)
				throw new ArgumentNullException(nameof(publicKey));
			return new Script(
				signature is null ? OpcodeType.OP_0 : Op.GetPushOp(signature.ToBytes()),
				Op.GetPushOp(publicKey.ToBytes())
				);
		}

		protected override bool FastCheckScriptPubKey(Script scriptPubKey, out bool needMoreCheck)
		{
			var bytes = scriptPubKey.ToBytes(true);
			needMoreCheck = false;
			return bytes.Length == 25 &&
				   bytes[0] == (byte)OpcodeType.OP_DUP &&
				   bytes[1] == (byte)OpcodeType.OP_HASH160 &&
				   bytes[2] == 0x14 &&
				   bytes[24] == (byte)OpcodeType.OP_CHECKSIG;
		}

		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			return true;
		}
		public KeyId? ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			bool needMoreCheck;
			if (!FastCheckScriptPubKey(scriptPubKey, out needMoreCheck))
				return null;
			return new KeyId(scriptPubKey.ToBytes(true).SafeSubarray(3, 20));
		}

		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script? scriptPubKey, Op[]? scriptPubKeyOps)
		{
			var ops = scriptSigOps;
			if (ops.Length != 2)
				return false;
			return ops[0].PushData != null &&
				   ((ops[0].Code == OpcodeType.OP_0) || TransactionSignature.IsValid(ops[0].PushData, ScriptVerify.DerSig)) &&
				   ops[1].PushData != null && PubKey.SanityCheck(ops[1].PushData);
		}

		public bool CheckScriptSig(Script scriptSig)
		{
			return CheckScriptSig(scriptSig, null);
		}

		public PayToPubkeyHashScriptSigParameters? ExtractScriptSigParameters(Script scriptSig)
		{
			var ops = scriptSig.ToOps().ToArray();
			if (!CheckScriptSigCore(scriptSig, ops, null, null))
				return null;
			if (PubKey.TryCreatePubKey(ops[1].PushData, out var pk))
			{
				return new PayToPubkeyHashScriptSigParameters(ops[0].Code == OpcodeType.OP_0 ? null : new TransactionSignature(ops[0].PushData), pk);
			}
			return null;
		}


		public Script GenerateScriptSig(PayToPubkeyHashScriptSigParameters parameters)
		{
			return GenerateScriptSig(parameters.TransactionSignature, parameters.PublicKey);
		}
	}
	public abstract class ScriptTemplate
	{
		public virtual bool CheckScriptPubKey(Script scriptPubKey)
		{
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));
			bool needMoreCheck;
			bool result = FastCheckScriptPubKey(scriptPubKey, out needMoreCheck);
			if (needMoreCheck)
			{
				result &= CheckScriptPubKeyCore(scriptPubKey, scriptPubKey.ToOps().ToArray());
			}
			return result;
		}

		protected virtual bool FastCheckScriptPubKey(Script scriptPubKey, out bool needMoreCheck)
		{
			needMoreCheck = true;
			return true;
		}

		protected abstract bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps);
		public virtual bool CheckScriptSig(Script scriptSig, Script? scriptPubKey)
		{
			if (scriptSig == null)
				throw new ArgumentNullException(nameof(scriptSig));
			bool needMoreCheck;
			var result = FastCheckScriptSig(scriptSig, scriptPubKey, out needMoreCheck);
			if (needMoreCheck)
			{
				result &= CheckScriptSigCore(scriptSig, scriptSig.ToOps().ToArray(), scriptPubKey, scriptPubKey == null ? null : scriptPubKey.ToOps().ToArray());
			}
			return result;
		}

		protected virtual bool FastCheckScriptSig(Script scriptSig, Script? scriptPubKey, out bool needMoreCheck)
		{
			needMoreCheck = true;
			return true;
		}

		protected abstract bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script? scriptPubKey, Op[]? scriptPubKeyOps);
	}

	public class PayToWitPubKeyHashTemplate : PayToWitTemplate
	{
		static PayToWitPubKeyHashTemplate? _Instance;
		public new static PayToWitPubKeyHashTemplate Instance => _Instance = _Instance ?? new PayToWitPubKeyHashTemplate();
		public Script GenerateScriptPubKey(PubKey pubKey)
		{
			if (pubKey == null)
				throw new ArgumentNullException(nameof(pubKey));
			return GenerateScriptPubKey(pubKey.WitHash);
		}
		public Script GenerateScriptPubKey(WitKeyId pubkeyHash)
		{
			return pubkeyHash.ScriptPubKey;
		}

		public WitScript GenerateWitScript(TransactionSignature? signature, PubKey publicKey)
		{
			if (publicKey == null)
				throw new ArgumentNullException(nameof(publicKey));
			return new WitScript(
				signature == null ? OpcodeType.OP_0 : Op.GetPushOp(signature.ToBytes()),
				Op.GetPushOp(publicKey.ToBytes())
				);
		}

		public Script GenerateScriptPubKey(BitcoinWitPubKeyAddress address)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));
			return GenerateScriptPubKey(address.Hash);
		}

		public override bool CheckScriptPubKey(Script scriptPubKey)
		{
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));
			var bytes = scriptPubKey.ToBytes(true);
			return bytes.Length == 22 && bytes[0] == 0 && bytes[1] == 20;
		}

		public new WitKeyId? ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			if (!CheckScriptPubKey(scriptPubKey))
				return null;
			byte[] data = new byte[20];
			Array.Copy(scriptPubKey.ToBytes(true), 2, data, 0, 20);
			return new WitKeyId(data);
		}

		public PayToWitPubkeyHashScriptSigParameters? ExtractWitScriptParameters(WitScript witScript)
		{
			if (!CheckWitScriptCore(witScript))
				return null;
			if (PubKey.TryCreatePubKey(witScript[1], out var pk))
				return new PayToWitPubkeyHashScriptSigParameters((witScript[0].Length == 0) ? null : new TransactionSignature(witScript[0]), pk);
			return null;
		}

		private bool CheckWitScriptCore(WitScript witScript)
		{
			return witScript.PushCount == 2 &&
				   ((witScript[0].Length == 0) || (TransactionSignature.IsValid(witScript[0], ScriptVerify.None))) &&
				   PubKey.SanityCheck(witScript[1]);
		}


		public WitScript GenerateWitScript(PayToWitPubkeyHashScriptSigParameters parameters)
		{
			return GenerateWitScript(parameters.TransactionSignature, parameters.PublicKey);
		}

	}

	public class PayToWitScriptHashTemplate : PayToWitTemplate
	{
		static PayToWitScriptHashTemplate? _Instance;
		public new static PayToWitScriptHashTemplate Instance
		{
			get
			{
				return _Instance = _Instance ?? new PayToWitScriptHashTemplate();
			}
		}
		public Script GenerateScriptPubKey(WitScriptId scriptHash)
		{
			return scriptHash.ScriptPubKey;
		}

		public WitScript GenerateWitScript(Script scriptSig, Script redeemScript)
		{
			if (redeemScript == null)
				throw new ArgumentNullException(nameof(redeemScript));
			if (scriptSig == null)
				throw new ArgumentNullException(nameof(scriptSig));
			if (!scriptSig.IsPushOnly)
				throw new ArgumentException("The script sig should be push only", "scriptSig");
			scriptSig = scriptSig + Op.GetPushOp(redeemScript.ToBytes(true));
			return new WitScript(scriptSig);
		}

		public WitScript GenerateWitScript(Op[] scriptSig, Script redeemScript)
		{
			if (redeemScript == null)
				throw new ArgumentNullException(nameof(redeemScript));
			if (scriptSig == null)
				throw new ArgumentNullException(nameof(scriptSig));
			return GenerateWitScript(new Script(scriptSig), redeemScript);
		}

		public Script GenerateScriptPubKey(BitcoinWitScriptAddress address)
		{
			if (address == null)
				throw new ArgumentNullException(nameof(address));
			return GenerateScriptPubKey(address.Hash);
		}

		public override bool CheckScriptPubKey(Script scriptPubKey)
		{
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));
			var bytes = scriptPubKey.ToBytes(true);
			return bytes.Length == 34 && bytes[0] == 0 && bytes[1] == 32;
		}
		public new WitScriptId? ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			if (!CheckScriptPubKey(scriptPubKey))
				return null;
			byte[] data = new byte[32];
			Array.Copy(scriptPubKey.ToBytes(true), 2, data, 0, 32);
			return new WitScriptId(data);
		}

		/// <summary>
		/// Extract witness redeem from WitScript
		/// </summary>
		/// <param name="witScript">Witscript to extract information from</param>
		/// <param name="expectedScriptId">Expected redeem hash</param>
		/// <returns>The witness redeem</returns>
		public Script? ExtractWitScriptParameters(WitScript witScript, WitScriptId? expectedScriptId = null)
		{
			if (witScript.PushCount == 0)
				return null;
			var last = witScript.GetUnsafePush(witScript.PushCount - 1);
			Script redeem = new Script(last);
			if (expectedScriptId != null)
			{
				if (expectedScriptId != redeem.WitHash)
					return null;
			}
			return redeem;
		}
	}

	public class WitProgramParameters
	{
		public WitProgramParameters(OpcodeType version, byte[] program)
		{
			if (program is null)
				throw new ArgumentNullException(nameof(program));
			Version = version;
			Program = program;
		}
		[Obsolete("Use WitProgramParameters(OpcodeType version, byte[] program) instead")]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		public WitProgramParameters()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		{

		}
		public OpcodeType Version
		{
			get;
			set;
		}

		public byte[] Program
		{
			get;
			set;
		}

		/// <summary>
		/// Check if this program represent P2WSH
		/// </summary>
		/// <returns>True if P2WSH</returns>
		public bool NeedWitnessRedeemScript()
		{
			return Version == OpcodeType.OP_0 && Program?.Length is 32;
		}

		public bool VerifyWitnessRedeemScript(Script witnessRedeemScript)
		{
			if (witnessRedeemScript == null)
				throw new ArgumentNullException(nameof(witnessRedeemScript));
			if (Version == OpcodeType.OP_0 && Program?.Length is 32)
			{
				return witnessRedeemScript.WitHash == new WitScriptId(Program);
			}
			return false;
		}
	}

	public class PayToWitTemplate : ScriptTemplate
	{
		static PayToWitTemplate? _Instance;
		public static PayToWitTemplate Instance => _Instance = _Instance ?? new PayToWitTemplate();

		public Script GenerateScriptPubKey(OpcodeType segWitVersion, byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));
			if (!ValidSegwitVersion((byte)segWitVersion))
				throw new ArgumentException("Segwit version must be from OP_0 to OP_16", "segWitVersion");
			return new Script(segWitVersion, Op.GetPushOp(data));
		}

		public override bool CheckScriptSig(Script scriptSig, Script? scriptPubKey)
		{
			if (scriptSig == null)
				throw new ArgumentNullException(nameof(scriptSig));
			return scriptSig.Length == 0;
		}

		public override bool CheckScriptPubKey(Script scriptPubKey)
		{
			if (scriptPubKey == null)
				throw new ArgumentNullException(nameof(scriptPubKey));
			var bytes = scriptPubKey.ToBytes(true);
			if (bytes.Length < 4 || bytes.Length > 42)
			{
				return false;
			}
			if (!ValidSegwitVersion(bytes[0]))
			{
				return false;
			}
			return bytes[1] + 2 == bytes.Length;
		}

		public static bool ValidSegwitVersion(byte version)
		{
			return version == 0 || ((byte)OpcodeType.OP_1 <= version && version <= (byte)OpcodeType.OP_16);
		}

		public IAddressableDestination? ExtractScriptPubKeyParameters(Script scriptPubKey)
		{
			if (!CheckScriptPubKey(scriptPubKey))
				return null;
			if (PayToTaprootTemplate.Instance.ExtractScriptPubKeyParameters(scriptPubKey) is TaprootPubKey tpk)
				return tpk;
			var ops = scriptPubKey.ToOps().ToArray();
			if (ops.Length != 2 || ops[1].PushData == null)
				return null;
			if (ops[0].Code == OpcodeType.OP_0)
			{
				if (ops[1].PushData.Length == 20)
					return new WitKeyId(ops[1].PushData);
				if (ops[1].PushData.Length == 32)
					return new WitScriptId(ops[1].PushData);
			}
			return null;
		}
		public WitProgramParameters? ExtractScriptPubKeyParameters2(Script scriptPubKey)
		{
			if (!CheckScriptPubKey(scriptPubKey))
				return null;
			var bytes = scriptPubKey.ToBytes(true);
			var program = new byte[bytes.Length - 2];
			Array.Copy(bytes, 2, program, 0, program.Length);
			return new WitProgramParameters((OpcodeType)bytes[0], program); ;
		}
		protected override bool CheckScriptPubKeyCore(Script scriptPubKey, Op[] scriptPubKeyOps)
		{
			throw new NotImplementedException();
		}

		protected override bool CheckScriptSigCore(Script scriptSig, Op[] scriptSigOps, Script? scriptPubKey, Op[]? scriptPubKeyOps)
		{
			throw new NotImplementedException();
		}
	}
}
