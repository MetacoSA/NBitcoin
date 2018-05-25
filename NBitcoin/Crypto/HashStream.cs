using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Crypto
{
	public class HashStream : Stream
	{
		public HashStream()
		{

		}

		public override bool CanRead
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override bool CanSeek
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override bool CanWrite
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override long Length
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override long Position
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			int copied = 0;
			int toCopy = 0;
			while(copied != count)
			{
				toCopy = Math.Min(_Buffer.Length - _Pos, count - copied);
				Buffer.BlockCopy(buffer, offset + copied, _Buffer, _Pos, toCopy);
				copied += (byte)toCopy;
				_Pos += (byte)toCopy;
				ProcessBlockIfNeeded();
			}
		}


		byte[] _Buffer = new byte[32];
		byte _Pos;
		public override void WriteByte(byte value)
		{
			_Buffer[_Pos++] = value;
			ProcessBlockIfNeeded();
		}

		private void ProcessBlockIfNeeded()
		{
			if(_Pos == _Buffer.Length)
				ProcessBlock();
		}

#if(USEBC || WINDOWS_UWP || NETCORE)
		BouncyCastle.Crypto.Digests.Sha256Digest sha = new BouncyCastle.Crypto.Digests.Sha256Digest();
		private void ProcessBlock()
		{
			sha.BlockUpdate(_Buffer, 0, _Pos);
			_Pos = 0;
		}

		public uint256 GetHash()
		{
			ProcessBlock();
			sha.DoFinal(_Buffer, 0);
			_Pos = 32;
			ProcessBlock();
			sha.DoFinal(_Buffer, 0);
			return new uint256(_Buffer);
		}

#else
		System.Security.Cryptography.SHA256Managed sha = new System.Security.Cryptography.SHA256Managed();
		private void ProcessBlock()
		{
			sha.TransformBlock(_Buffer, 0, _Pos, _Buffer, 0);
			_Pos = 0;
		}

		static readonly byte[] Empty = new byte[0];
		public uint256 GetHash()
		{
			ProcessBlock();
			sha.TransformFinalBlock(Empty, 0, 0);
			var hash1 = sha.Hash;
			Buffer.BlockCopy(sha.Hash, 0, _Buffer, 0, 32);
			sha.Initialize();
			sha.TransformFinalBlock(_Buffer, 0, 32);
			var hash2 = sha.Hash;
			return new uint256(hash2);
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
				sha.Dispose();
			base.Dispose(disposing);
		}
#endif
	}
}
