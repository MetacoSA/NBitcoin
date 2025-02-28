#nullable enable
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Map = System.Collections.Generic.SortedDictionary<byte[], byte[]>;
namespace NBitcoin.BIP370;

public class PSBT2 : PSBT
{
	internal PSBT2(Transaction transaction, Network network):base(network, PSBTVersion.PSBTv2)
	{
	}

	internal PSBT2(List<Map> maps, Network network) : base(network, PSBTVersion.PSBTv2)
	{
		var globalMap = maps[0];
		if (globalMap.ContainsKey([PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX]))
		{
			throw new FormatException("PSBT v2 must not contain PSBT_GLOBAL_UNSIGNED_TX");
		}

		if (globalMap.TryRemove([PSBT2Constants.PSBT_GLOBAL_FALLBACK_LOCKTIME], out var lockTimeBytes))
		{
			FallbackLockTime = new LockTime();
			FallbackLockTime.FromBytes(lockTimeBytes);
		}

		if (globalMap.TryRemove([PSBT2Constants.PSBT_GLOBAL_TX_VERSION], out var txVersion))
		{
			uint version = 0;
			new BitcoinStream(txVersion).ReadWrite(ref version);
			TransactionVersion = version;
		}
		else
		{
			throw new FormatException("PSBT v2 must contain PSBT_GLOBAL_TX_VERSION");
		}

		if (globalMap.TryRemove([PSBT2Constants.PSBT_GLOBAL_TX_MODIFIABLE], out var modifiableFlagsBytes))
		{
			byte modifiableFlags = 0;
			new BitcoinStream(modifiableFlagsBytes).ReadWrite(ref modifiableFlags);
			ModifiableFlags = (PSBTModifiable)modifiableFlags;
		}

		if (!globalMap.TryRemove([PSBT2Constants.PSBT_GLOBAL_INPUT_COUNT], out var inputCountBytes))
		{
			throw new FormatException("PSBT v2 must contain PSBT_GLOBAL_INPUT_COUNT");
		}
		if (!globalMap.TryRemove([PSBT2Constants.PSBT_GLOBAL_OUTPUT_COUNT], out var outputCountBytes))
		{
			throw new FormatException("PSBT v2 must contain PSBT_GLOBAL_OUTPUT_COUNT");
		}

		while (globalMap.Pop(out byte[] k, out byte[] v))
		{
			byte[]? xpubBytes = null;
			switch (k[0])
			{
				case PSBTConstants.PSBT_GLOBAL_XPUB:
					xpubBytes ??= Network.GetVersionBytes(Base58Type.EXT_PUBLIC_KEY, false);
					if (xpubBytes is null)
						throw new FormatException("Invalid PSBT. No xpub version bytes");
					var (xpub, rootedKeyPath) = PSBT0.ParseXpub(xpubBytes, k, v);
					GlobalXPubs.Add(xpub.GetWif(Network), rootedKeyPath);
					break;
				default:
					if (!Unknown.TryAdd(k, v))
						throw new FormatException($"Invalid PSBT, duplicate key ({Encoders.Hex.EncodeData(k)}) for unknown value");
					break;
			}
		}

		uint inputCount = 0U;
		uint outputCount = 0U;
		new BitcoinStream(inputCountBytes).ReadWriteAsVarInt(ref inputCount);
		new BitcoinStream(outputCountBytes).ReadWriteAsVarInt(ref outputCount);

		var mapIndex = 1;
		for (; mapIndex <= inputCount; mapIndex++)
		{
			var map = maps[mapIndex];
			if (!map.TryRemove([PSBT2Constants.PSBT_IN_PREVIOUS_TXID], out var txidBytes))
			{
				throw new FormatException("PSBT v2 must contain PSBT_IN_PREVIOUS_TXID");
			}

			if (!map.TryRemove([PSBT2Constants.PSBT_IN_OUTPUT_INDEX], out var indexBytes))
			{
				throw new FormatException("PSBT v2 must contain PSBT_IN_OUTPUT_INDEX");
			}

			var txId = new uint256(txidBytes);
			uint index = 0;
			new BitcoinStream(indexBytes).ReadWrite(ref index);

			var outpoint = new OutPoint(txId, index);

			Sequence? sequence = null;
			if (map.TryRemove([PSBT2Constants.PSBT_IN_SEQUENCE], out var sequenceBytes))
			{
				uint seq = 0;
				new BitcoinStream(sequenceBytes).ReadWrite(ref seq);
				sequence = seq;
			}

			var input = new PSBT2Input(map, this, (uint)(mapIndex - 1), outpoint)
			{
				Sequence = sequence
			};
			Inputs.Add(input);
		}

		for (; mapIndex <= outputCount + inputCount; mapIndex++)
		{
			var outputIndex = mapIndex - inputCount;
			var map = maps[mapIndex];
			if (!map.TryRemove([PSBT2Constants.PSBT_OUT_AMOUNT], out var amountBytes))
			{
				throw new FormatException("PSBT v2 must contain PSBT_OUT_AMOUNT");
			}

			long amt = 0;
			new BitcoinStream(amountBytes).ReadWrite(ref amt);
			if (!map.TryRemove([PSBT2Constants.PSBT_OUT_SCRIPT], out var scriptBytes))
			{
				throw new FormatException("PSBT v2 must contain PSBT_OUT_SCRIPT");
			}

			var amount = new Money(amt);
			var script = new Script(scriptBytes);
			var txOut = Network.Consensus.ConsensusFactory.CreateTxOut();
			txOut.Value = amount;
			txOut.ScriptPubKey = script;
			Outputs.Add(new PSBT2Output(map, this, (uint)outputIndex, txOut));
		}
	}

	protected override PSBTInput CreatePSBTInput(uint index, TxIn txIn)
	{
		return new PSBT2Input(new (), this, index, txIn.PrevOut);
	}

	protected override PSBTOutput CreatePSBTOutput(uint index, TxOut txOut)
	{
		return new PSBT2Output(new(), this, index, txOut);
	}

	public override Transaction GetGlobalTransaction(bool @unsafe)
	{
		var tx = Network.CreateTransaction();
		tx.Version = TransactionVersion;

		foreach (var input in Inputs.OrderBy(input => input.Index))
		{
			tx.Inputs.Add(((PSBT2Input)input).CreateTxIn());
		}

		foreach (var output in Outputs.OrderBy(output => output.Index))
		{
			tx.Outputs.Add(output.TxOut);
		}

		tx.LockTime = EffectiveLockTime();
		tx.Version = TransactionVersion;

		return tx;
	}

	public override PSBT CoinJoin(PSBT other)
	{
		if (other == null)
			throw new ArgumentNullException(nameof(other));
		if (other is not PSBT2)
			throw new ArgumentException("PSBT2 can only coinjoin with PSBT2", nameof(other));
		other.AssertSanity();

		var result = this.Clone();
		for (int i = 0; i < other.Inputs.Count; i++)
		{
			result.Inputs.Add(other.Inputs[i]);
		}
		for (int i = 0; i < other.Outputs.Count; i++)
		{
			result.Outputs.Add(other.Outputs[i]);
		}
		return result;
	}

	public new PSBT2 Clone() => (PSBT2)Clone();


	public LockTime EffectiveLockTime()
	{
		// Check if any input requires time-based or height-based lock time.
		bool requireTimeBasedLockTime = Inputs.Any(input => input is PSBT2Input { UnifiedTimeLock: { IsTimeLock: true } });
		bool requireHeightBasedLockTime = Inputs.Any(input => input is PSBT2Input { LockTime: null, UnifiedTimeLock: { IsHeightLock: true } });

		// If both types of lock time are required, return the fallback.
		if (requireTimeBasedLockTime && requireHeightBasedLockTime)
		{
			throw new InvalidOperationException("Cannot determine lock time due to conflicting constraints on inputs");
		}

		if (!requireHeightBasedLockTime && !requireTimeBasedLockTime)
		{
			requireHeightBasedLockTime = Inputs.Any(output => output is PSBT2Input { LockTimeHeight: not null });
			if (!requireHeightBasedLockTime)
				requireTimeBasedLockTime = Inputs.Any(output => output is PSBT2Input { LockTime: not null });
		}

		LockTime? lockTime;
		if (requireTimeBasedLockTime || requireHeightBasedLockTime)
		{
			// Choose the maximum value of the chosen type of lock time.
			if (requireHeightBasedLockTime)
			{
				// Use height-based lock time.
				int height = Inputs.Max(input => input is PSBT2Input { LockTimeHeight: { } v } ? v : 0);
				lockTime = new LockTime(height);
				if (!lockTime.Value.IsHeightLock)
					throw new InvalidOperationException("This is not a height based lock");
			}
			else // if (requireTimeBasedLockTime)
			{
				// Use time-based lock time.
				uint time = Inputs.Max(input => input is PSBT2Input { LockTime: { } v } ? Utils.DateTimeToUnixTime(v) : 0);
				lockTime = new LockTime(time);
				if (!lockTime.Value.IsTimeLock)
					throw new InvalidOperationException("This is not a time based lock");
			}
		}
		else
		{
			// Use fallback lock time if no input has a required lock time.
			lockTime = FallbackLockTime;
		}

		return lockTime ?? LockTime.Zero;
	}

	public uint TransactionVersion { get; set; }

	public LockTime? FallbackLockTime { get; set; }

	//An 8 bit unsigned integer as a bitfield for various transaction modification flags. Bit 0 is the Inputs Modifiable Flag, set to 1 to indicate whether inputs can be added or removed. Bit 1 is the Outputs Modifiable Flag, set to 1 to indicate whether outputs can be added or removed. Bit 2 is the Has SIGHASH_SINGLE flag, set to 1 to indicate whether the transaction has a SIGHASH_SINGLE signature who's input and output pairing must be preserved. Bit 2 essentially indicates that the Constructor must iterate the inputs to determine whether and how to add or remove an input.
	public PSBTModifiable? ModifiableFlags { get; set; }

	protected override void WriteCore(JsonTextWriter jsonWriter)
	{
		if (FallbackLockTime is { } fallbackLockTime)
		{
			jsonWriter.WritePropertyValue("fallback_locktime", fallbackLockTime.ToString());
		}
		if (ModifiableFlags is { } f)
		{
			jsonWriter.WritePropertyValue("modifiableFlags", f.ToString());
		}
	}


	protected override Map GetGlobalMap()
	{
		var map = new Map(BytesComparer.Instance);
		map.Add([PSBTConstants.PSBT_GLOBAL_VERSION],  PSBT2Constants.PSBT2Version.ToBytes());
		map.Add([PSBT2Constants.PSBT_GLOBAL_TX_VERSION],  TransactionVersion.ToBytes());
		if (FallbackLockTime != null)
		{

			map.Add([PSBT2Constants.PSBT_GLOBAL_FALLBACK_LOCKTIME],  FallbackLockTime.ToBytes());

		}
		map.Add([PSBT2Constants.PSBT_GLOBAL_INPUT_COUNT], new VarInt((ulong)Inputs.Count).ToBytes());
		map.Add([PSBT2Constants.PSBT_GLOBAL_OUTPUT_COUNT], new VarInt((ulong)Outputs.Count).ToBytes());
		if (ModifiableFlags is { } v)
			map.Add([PSBT2Constants.PSBT_GLOBAL_TX_MODIFIABLE], [(byte)v]);

		return map;
	}
}
