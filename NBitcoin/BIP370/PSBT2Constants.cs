using System;
using System.Collections.Generic;
using System.Linq;
using Map = System.Collections.Generic.SortedDictionary<byte[], byte[]>;

namespace NBitcoin.BIP370;

static  class PSBT2Constants
{
	// Global types

 public const byte PSBT_GLOBAL_TX_VERSION = 0x02;
 public const byte PSBT_GLOBAL_FALLBACK_LOCKTIME = 0x03;
 public const byte PSBT_GLOBAL_INPUT_COUNT = 0x04;
 public const byte PSBT_GLOBAL_OUTPUT_COUNT = 0x05;
 public const byte PSBT_GLOBAL_TX_MODIFIABLE = 0x06;


 public const byte PSBT_IN_PREVIOUS_TXID = 0x0e;
 public const byte PSBT_IN_OUTPUT_INDEX = 0x0f;
 public const byte PSBT_IN_SEQUENCE = 0x10;
 public const byte PSBT_IN_REQUIRED_TIME_LOCKTIME = 0x11;
 public const byte PSBT_IN_REQUIRED_HEIGHT_LOCKTIME = 0x12;

 // Output types
 public const byte PSBT_OUT_AMOUNT = 0x03;
 public const byte PSBT_OUT_SCRIPT = 0x04;

 public const uint PSBT2Version = 2;

}

public enum PSBTModifiable:byte
{
	InputsModifiable = 0,
	OutputsModifiable = 1,
	HasSigHashSingle = 2,

}


public static class SortedDictionaryExtensions
{
	public static bool TryRemove<TKey,TValue>(this SortedDictionary<TKey,TValue> map, TKey key, out TValue value)
	{
		if (!map.TryGetValue(key, out value)) return false;
		map.Remove(key);
		return true;
	}
	public static bool Pop<TKey,TValue>(this SortedDictionary<TKey,TValue> map, out TKey key, out TValue value)
	{
		if (map.Count == 0)
		{
			key = default;
			value = default;
			return false;
		}
		key = map.Keys.First();
		value = map[key];
		map.Remove(key);
		return true;
	}

}

public class PSBT2 : PSBT
{
	internal PSBT2(List<SortedDictionary<byte[], byte[]>> maps, Network network) : base(maps, network)
	{


	}

	protected override void Load(List<Map> maps)
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
		var inputCountBytes = globalMap[[PSBT2Constants.PSBT_GLOBAL_INPUT_COUNT]];
		var outputCountBytes = globalMap[[PSBT2Constants.PSBT_GLOBAL_OUTPUT_COUNT]];
		uint inputCount = 0U;
		uint outputCount = 0U;
		new BitcoinStream(inputCountBytes).ReadWrite(ref inputCount);
		new BitcoinStream(outputCountBytes).ReadWrite(ref outputCount);

		var mapIndex = 1;
		for (;mapIndex <= inputCount; mapIndex++)
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

			var input = new PSBTInput(map, this, (uint)(mapIndex - 1), txIn);

			if (map.TryRemove([PSBT2Constants.PSBT_IN_SEQUENCE], out var sequenceBytes))
			{
				uint sequence = 0;
				new BitcoinStream(sequenceBytes).ReadWrite(ref sequence);

				txIn.Sequence = sequence;
			}

			if (map.TryRemove([PSBT2Constants.PSBT_IN_REQUIRED_TIME_LOCKTIME], out var timeLockTimeBytes))
			{

				var locktime = new LockTime();
				locktime.FromBytes(timeLockTimeBytes);
				//TODO: Add to PSBTInput
			}

			if (map.TryRemove([PSBT2Constants.PSBT_IN_REQUIRED_HEIGHT_LOCKTIME], out var locktimeBytes))
			{

				var locktime = new LockTime();
				locktime.FromBytes(locktimeBytes);
				//TODO: Add to PSBTInput
			}

			Inputs.Add(input);

		}
		for(;mapIndex <= outputCount; mapIndex++)
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
		 var tx=	Network.CreateTransaction();
		 			tx.Version = TransactionVersion;


		}
		set
		{
			throw new Exception("PSBT2 does not support setting a global transaction");
		}
	}

	protected override void LoadInputsOutputs(List<Map> maps)
	{
		//we ovveride this method to avoid loading inputs and outputs here as we load them beforehand.
	}

	public uint TransactionVersion { get; set; }
	LockTime? FallbackLockTime { get; set; }
	//An 8 bit unsigned integer as a bitfield for various transaction modification flags. Bit 0 is the Inputs Modifiable Flag, set to 1 to indicate whether inputs can be added or removed. Bit 1 is the Outputs Modifiable Flag, set to 1 to indicate whether outputs can be added or removed. Bit 2 is the Has SIGHASH_SINGLE flag, set to 1 to indicate whether the transaction has a SIGHASH_SINGLE signature who's input and output pairing must be preserved. Bit 2 essentially indicates that the Constructor must iterate the inputs to determine whether and how to add or remove an input.
	PSBTModifiable ModifiableFlags { get; set; }


	protected override void SerializeGlobals(BitcoinStream stream)
	{
		var psbtGlobalVersion = new  byte[]{PSBTConstants.PSBT_GLOBAL_VERSION};
		stream.ReadWriteAsVarString(ref psbtGlobalVersion);
		stream.ReadWrite(;
		stream.ReadWriteAsVarInt(ref DefaultKeyLen);
		stream.ReadWrite(PSBT2Constants.PSBT2Version);


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

