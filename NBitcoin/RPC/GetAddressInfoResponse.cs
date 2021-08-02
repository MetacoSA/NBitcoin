#if !NOJSONNET
using System;
using System.Collections.Generic;
using NBitcoin.Scripting;
using Newtonsoft.Json.Linq;

namespace NBitcoin.RPC
{
	public class GetAddressInfoResponse : GetAddressInfoScriptInfoResponse
	{
		public bool IsMine { get; private set; }
		public bool? Solvable { get; private set; }

# nullable enable
		public OutputDescriptor? Descriptor { get; private set; }
#nullable disable

		// present only in p2sh-nested case
		public GetAddressInfoScriptInfoResponse Embedded { get; private set; }
		public string Label { get; private set; }
		public bool? IsChange { get; private set; }
		public bool IsWatchOnly { get; private set; }
		public DateTimeOffset? Timestamp { get; private set; }
		public KeyPath HDKeyPath { get; private set; }
		public uint160 HDSeedID { get; private set; }
		public uint160 HDMasterKeyID { get; private set; }

		public bool? IsCompressed { get; private set; }

		public static GetAddressInfoResponse FromJsonResponse(JObject raw, Network network)
		{
			return new GetAddressInfoResponse().LoadFromJson(raw, network);
		}
		public virtual GetAddressInfoResponse LoadFromJson(JObject raw, Network network)
		{
			SetSubInfo(this, raw, network);
			IsMine = raw.Property("ismine").Value.Value<bool>();
			Solvable = raw.Property("solvable")?.Value.Value<bool>();
			Descriptor = raw.Property("desc") == null
				? null
				: OutputDescriptor.Parse(raw.Property("desc").Value.Value<string>(), network);
			IsWatchOnly = raw.Property("iswatchonly").Value.Value<bool>();
			IsScript = raw.Property("isscript").Value.Value<bool>();
			IsWitness = raw.Property("iswitness").Value.Value<bool>();
			Script = raw.Property("script")?.Value.Value<string>();
			Hex = raw.Property("hex")?.Value.Value<string>();
			var jEmbedded = raw.Property("embedded");
			if (jEmbedded != null)
			{
				var j = jEmbedded.Value.Value<JObject>();
				var e = new GetAddressInfoScriptInfoResponse();
				SetSubInfo(e, j, network);
				Embedded = e;
			}
			IsCompressed = raw.Property("iscompressed")?.Value.Value<bool>();
			Label = raw.Property("label")?.Value.Value<string>();
			IsChange = raw.Property("ischange")?.Value.Value<bool>();
			Timestamp = raw.Property("timestamp") == null ? (DateTimeOffset?)null : Utils.UnixTimeToDateTime(raw.Property("timestamp").Value.Value<ulong>());
			HDKeyPath = raw.Property("hdkeypath") == null ? null : KeyPath.Parse(raw.Property("hdkeypath").Value.Value<string>());
			HDSeedID = raw.Property("hdseedid") == null ? null : uint160.Parse(raw.Property("hdseedid").Value.Value<string>());
			HDMasterKeyID = raw.Property("hdmasterkeyid") == null ? null : uint160.Parse(raw.Property("hdmasterkeyid").Value.Value<string>());
			return this;
		}

		private static void SetSubInfo(GetAddressInfoScriptInfoResponse target, JObject raw, Network network)
		{
			target.IsWitness = raw.Property("iswitness").Value.Value<bool>();
			target.IsScript = raw.Property("isscript").Value.Value<bool>();
			target.Address = BitcoinAddress.Create(raw.Property("address").Value.Value<string>(), network);
			target.ScriptPubKey = new Script(raw.Property("scriptPubKey").Value.Value<string>());
			target.PubKey = raw.Property("pubkey") == null ? null : new PubKey(raw.Property("pubkey").Value.Value<string>());
			var pubkeys = raw.Property("pubkeys");
			if (pubkeys != null)
			{
				foreach (var pk in pubkeys.Value.Values<string>())
					target.PubKeys.Add(new PubKey(pk));
			}
			target.SigsRequired = raw.Property("sigsrequired")?.Value.Value<uint>();
			target.WitnessVersion = raw.Property("witness_version")?.Value.Value<int>();
			target.WitnessProgram = raw.Property("witness_program")?.Value.Value<string>();
		}
	}

	public class GetAddressInfoScriptInfoResponse
	{
		public BitcoinAddress Address { get; internal set; }
		public Script ScriptPubKey { get; internal set; }
		public bool IsScript { get; internal set; }
		public bool IsWitness { get; internal set; }

		// present only in a witness address.
		public int? WitnessVersion { get; internal set; }
		// present only in a witness address.
		public string WitnessProgram { get; internal set; }
		public string Script { get; internal set; }
		public string Hex { get; internal set; }
		public PubKey PubKey { get; internal set; }
		public List<PubKey> PubKeys { get; internal set; }
		public uint? SigsRequired { get; internal set; }
	}
}
#endif
