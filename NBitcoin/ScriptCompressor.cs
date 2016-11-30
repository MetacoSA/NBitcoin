using System;

namespace NBitcoin
{
	public class ScriptCompressor : IBitcoinSerializable
	{
		// make this static for now (there are only 6 special scripts defined)
		// this can potentially be extended together with a new nVersion for
		// transactions, in which case this value becomes dependent on nVersion
		// and nHeight of the enclosing transaction.
		const uint nSpecialScripts = 6;
		byte[] _Script;
		public byte[] ScriptBytes
		{
			get
			{
				return _Script;
			}
		}
		public ScriptCompressor(Script script)
		{
			_Script = script.ToBytes(true);
		}
		public ScriptCompressor()
		{

		}

		public Script GetScript()
		{
			return new Script(_Script);
		}

		byte[] Compress()
		{
			byte[] result = null;
			var script = Script.FromBytesUnsafe(_Script);
			KeyId keyID = PayToPubkeyHashTemplate.Instance.ExtractScriptPubKeyParameters(script);
			if(keyID != null)
			{
				result = new byte[21];
				result[0] = 0x00;
				Array.Copy(keyID.ToBytes(true), 0, result, 1, 20);
				return result;
			}
			ScriptId scriptID = PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(script);
			if(scriptID != null)
			{
				result = new byte[21];
				result[0] = 0x01;
				Array.Copy(scriptID.ToBytes(true), 0, result, 1, 20);
				return result;
			}
			PubKey pubkey = PayToPubkeyTemplate.Instance.ExtractScriptPubKeyParameters(script, true);
			if(pubkey != null)
			{
				result = new byte[33];
				var pubBytes = pubkey.ToBytes(true);
				Array.Copy(pubBytes, 1, result, 1, 32);
				if(pubBytes[0] == 0x02 || pubBytes[0] == 0x03)
				{
					result[0] = pubBytes[0];
					return result;
				}
				else if(pubBytes[0] == 0x04)
				{
					result[0] = (byte)(0x04 | (pubBytes[64] & 0x01));
					return result;
				}
			}
			return null;
		}

		Script Decompress(uint nSize, byte[] data)
		{
			switch(nSize)
			{
				case 0x00:
					return PayToPubkeyHashTemplate.Instance.GenerateScriptPubKey(new KeyId(data.SafeSubarray(0, 20)));
				case 0x01:
					return PayToScriptHashTemplate.Instance.GenerateScriptPubKey(new ScriptId(data.SafeSubarray(0, 20)));
				case 0x02:
				case 0x03:
					var keyPart = data.SafeSubarray(0, 32);
					var keyBytes = new byte[33];
					keyBytes[0] = (byte)nSize;
					Array.Copy(keyPart, 0, keyBytes, 1, 32);
					return PayToPubkeyTemplate.Instance.GenerateScriptPubKey(keyBytes);
				case 0x04:
				case 0x05:
					byte[] vch = new byte[33];
					vch[0] = (byte)(nSize - 2);
					Array.Copy(data, 0, vch, 1, 32);
					PubKey pubkey = new PubKey(vch, true);
					pubkey = pubkey.Decompress();
					return PayToPubkeyTemplate.Instance.GenerateScriptPubKey(pubkey);
			}
			return null;
		}





		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			if(stream.Serializing)
			{
				var compr = Compress();
				if(compr != null)
				{
					stream.ReadWrite(ref compr);
					return;
				}
				uint nSize = (uint)_Script.Length + nSpecialScripts;
				stream.ReadWriteAsCompactVarInt(ref nSize);
				stream.ReadWrite(ref _Script);
			}
			else
			{
				uint nSize = 0;
				stream.ReadWriteAsCompactVarInt(ref nSize);
				if(nSize < nSpecialScripts)
				{
					byte[] vch = new byte[GetSpecialSize(nSize)];
					stream.ReadWrite(ref vch);
					_Script = Decompress(nSize, vch).ToBytes();
					return;
				}
				nSize -= nSpecialScripts;
				_Script = new byte[nSize];
				stream.ReadWrite(ref _Script);
			}
		}

		private int GetSpecialSize(uint nSize)
		{
			if(nSize == 0 || nSize == 1)
				return 20;
			if(nSize == 2 || nSize == 3 || nSize == 4 || nSize == 5)
				return 32;
			return 0;
		}



		#endregion
	}
}
