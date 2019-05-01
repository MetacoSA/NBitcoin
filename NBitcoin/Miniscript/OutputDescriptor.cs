using System;
using NBitcoin.Miniscript.Parser;

namespace NBitcoin.Miniscript
{
	public enum OutputDescriptorType
	{
		Bare,
		Pkh,
		Wpkh,
		P2ShWpkh,
		P2Sh,
		Wsh,
		P2ShWsh
	}

	public class OutputDescriptor : IDestination, IEquatable<OutputDescriptor>
	{
		public Miniscript InnerScript { get; }

		public Script ScriptPubKey {
			get {
					switch (Type)
						{
							case (OutputDescriptorType.Pkh):
								return PubKey.Hash.ScriptPubKey;
							case (OutputDescriptorType.Wpkh):
								return PubKey.WitHash.ScriptPubKey;
							case (OutputDescriptorType.P2ShWpkh):
								return PubKey.WitHash.ScriptPubKey.Hash.ScriptPubKey;
							case (OutputDescriptorType.P2Sh):
								return InnerScript.Script.Hash.ScriptPubKey;
							case (OutputDescriptorType.Wsh):
								return InnerScript.Script.WitHash.ScriptPubKey;
							case (OutputDescriptorType.P2ShWsh):
								return InnerScript.Script.WitHash.ScriptPubKey.Hash.ScriptPubKey;
							case (OutputDescriptorType.Bare):
								return InnerScript.Script;

				}
				return null;
			}
			}

		public OutputDescriptorType Type { get; }

		public PubKey PubKey { get; }

		public OutputDescriptor(PubKey pk, OutputDescriptorType type)
		{
			PubKey = pk;
			Type = type;
		}
		public OutputDescriptor(Miniscript inner, OutputDescriptorType type)
		{
			InnerScript = inner;
			Type = type;
		}

		public override string ToString()
		{
			switch (this.Type)
			{
				case OutputDescriptorType.Bare:
					return InnerScript.ToString();
				case OutputDescriptorType.Pkh:
					return $"pkh({PubKey})";
				case OutputDescriptorType.Wpkh:
					return $"wpkh({PubKey})";
				case OutputDescriptorType.P2ShWpkh:
					return $"sh(wpkh({PubKey}))";
				case OutputDescriptorType.P2Sh:
					return $"sh({InnerScript})";
				case OutputDescriptorType.Wsh:
					return $"wsh({InnerScript})";
				case OutputDescriptorType.P2ShWsh:
					return $"sh(wsh({InnerScript}))";
			}
			throw new Exception("unreachable");
		}
		public static OutputDescriptor Parse(string desc)
			=> OutputDescriptorParser.ParseDescriptor(desc);

		public static bool TryParse(string desc, out OutputDescriptor result)
		{
			result = null;
			var res = OutputDescriptorParser.POutputDescriptor.TryParse(desc);
			if (!res.IsSuccess)
				return false;
			result = res.Value;
			return true;
		}

		public sealed override bool Equals(object obj)
		{
			OutputDescriptor other = obj as OutputDescriptor;
			if (other != null)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(OutputDescriptor other)
			=> this.ToString() == other.ToString();

		public override int GetHashCode() => this.ToString().GetHashCode();
	}
}