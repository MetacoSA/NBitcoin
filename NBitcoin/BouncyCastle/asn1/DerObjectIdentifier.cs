using System;
using System.IO;
using System.Text;

using NBitcoin.BouncyCastle.Math;
using NBitcoin.BouncyCastle.Utilities;

namespace NBitcoin.BouncyCastle.Asn1
{
	internal class DerObjectIdentifier
		: Asn1Object
	{
		private readonly string identifier;

		private byte[] body = null;

		public DerObjectIdentifier(
			string identifier)
		{
			if(identifier == null)
				throw new ArgumentNullException(nameof(identifier));
			if(!IsValidIdentifier(identifier))
				throw new FormatException("string " + identifier + " not an OID");

			this.identifier = identifier;
		}

		internal DerObjectIdentifier(DerObjectIdentifier oid, string branchID)
		{
			if(!IsValidBranchID(branchID, 0))
				throw new ArgumentException("string " + branchID + " not a valid OID branch", "branchID");

			this.identifier = oid.Id + "." + branchID;
		}

		// TODO Change to ID?
		public string Id
		{
			get
			{
				return identifier;
			}
		}

		public virtual DerObjectIdentifier Branch(string branchID)
		{
			return new DerObjectIdentifier(this, branchID);
		}

		/**
         * Return  true if this oid is an extension of the passed in branch, stem.
         * @param stem the arc or branch that is a possible parent.
         * @return  true if the branch is on the passed in stem, false otherwise.
         */
		public virtual bool On(DerObjectIdentifier stem)
		{
			string id = Id, stemId = stem.Id;
			return id.Length > stemId.Length && id[stemId.Length] == '.' && Platform.StartsWith(id, stemId);
		}

		internal DerObjectIdentifier(byte[] bytes)
		{
			this.identifier = MakeOidStringFromBytes(bytes);
			this.body = Arrays.Clone(bytes);
		}

		private void WriteField(
			Stream outputStream,
			long fieldValue)
		{
			byte[] result = new byte[9];
			int pos = 8;
			result[pos] = (byte)(fieldValue & 0x7f);
			while(fieldValue >= (1L << 7))
			{
				fieldValue >>= 7;
				result[--pos] = (byte)((fieldValue & 0x7f) | 0x80);
			}
			outputStream.Write(result, pos, 9 - pos);
		}

		private void WriteField(
			Stream outputStream,
			BigInteger fieldValue)
		{
			int byteCount = (fieldValue.BitLength + 6) / 7;
			if(byteCount == 0)
			{
				outputStream.WriteByte(0);
			}
			else
			{
				BigInteger tmpValue = fieldValue;
				byte[] tmp = new byte[byteCount];
				for(int i = byteCount - 1; i >= 0; i--)
				{
					tmp[i] = (byte)((tmpValue.IntValue & 0x7f) | 0x80);
					tmpValue = tmpValue.ShiftRight(7);
				}
				tmp[byteCount - 1] &= 0x7f;
				outputStream.Write(tmp, 0, tmp.Length);
			}
		}

		protected override int Asn1GetHashCode()
		{
			return identifier.GetHashCode();
		}

		protected override bool Asn1Equals(
			Asn1Object asn1Object)
		{
			DerObjectIdentifier other = asn1Object as DerObjectIdentifier;

			if(other == null)
				return false;

			return this.identifier.Equals(other.identifier);
		}

		public override string ToString()
		{
			return identifier;
		}

		private static bool IsValidBranchID(
			String branchID, int start)
		{
			bool periodAllowed = false;

			int pos = branchID.Length;
			while(--pos >= start)
			{
				char ch = branchID[pos];

				// TODO Leading zeroes?
				if('0' <= ch && ch <= '9')
				{
					periodAllowed = true;
					continue;
				}

				if(ch == '.')
				{
					if(!periodAllowed)
						return false;

					periodAllowed = false;
					continue;
				}

				return false;
			}

			return periodAllowed;
		}

		private static bool IsValidIdentifier(string identifier)
		{
			if(identifier.Length < 3 || identifier[1] != '.')
				return false;

			char first = identifier[0];
			if(first < '0' || first > '2')
				return false;

			return IsValidBranchID(identifier, 2);
		}

		private const long LONG_LIMIT = (long.MaxValue >> 7) - 0x7f;

		private static string MakeOidStringFromBytes(
			byte[] bytes)
		{
			StringBuilder objId = new StringBuilder();
			long value = 0;
			BigInteger bigValue = null;
			bool first = true;

			for(int i = 0; i != bytes.Length; i++)
			{
				int b = bytes[i];

				if(value <= LONG_LIMIT)
				{
					value += (b & 0x7f);
					if((b & 0x80) == 0)             // end of number reached
					{
						if(first)
						{
							if(value < 40)
							{
								objId.Append('0');
							}
							else if(value < 80)
							{
								objId.Append('1');
								value -= 40;
							}
							else
							{
								objId.Append('2');
								value -= 80;
							}
							first = false;
						}

						objId.Append('.');
						objId.Append(value);
						value = 0;
					}
					else
					{
						value <<= 7;
					}
				}
				else
				{
					if(bigValue == null)
					{
						bigValue = BigInteger.ValueOf(value);
					}
					bigValue = bigValue.Or(BigInteger.ValueOf(b & 0x7f));
					if((b & 0x80) == 0)
					{
						if(first)
						{
							objId.Append('2');
							bigValue = bigValue.Subtract(BigInteger.ValueOf(80));
							first = false;
						}

						objId.Append('.');
						objId.Append(bigValue);
						bigValue = null;
						value = 0;
					}
					else
					{
						bigValue = bigValue.ShiftLeft(7);
					}
				}
			}

			return objId.ToString();
		}

		private static readonly DerObjectIdentifier[] cache = new DerObjectIdentifier[1024];

		internal override void Encode(DerOutputStream derOut)
		{
			throw new NotImplementedException();
		}
	}
}
