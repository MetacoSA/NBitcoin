#nullable enable
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Map = System.Collections.Generic.SortedDictionary<byte[], byte[]>;
namespace NBitcoin.BIP370;

public class PSBT2 : PSBT
{
	internal PSBT2(Transaction tx, Network network): this(CreateMap(tx, network), network)
	{
	}

	private static Maps CreateMap(Transaction tx, Network network)
	{
		var m = new Maps();
		var global = m.NewMap();
		global.Add([PSBT2Constants.PSBT_GLOBAL_TX_VERSION], tx.Version);
		global.Add([PSBT2Constants.PSBT_GLOBAL_INPUT_COUNT], new VarInt((uint)tx.Inputs.Count));
		global.Add([PSBT2Constants.PSBT_GLOBAL_OUTPUT_COUNT], new VarInt((uint)tx.Outputs.Count));

		foreach (var txin in tx.Inputs)
		{
			PSBT2Input.FillMap(m.NewMap(), txin);
		}
		foreach (var txout in tx.Outputs)
		{
			PSBT2Output.FillMap(m.NewMap(), txout);
		}
		return m;
	}

	internal PSBT2(Maps maps, Network network) : base(maps, network, PSBTVersion.PSBTv2)
	{
		var globalMap = maps[0];
		if (globalMap.ContainsKey([PSBTConstants.PSBT_GLOBAL_UNSIGNED_TX]))
		{
			throw new FormatException("PSBT v2 must not contain PSBT_GLOBAL_UNSIGNED_TX");
		}

		if (globalMap.TryRemove<uint>(PSBT2Constants.PSBT_GLOBAL_FALLBACK_LOCKTIME, out var v))
			FallbackLockTime = new LockTime(v);

		if (globalMap.TryRemove<uint>(PSBT2Constants.PSBT_GLOBAL_TX_VERSION, out var txVersion))
			TransactionVersion = txVersion;
		else
			throw new FormatException("PSBT v2 must contain PSBT_GLOBAL_TX_VERSION");

		if (globalMap.TryRemove<byte>(PSBT2Constants.PSBT_GLOBAL_TX_MODIFIABLE, out var modifiableFlagsByte))
			ModifiableFlags = (PSBTModifiable)modifiableFlagsByte;

		if (!globalMap.TryRemove<VarInt>(PSBT2Constants.PSBT_GLOBAL_INPUT_COUNT, out var inputCount))
			throw new FormatException("PSBT v2 must contain PSBT_GLOBAL_INPUT_COUNT");
		if (!globalMap.TryRemove<VarInt>(PSBT2Constants.PSBT_GLOBAL_OUTPUT_COUNT, out var outputCount))
			throw new FormatException("PSBT v2 must contain PSBT_GLOBAL_OUTPUT_COUNT");

		Unknown = globalMap;

		var mapIndex = 1UL;
		for (; mapIndex <= inputCount.ToLong(); mapIndex++)
		{
			var map = maps[(int)mapIndex];
			var input = new PSBT2Input(map, this, (uint)(mapIndex - 1));
			Inputs.Add(input);
		}

		for (; mapIndex <= (outputCount.ToLong() + inputCount.ToLong()); mapIndex++)
		{
			var outputIndex = mapIndex - inputCount.ToLong();
			var map = maps[(int)mapIndex];
			Outputs.Add(new PSBT2Output(map, this, (uint)outputIndex));
		}
		maps.ThrowIfInvalidKeysLeft();
	}

	internal override Transaction GetGlobalTransaction(bool @unsafe)
	{
		var tx = Network.CreateTransaction();
		tx.Version = TransactionVersion;

		foreach (var input in Inputs.OrderBy(input => input.Index))
		{
			tx.Inputs.Add(((PSBT2Input)input).CreateTxIn());
		}

		foreach (var output in Outputs.OrderBy(output => output.Index))
		{
			tx.Outputs.Add(((PSBT2Output)output).GetTxOut());
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

	public PSBTModifiable? ModifiableFlags { get; set; }

	public PSBT2Input AddInput(OutPoint prevOut)
	{
		if (ModifiableFlags is { } v && !v.HasFlag(PSBTModifiable.InputsModifiable))
			throw new InvalidOperationException("Inputs are not modifiable");
		return new PSBT2Input(prevOut, this, (uint)Inputs.Count);
	}
	public PSBT2Output AddOutput(Money value, Script scriptPubKey)
	{
		if (ModifiableFlags is { } v && !v.HasFlag(PSBTModifiable.OutputsModifiable))
			throw new InvalidOperationException("Inputs are not modifiable");

		var txOut = this.Network.Consensus.ConsensusFactory.CreateTxOut();
		txOut.Value = value;
		txOut.ScriptPubKey = scriptPubKey;
		return new PSBT2Output(value, scriptPubKey, this, (uint)Inputs.Count);
	}
	public override PSBT UpdateFrom(PSBT other)
	{
		if (other is PSBT2 o)
		{
			if (o.FallbackLockTime is { } fallbackLockTime)
				FallbackLockTime = fallbackLockTime;
			if (o.ModifiableFlags is { } modifiableFlags)
				ModifiableFlags = modifiableFlags;
		}
		return base.UpdateFrom(other);
	}

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


	internal override void FillMap(Map map)
	{
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
	}
}
