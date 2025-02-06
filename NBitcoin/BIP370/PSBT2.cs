using System;
using System.Collections.Generic;
using System.Linq;

namespace NBitcoin.BIP370;

public class PSBT2 : PSBT
{
	internal PSBT2(List<SortedDictionary<byte[], byte[]>> maps, Network network) : base(maps, network)
	{
	}

	protected override void Load(List<SortedDictionary<byte[], byte[]>> maps)
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

		if (globalMap.TryRemove([PSBT2Constants.PSBT_GLOBAL_TX_MODIFIABLE], out var modifiableFlagsBytes))
		{
			byte modifiableFlags = 0;
			new BitcoinStream(modifiableFlagsBytes).ReadWrite(ref modifiableFlags);
			ModifiableFlags = (PSBTModifiable)modifiableFlags;
		}

		if(!globalMap.TryRemove([PSBT2Constants.PSBT_GLOBAL_INPUT_COUNT], out var inputCountBytes))
		{
			throw new FormatException("PSBT v2 must contain PSBT_GLOBAL_INPUT_COUNT");
		}
		if(!globalMap.TryRemove([PSBT2Constants.PSBT_GLOBAL_OUTPUT_COUNT], out var outputCountBytes))
		{
			throw new FormatException("PSBT v2 must contain PSBT_GLOBAL_OUTPUT_COUNT");
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

			var txIn = Network.Consensus.ConsensusFactory.CreateTxIn();
			txIn.PrevOut = outpoint;

			if (map.TryRemove([PSBT2Constants.PSBT_IN_SEQUENCE], out var sequenceBytes))
			{
				uint sequence = 0;
				new BitcoinStream(sequenceBytes).ReadWrite(ref sequence);

				txIn.Sequence = sequence;
			}



			var input = new PSBT2Input(map, this, (uint)(mapIndex - 1), txIn);



			Inputs.Add(input);
		}

		for (; mapIndex <= outputCount+ inputCount; mapIndex++)
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
			Outputs.Add(new PSBTOutput(map, this, (uint)outputIndex, txOut));
		}

		base.Load(maps);
	}

	internal override Transaction tx
	{
		get
		{
			var tx = Network.CreateTransaction();
			tx.Version = TransactionVersion;

			foreach (var input in Inputs.OrderBy(input => input.Index))
			{
				tx.Inputs.Add(input.TxIn);
			}

			foreach (var output in Outputs.OrderBy(output => output.Index))
			{
				tx.Outputs.Add(output.TxOut);
			}

			tx.LockTime = EffectiveLockTime();
			tx.Version = TransactionVersion;

			return tx;
		}
		set { throw new Exception("PSBT2 does not support setting a global transaction"); }
	}


	public LockTime EffectiveLockTime()
	{
		// Check if any input requires time-based or height-based lock time.
		bool requireTimeBasedLockTime = Inputs.Any(input => input is PSBT2Input psbt2Input && psbt2Input.RequiresTimeBasedLockTime());
		bool requireHeightBasedLockTime =  Inputs.Any(input => input is PSBT2Input psbt2Input && psbt2Input.RequiresHeightBasedLockTime());

		// If both types of lock time are required, return the fallback.
		if (requireTimeBasedLockTime && requireHeightBasedLockTime)
		{
			throw new Exception("Cannot determine lock time due to conflicting constraints on inputs");
		}


		LockTime lockTime;
		if (Inputs.Any(input => input is PSBT2Input psbt2Input && ( psbt2Input.LockTime != LockTime.Zero || psbt2Input.LockTimeHeight != LockTime.Zero)))
		{
			// Determine if all inputs are satisfied with height-based lock time.
			bool allInputsSatisfiedWithHeightBasedLockTime = Inputs.All(input => input is not PSBT2Input psbt2Input || psbt2Input.IsSatisfiedWithHeightBasedLockTime());

			// Choose the maximum value of the chosen type of lock time.
			if (allInputsSatisfiedWithHeightBasedLockTime)
			{
				// Use height-based lock time.
				uint height = Inputs.Max(input => input is PSBT2Input psbt2Input ? psbt2Input.LockTimeHeight.Value : 0);
				lockTime = new LockTime(height);
			}
			else
			{
				// Use time-based lock time.
				uint time = Inputs.Max(input => input is PSBT2Input psbt2Input ? psbt2Input.LockTime.Value : 0);
				lockTime = new LockTime(time);
			}
		}
		else
		{
			// Use fallback lock time if no input has a required lock time.
			lockTime = FallbackLockTime;
		}

		return lockTime;
	}



	protected override void LoadInputsOutputs(List<SortedDictionary<byte[], byte[]>> maps)
	{
		//we ovveride this method to avoid loading inputs and outputs here as we load them beforehand.
	}

	public uint TransactionVersion { get; set; }

	LockTime FallbackLockTime { get; set; } = LockTime.Zero;

	//An 8 bit unsigned integer as a bitfield for various transaction modification flags. Bit 0 is the Inputs Modifiable Flag, set to 1 to indicate whether inputs can be added or removed. Bit 1 is the Outputs Modifiable Flag, set to 1 to indicate whether outputs can be added or removed. Bit 2 is the Has SIGHASH_SINGLE flag, set to 1 to indicate whether the transaction has a SIGHASH_SINGLE signature who's input and output pairing must be preserved. Bit 2 essentially indicates that the Constructor must iterate the inputs to determine whether and how to add or remove an input.
	PSBTModifiable ModifiableFlags { get; set; }


	protected override void SerializeGlobals(BitcoinStream stream)
	{
		var psbtGlobalVersion = new byte[] { PSBTConstants.PSBT_GLOBAL_VERSION };
		stream.ReadWriteAsVarString(ref psbtGlobalVersion);

		var version = PSBT2Constants.PSBT2Version;
		stream.ReadWriteAsVarInt(ref version);


		stream.ReadWriteAsVarInt(ref DefaultKeyLen);
		stream.ReadWrite(PSBT2Constants.PSBT_GLOBAL_TX_VERSION);
		stream.ReadWriteAsVarInt(ref DefaultKeyLen);
		stream.ReadWrite(TransactionVersion);


		if (FallbackLockTime != null)
		{
			stream.ReadWriteAsVarInt(ref DefaultKeyLen);
			stream.ReadWrite(PSBT2Constants.PSBT_GLOBAL_FALLBACK_LOCKTIME);
			stream.ReadWrite(FallbackLockTime.Value);
		}

		stream.ReadWriteAsVarInt(ref DefaultKeyLen);
		stream.ReadWrite(PSBT2Constants.PSBT_GLOBAL_INPUT_COUNT);
		stream.ReadWriteAsVarInt(ref DefaultKeyLen);
		stream.ReadWrite((uint)Inputs.Count);

		stream.ReadWriteAsVarInt(ref DefaultKeyLen);
		stream.ReadWrite(PSBT2Constants.PSBT_GLOBAL_OUTPUT_COUNT);
		stream.ReadWriteAsVarInt(ref DefaultKeyLen);
		stream.ReadWrite((uint)Outputs.Count);

		stream.ReadWriteAsVarInt(ref DefaultKeyLen);
		stream.ReadWrite(PSBT2Constants.PSBT_GLOBAL_TX_MODIFIABLE);
		stream.ReadWriteAsVarInt(ref DefaultKeyLen);
		stream.ReadWrite((byte)ModifiableFlags);
	}
}
