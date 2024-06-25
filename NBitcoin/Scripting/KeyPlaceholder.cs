#if !NO_RECORDS
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NBitcoin.Scripting
{
	public class KeyPlaceholder
	{
		private readonly bool preferShortForm;

		public KeyPlaceholder(int keyIndex, int deposit, int change, bool preferShortForm)
		{
			_identity = new Identity(keyIndex, deposit, change);
			this.preferShortForm = preferShortForm;
		}
		record Identity(int KeyIndex, int Deposit, int Change);
		Identity _identity;
		public int KeyIndex => _identity.KeyIndex;
		public int Deposit => _identity.Deposit;
		public int Change => _identity.Change;

		public static bool TryParse(string str, [MaybeNullWhen(false)] out KeyPlaceholder keyPlaceHolder)
		{
			ArgumentNullException.ThrowIfNull(str);
			keyPlaceHolder = null;
			var match = Regex.Match(str, @"^@(\d+)/(\*\*|<\d+;\d+>)(/\*)?$");
			if (!match.Success)
				return false;
			if (!int.TryParse(match.Groups[1].Value, out var keyIndex))
				return false;

			int deposit = 0;
			int change = 1;
			bool preferShortForm = false;
			if (match.Groups[2].Value == "**")
			{
				preferShortForm = true;
				if (match.Groups[3].Value != "")
					return false;
			}
			else
			{
				if (match.Groups[3].Value != "/*")
					return false;
				var parts = match.Groups[2].Value.Split(';');
				if (parts.Length != 2 ||
					!int.TryParse(parts[0][1..], out deposit) ||
					!int.TryParse(parts[1][..^1], out change))
					return false;
			}
			keyPlaceHolder = new KeyPlaceholder(keyIndex, deposit, change, preferShortForm);
			return true;
		}
		public static KeyPlaceholder Parse(string str)
		{
			if (TryParse(str, out var kp))
				return kp;
			throw new FormatException("Invalid KeyPlaceHolder");
		}

		public override sealed string ToString()
		{
			if (preferShortForm && Deposit == 0 && Change == 1)
				return $"@{KeyIndex}/**";
			else
				return $"@{KeyIndex}/<{Deposit};{Change}>/*";
		}

		public override bool Equals(object? obj) => obj is KeyPlaceholder o && _identity.Equals(o._identity);
		public static bool operator ==(KeyPlaceholder? a, KeyPlaceholder? b) => a is null ? b is null : a.Equals(b);
		public static bool operator !=(KeyPlaceholder? a, KeyPlaceholder? b) => !(a == b);
		public override int GetHashCode() => _identity.GetHashCode();
	}
}
#endif
