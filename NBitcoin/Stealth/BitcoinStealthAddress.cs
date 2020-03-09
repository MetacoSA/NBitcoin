using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Stealth
{
	public class BitField
	{
		byte[] _Rawform;
		byte[] _Mask;
		private readonly int _BitCount;
		public int BitCount
		{
			get
			{
				return _BitCount;
			}
		}
		public int ByteCount
		{
			get
			{
				return _Rawform.Length;
			}
		}

		public byte[] Mask
		{
			get
			{
				return _Mask;
			}
		}

		public BitField(byte[] rawform, int bitcount)
		{
			if (rawform == null)
				throw new ArgumentNullException(nameof(rawform));
			_BitCount = bitcount;

			var byteCount = GetPrefixByteLength(bitcount);
			if (rawform.Length == byteCount)
				_Rawform = rawform.ToArray();
			if (rawform.Length < byteCount)
				_Rawform = rawform.Concat(new byte[byteCount - rawform.Length]).ToArray();
			if (rawform.Length > byteCount)
				_Rawform = rawform.SafeSubarray(0, byteCount);

			_Mask = new byte[byteCount];
			int bitleft = bitcount;

			for (int i = 0; i < byteCount; i++)
			{
				var numberBits = Math.Min(8, bitleft);
				_Mask[i] = (byte)((1 << numberBits) - 1);
				bitleft -= numberBits;
				if (bitleft == 0)
					break;
			}
		}
		public BitField(uint encodedForm, int bitcount)
			: this(Utils.ToBytes(encodedForm, true), bitcount)
		{

		}

		public static int GetPrefixByteLength(int bitcount)
		{
			if (bitcount > 32)
				throw new ArgumentException("Bitcount should be less or equal to 32", "bitcount");
			if (bitcount == 0)
				return 0;
			return Math.Min(4, bitcount / 8 + 1);
		}

		public byte[] GetRawForm()
		{
			return _Rawform.ToArray();
		}

		public uint GetEncodedForm()
		{
			var encoded =
				_Rawform.Length == 4 ? _Rawform : _Rawform.Concat(new byte[4 - _Rawform.Length]).ToArray();

			return Utils.ToUInt32(encoded, true);
		}

		public bool Match(uint value)
		{
			var data = Utils.ToBytes(value, true);
			if (data.Length * 8 < _BitCount)
				return false;

			for (int i = 0; i < _Mask.Length; i++)
			{
				if ((data[i] & _Mask[i]) != (_Rawform[i] & _Mask[i]))
					return false;
			}
			return true;
		}
		public bool Match(StealthMetadata metadata)
		{
			if (metadata == null)
				throw new ArgumentNullException(nameof(metadata));
			return Match(metadata.BitField);
		}

		public StealthPayment[] GetPayments(Transaction transaction)
		{
			return StealthPayment.GetPayments(transaction, null, null).Where(p => this.Match(p.Metadata)).ToArray();
		}
	}
	public class BitcoinStealthAddress : Base58Data
	{

		public BitcoinStealthAddress(string base58, Network expectedNetwork = null)
		{
			Init<BitcoinStealthAddress>(base58, expectedNetwork);
		}
		public BitcoinStealthAddress(byte[] raw, Network network)
			: base(raw, network)
		{
		}


		public BitcoinStealthAddress(PubKey scanKey, PubKey[] pubKeys, int signatureCount, BitField bitfield, Network network)
			: base(GenerateBytes(scanKey, pubKeys, bitfield, signatureCount), network)
		{
		}





		public byte Options
		{
			get;
			private set;
		}

		public byte SignatureCount
		{
			get;
			set;
		}

		public PubKey ScanPubKey
		{
			get;
			private set;
		}

		public PubKey[] SpendPubKeys
		{
			get;
			private set;
		}

		public BitField Prefix
		{
			get;
			private set;
		}

		protected override bool IsValid
		{
			get
			{
				try
				{
					MemoryStream ms = new MemoryStream(vchData);
					this.Options = (byte)ms.ReadByte();
					this.ScanPubKey = new PubKey(ms.ReadBytes(33));
					var pubkeycount = (byte)ms.ReadByte();
					List<PubKey> pubKeys = new List<PubKey>();
					for (int i = 0; i < pubkeycount; i++)
					{
						pubKeys.Add(new PubKey(ms.ReadBytes(33)));
					}
					SpendPubKeys = pubKeys.ToArray();
					SignatureCount = (byte)ms.ReadByte();

					var bitcount = (byte)ms.ReadByte();
					var byteLength = BitField.GetPrefixByteLength(bitcount);

					var prefix = ms.ReadBytes(byteLength);
					Prefix = new BitField(prefix, bitcount);
				}
				catch (Exception)
				{
					return false;
				}
				return true;
			}
		}
		private static byte[] GenerateBytes(PubKey scanKey, PubKey[] pubKeys, BitField bitField, int signatureCount)
		{
			MemoryStream ms = new MemoryStream();
			ms.WriteByte(0); //Options
			ms.Write(scanKey.Compress().ToBytes(), 0, 33);
			ms.WriteByte((byte)pubKeys.Length);
			foreach (var key in pubKeys)
			{
				ms.Write(key.Compress().ToBytes(), 0, 33);
			}
			ms.WriteByte((byte)signatureCount);
			if (bitField == null)
				ms.Write(new byte[] { 0 }, 0, 1);
			else
			{
				ms.WriteByte((byte)bitField.BitCount);
				var raw = bitField.GetRawForm();
				ms.Write(raw, 0, raw.Length);
			}
			return ms.ToArray();
		}

		public override Base58Type Type
		{
			get
			{
				return Base58Type.STEALTH_ADDRESS;
			}
		}


		/// <summary>
		/// Scan the Transaction for StealthCoin given address and scan key
		/// </summary>
		/// <param name="tx">The transaction to scan</param>
		/// <param name="address">The stealth address</param>
		/// <param name="scan">The scan private key</param>
		/// <returns></returns>
		public StealthPayment[] GetPayments(Transaction transaction, Key scanKey)
		{
			return StealthPayment.GetPayments(transaction, this, scanKey);
		}

		/// <summary>
		/// Scan the Transaction for StealthCoin given address and scan key
		/// </summary>
		/// <param name="tx">The transaction to scan</param>
		/// <param name="address">The stealth address</param>
		/// <param name="scan">The scan private key</param>
		/// <returns></returns>
		public StealthPayment[] GetPayments(Transaction transaction, ISecret scanKey)
		{
			if (transaction == null)
				throw new ArgumentNullException(nameof(transaction));
			if (scanKey == null)
				throw new ArgumentNullException(nameof(scanKey));
			return GetPayments(transaction, scanKey.PrivateKey);
		}

		/// <summary>
		/// Prepare a stealth payment 
		/// </summary>
		/// <param name="ephemKey">Ephem Key</param>
		/// <returns>Stealth Payment</returns>
		public StealthPayment CreatePayment(Key ephemKey = null)
		{
			if (ephemKey == null)
				ephemKey = new Key();

			var metadata = StealthMetadata.CreateMetadata(ephemKey, this.Prefix);
			return new StealthPayment(this, ephemKey, metadata);
		}

		/// <summary>
		/// Add a stealth payment to the transaction
		/// </summary>
		/// <param name="transaction">Destination transaction</param>
		/// <param name="value">Money to send</param>
		/// <param name="ephemKey">Ephem Key</param>
		public void SendTo(Transaction transaction, Money value, Key ephemKey = null)
		{
			CreatePayment(ephemKey).AddToTransaction(transaction, value);
		}
	}
}
