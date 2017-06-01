using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	[Payload("alert")]
	public class AlertPayload : Payload, IBitcoinSerializable
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
					messages.Add(Encoders.ASCII.EncodeData(v.GetString()));
				}
				return messages.ToArray();
			}
			set
			{
				List<VarString> messages = new List<VarString>();
				foreach(var v in value)
				{
					messages.Add(new VarString(Encoders.ASCII.DecodeData(v)));
				}
				setSubVer = messages.ToArray();
			}
		}

		public string Comment
		{
			get
			{
				return Encoders.ASCII.EncodeData(comment.GetString());
			}
			set
			{
				comment = new VarString(Encoders.ASCII.DecodeData(value));
			}
		}
		public string StatusBar
		{
			get
			{
				return Encoders.ASCII.EncodeData(statusBar.GetString());
			}
			set
			{
				statusBar = new VarString(Encoders.ASCII.DecodeData(value));
			}
		}

		#region IBitcoinSerializable Members

		public override void ReadWriteCore(BitcoinStream stream)
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

		// FIXME: why do we need version parameter? 
		// it shouldn't be called "version" because the it a field with the same name 
		public void UpdateSignature(Key key)
		{
			if(key == null)
				throw new ArgumentNullException("key");
			UpdatePayload();
			signature = new VarString(key.Sign(Hashes.Hash256(payload.GetString())).ToDER());
		}

		public void UpdatePayload(ProtocolVersion protocolVersion = ProtocolVersion.PROTOCOL_VERSION)
		{
			UpdatePayload(new BitcoinStream(new byte[0])
			{
				ProtocolVersion = protocolVersion
			});
		}

		public bool CheckSignature(Network network)
		{
			if(network == null)
				throw new ArgumentNullException("network");
			return CheckSignature(network.AlertPubKey);
		}

		public bool CheckSignature(PubKey key)
		{
			if(key == null)
				throw new ArgumentNullException("key");
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

		public override string ToString()
		{
			return StatusBar;
		}
	}
}
