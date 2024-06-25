#if !NO_RECORDS
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NBitcoin.Scripting
{
	public enum AddressIntent
	{
		Deposit,
		Change
	}
	public class KeyInformation
	{
		readonly string _Id;
		public RootedKeyPath RootedKeyPath { get; }
		public BitcoinExtPubKey PubKey { get; }

		public KeyInformation(BitcoinExtKey root, KeyPath accountKeyPath)
		{
			RootedKeyPath = new RootedKeyPath(root.GetPublicKey().GetHDFingerPrint(), accountKeyPath);
			PubKey = root.Derive(accountKeyPath).Neuter();
			_Id = $"[{RootedKeyPath}]{PubKey}";
		}
		public KeyInformation(RootedKeyPath rootedKeyPath, BitcoinExtPubKey pubKey)
		{
			ArgumentNullException.ThrowIfNull(rootedKeyPath);
			ArgumentNullException.ThrowIfNull(pubKey);
			RootedKeyPath = rootedKeyPath;
			PubKey = pubKey;
			_Id = $"[{RootedKeyPath}]{PubKey}";
		}
		public static KeyInformation Parse(string str, Network network)
		{
			if (!TryParse(str, network, out var keyInformation))
				throw new FormatException("Invalid KeyInformation");
			return keyInformation;
		}
		public static bool TryParse(string str, Network network, [MaybeNullWhen(false)] out KeyInformation keyInformation)
		{
			ArgumentNullException.ThrowIfNull(str);
			ArgumentNullException.ThrowIfNull(network);
			keyInformation = null;
			Regex regex = new(@"^\[([a-f0-9'h/]+)\]([123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]+)$");
			Match match = regex.Match(str);
			if (!match.Success)
				return false;
			if (!RootedKeyPath.TryParse(match.Groups[1].Value, out RootedKeyPath rootedKeyPath))
				return false;
			BitcoinExtPubKey pubkey;
			try
			{
				pubkey = new BitcoinExtPubKey(match.Groups[2].Value, network);
			}
			catch
			{
				return false;
			}
			keyInformation = new KeyInformation(rootedKeyPath, pubkey);
			return true;
		}

		public override bool Equals(object? obj) => obj is KeyInformation o && _Id.Equals(o._Id);
		public static bool operator ==(KeyInformation? a, KeyInformation? b) => a is null ? b is null : a.Equals(b);
		public static bool operator !=(KeyInformation? a, KeyInformation? b) => !(a == b);
		public override int GetHashCode() => _Id.GetHashCode();
		public override string ToString() => _Id;

		public MultiPathKeyInformation ToMultiPathKeyInformation(KeyPlaceholder keyPlaceholder)
		{
			ArgumentNullException.ThrowIfNull(keyPlaceholder);
			return new(this.RootedKeyPath, this.PubKey, keyPlaceholder.Deposit, keyPlaceholder.Change);
		}
	}
	public class MultiPathKeyInformation
	{
		public MultiPathKeyInformation(RootedKeyPath rootedKeyPath, BitcoinExtPubKey pubKey, int depositIndex, int changeIndex)
		{
			if (depositIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(depositIndex));
			if (changeIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(changeIndex));
			ArgumentNullException.ThrowIfNull(rootedKeyPath);
			ArgumentNullException.ThrowIfNull(pubKey);
			KeyInformation = new KeyInformation(rootedKeyPath, pubKey);

			DepositIndex = depositIndex;
			ChangeIndex = changeIndex;

			_Id = $"[{RootedKeyPath}]{PubKey}/<{DepositIndex};{ChangeIndex}>/*";
		}

		public KeyInformation KeyInformation { get; }

		public static MultiPathKeyInformation Parse(string str, Network network)
		{
			if (!TryParse(str, network, out MultiPathKeyInformation? multiPathKeyInformation))
				throw new FormatException("Invalid MultiPathKeyInformation");
			return multiPathKeyInformation;
		}

		public static bool TryParse(string str, Network network, [MaybeNullWhen(false)] out MultiPathKeyInformation multiPathKeyInformation)
		{
			ArgumentNullException.ThrowIfNull(str);
			ArgumentNullException.ThrowIfNull(network);
			multiPathKeyInformation = null;
			Match match = Regex.Match(str, @"^\[([a-f0-9'h/]+)\]([123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz]+)/<(\d+);(\d+)>/\*$");
			if (!match.Success)
				return false;
			if (!RootedKeyPath.TryParse(match.Groups[1].Value, out RootedKeyPath rootedKeyPath))
				return false;
			BitcoinExtPubKey pubkey;
			try
			{
				pubkey = new BitcoinExtPubKey(match.Groups[2].Value, network);
			}
			catch
			{
				return false;
			}
			if (!int.TryParse(match.Groups[3].Value, out int depositIndex) || depositIndex < 0)
				return false;
			if (!int.TryParse(match.Groups[4].Value, out int changeIndex) || changeIndex < 0)
				return false;
			multiPathKeyInformation = new MultiPathKeyInformation(rootedKeyPath, pubkey, depositIndex, changeIndex);
			return true;
		}

		public BitcoinExtPubKey Derive(AddressIntent intent, int index)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			var intentIdx = intent == AddressIntent.Deposit ? this.DepositIndex : this.ChangeIndex;
			return PubKey.Derive(new KeyPath(new uint[] { (uint)intentIdx, (uint)index }));
		}

		public RootedKeyPath RootedKeyPath => this.KeyInformation.RootedKeyPath;
		public BitcoinExtPubKey PubKey => this.KeyInformation.PubKey;
		public int DepositIndex { get; }
		public int ChangeIndex { get; }
		public override string ToString() => _Id;

		readonly string _Id;

		public override bool Equals(object? obj) => obj is MultiPathKeyInformation o && _Id.Equals(o._Id);
		public static bool operator ==(MultiPathKeyInformation? a, MultiPathKeyInformation? b) => a is null ? b is null : a.Equals(b);
		public static bool operator !=(MultiPathKeyInformation? a, MultiPathKeyInformation? b) => !(a == b);
		public override int GetHashCode() => _Id.GetHashCode();
	}
}
#endif
