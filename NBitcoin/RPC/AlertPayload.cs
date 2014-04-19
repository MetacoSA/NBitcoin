using NBitcoin.Crypto;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.RPC
{
	[Payload("alert")]
	public class AlertPayload : IBitcoinSerializable
	{
		/// <summary>
		/// Used for knowing if an alert is valid in past of future
		/// </summary>
		public DateTimeOffset? Now
		{
			get;
			set;
		}

		VarString payload;
		VarString signature;

		int version;
		long relayUntil;
		long expiration;
		public DateTimeOffset Expiration
		{
			get
			{
				return Utils.UnixTimeToDateTime((uint)expiration);
			}
			set
			{
				expiration = Utils.DateTimeToUnixTime(value);
			}
		}

		int id;
		int cancel;
		int[] setCancel = new int[0];
		int minVer;
		int maxVer;
		VarString[] setSubVer = new VarString[0];
		int priority;
		VarString comment;
		VarString statusBar;
		VarString reserved;

		public string[] SetSubVer
		{
			get
			{
				List<string> messages = new List<string>();
				foreach(var v in setSubVer)
				{
					messages.Add(Utils.BytesToString(v.GetString()));
				}
				return messages.ToArray();
			}
			set
			{
				List<VarString> messages = new List<VarString>();
				foreach(var v in value)
				{
					messages.Add(new VarString(Utils.StringToBytes(v)));
				}
				setSubVer = messages.ToArray();
			}
		}

		public string Comment
		{
			get
			{
				return Utils.BytesToString(comment.GetString());
			}
			set
			{
				comment = new VarString(Utils.StringToBytes(value));
			}
		}
		public string StatusBar
		{
			get
			{
				return Utils.BytesToString(statusBar.GetString());
			}
			set
			{
				statusBar = new VarString(Utils.StringToBytes(value));
			}
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			stream.ReadWrite(ref payload);
			if(!stream.Serializing)
			{
				var payloadStream = new BitcoinStream(payload.GetString());
				payloadStream.CopyParameters(stream);

				ReadWritePayloadFields(payloadStream);

			}

			stream.ReadWrite(ref signature);
		}

		private void ReadWritePayloadFields(BitcoinStream payloadStream)
		{
			payloadStream.ReadWrite(ref version);
			payloadStream.ReadWrite(ref relayUntil);
			payloadStream.ReadWrite(ref expiration);
			payloadStream.ReadWrite(ref id);
			payloadStream.ReadWrite(ref cancel);
			payloadStream.ReadWrite(ref setCancel);
			payloadStream.ReadWrite(ref minVer);
			payloadStream.ReadWrite(ref maxVer);
			payloadStream.ReadWrite(ref setSubVer);
			payloadStream.ReadWrite(ref priority);
			payloadStream.ReadWrite(ref comment);
			payloadStream.ReadWrite(ref statusBar);
			payloadStream.ReadWrite(ref reserved);
		}

		private void UpdatePayload(BitcoinStream stream)
		{
			MemoryStream ms = new MemoryStream();
			var seria = new BitcoinStream(ms, true);
			seria.CopyParameters(stream);
			ReadWritePayloadFields(seria);
			payload = new VarString(ms.ToArray());
		}

		#endregion

		public void UpdateSignature(Key key, ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
		{
			UpdatePayload();
			signature = new VarString(key.Sign(Hashes.Hash256(payload.GetString())));
		}

		public void UpdatePayload(ProtocolVersion version = ProtocolVersion.PROTOCOL_VERSION)
		{
			UpdatePayload(new BitcoinStream(new byte[0])
			{
				ProtocolVersion = version
			});
		}

		public bool CheckSignature(Network network)
		{
			return CheckSignature(network.AlertPubKey);
		}
		public bool CheckSignature(PubKey key)
		{
			return key.Verify(Hashes.Hash256(payload.GetString()), signature.GetString());
		}

		public bool IsInEffect
		{
			get
			{
				DateTimeOffset now = Now ?? DateTimeOffset.Now;
				return now < Expiration;
			}
		}

		public bool AppliesTo(int nVersion, string strSubVerIn)
		{
			return IsInEffect
					&& minVer <= nVersion && nVersion <= maxVer
					&& (SetSubVer.Length == 0 || SetSubVer.Contains(strSubVerIn));
		}
	}
}
