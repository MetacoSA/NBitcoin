using System.Collections;

namespace NBitcoin.Protocol.Payloads
{
	[Payload("utxos")]
	public class UTxOutputPayload : Payload
	{
		private UTxOutputs _uTxOutputs;

		public override void ReadWriteCore(BitcoinStream stream)
		{
			_uTxOutputs = new UTxOutputs();
			stream.ReadWrite(ref _uTxOutputs);
		}
	}

	public class UTxOutputs : IBitcoinSerializable
	{
		private VarString _bitmap;
		private int _chainHeight;
		private uint256 _chainTipHash;
		private UTxOut[] _outputs;

		public int ChainHeight
		{
			get
			{
				return _chainHeight;
			}
			internal set
			{
				_chainHeight = value;
			}
		}

		public uint256 ChainTipHash
		{
			get
			{
				return _chainTipHash;
			}
			internal set
			{
				_chainTipHash = value;
			}
		}

		public BitArray Bitmap
		{
			get
			{
				return new BitArray(_bitmap.ToBytes());
			}
		}

		public UTxOut[] Outputs
		{
			get
			{
				return _outputs;
			}
			internal set
			{
				_outputs = value;
			}
		}

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _chainHeight);
			stream.ReadWrite(ref _chainTipHash);
			stream.ReadWrite(ref _bitmap);
			stream.ReadWrite(ref _outputs);
		}
	}

	public class UTxOut : IBitcoinSerializable
	{
		private uint _version;
		private uint _height;
		private TxOut _txOut;

		public uint Version
		{
			get
			{
				return _version;
			}
			internal set
			{
				_version = value;
			}
		}

		public uint Height
		{
			get
			{
				return _height;
			}
			internal set
			{
				_height = value;
			}
		}

		public TxOut Output
		{
			get
			{
				return _txOut;
			}
			internal set
			{
				_txOut = value;
			}
		}

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref _version);
			stream.ReadWrite(ref _height);
			stream.ReadWrite(ref _txOut);
		}
	}

}
