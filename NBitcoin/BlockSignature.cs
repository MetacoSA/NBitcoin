using System.Linq;
using NBitcoin.DataEncoders;

namespace NBitcoin
{
	/// <summary>
	/// A representation of a block signature.
	/// </summary>
	public class BlockSignature : IBitcoinSerializable
	{
		protected bool Equals(BlockSignature other)
		{
			return Equals(signature, other.signature);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((BlockSignature) obj);
		}

		public override int GetHashCode()
		{
			return (signature?.GetHashCode() ?? 0);
		}

		public BlockSignature()
		{
			this.signature = new byte[0];
		}

		private byte[] signature;

		public byte[] Signature
		{
			get
			{
				return signature;
			}
			set
			{
				signature = value;
			}
		}

		internal void SetNull()
		{
			signature = new byte[0];
		}

		public bool IsEmpty()
		{
			return !this.signature.Any();
		}

		public static bool operator ==(BlockSignature a, BlockSignature b)
		{
			if (System.Object.ReferenceEquals(a, b))
				return true;

			if (((object)a == null) || ((object)b == null))
				return false;

			return a.signature.SequenceEqual(b.signature);
		}

		public static bool operator !=(BlockSignature a, BlockSignature b)
		{
			return !(a == b);
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWriteAsVarString(ref signature);
		}

		#endregion

		public override string ToString()
		{
			return Encoders.Hex.EncodeData(this.signature);
		}


	}
}
