using System;
using System.Collections.Generic;
using System.Text;
using UnKnownKVMap = System.Collections.Generic.SortedDictionary<byte[], byte[]>;
using HDKeyPathKVMap = System.Collections.Generic.SortedDictionary<NBitcoin.PubKey, System.Tuple<NBitcoin.HDFingerprint, NBitcoin.KeyPath>>;

namespace NBitcoin
{
	public abstract class PSBTCoin
	{
		protected HDKeyPathKVMap hd_keypaths = new HDKeyPathKVMap(new PubKeyComparer());
		protected UnKnownKVMap unknown = new SortedDictionary<byte[], byte[]>(BytesComparer.Instance);
		protected Script redeem_script;
		protected Script witness_script;
		protected readonly PSBT Parent;
		public PSBTCoin(PSBT parent)
		{
			hd_keypaths = new HDKeyPathKVMap(new PubKeyComparer());
			unknown = new UnKnownKVMap(BytesComparer.Instance);
			Parent = parent;
		}

		public SortedDictionary<byte[], byte[]> Unknown
		{
			get
			{
				return unknown;
			}
		}

		public HDKeyPathKVMap HDKeyPaths
		{
			get
			{
				return hd_keypaths;
			}
		}

		public Script RedeemScript
		{
			get
			{
				return redeem_script;
			}
			set
			{
				redeem_script = value;
			}
		}

		public Script WitnessScript
		{
			get
			{
				return witness_script;
			}
			set
			{
				witness_script = value;
			}
		}

		public void AddKeyPath(ExtPubKey extPubKey, KeyPath path)
		{
			if (extPubKey == null)
				throw new ArgumentNullException(nameof(extPubKey));
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			AddKeyPath(extPubKey.ParentFingerprint, extPubKey.PubKey, path);
		}
		public virtual void AddKeyPath(HDFingerprint fingerprint, PubKey key, KeyPath path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));
			hd_keypaths.AddOrReplace(key, new Tuple<HDFingerprint, KeyPath>(fingerprint, path));

			// Let's try to be smart, if the added key match the scriptPubKey then we are in p2psh p2wpkh
			if (Parent.Settings.IsSmart && redeem_script == null)
			{
				var output = GetCoin();
				if (output != null)
				{
					if (key.WitHash.ScriptPubKey.Hash.ScriptPubKey == output.ScriptPubKey)
					{
						redeem_script = key.WitHash.ScriptPubKey;
					}
				}
			}
		}

		public abstract Coin GetCoin();

		public Coin GetSignableCoin()
		{
			return GetSignableCoin(out _);
		}
		public virtual Coin GetSignableCoin(out string error)
		{
			var coin = GetCoin();
			if (coin == null)
			{
				error = "Impossible to know the TxOut this coin refers to";
				return null;
			}
			if (PayToScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(coin.ScriptPubKey) is ScriptId scriptId)
			{
				if (RedeemScript == null)
				{
					error = "Spending p2sh output but redeem_script is not set";
					return null;
				}

				if (RedeemScript.Hash != scriptId)
				{
					error = "Spending p2sh output but redeem_script is not matching the utxo scriptPubKey";
					return null;
				}

				if (PayToWitTemplate.Instance.ExtractScriptPubKeyParameters2(RedeemScript) is WitProgramParameters prog
					&& prog.NeedWitnessRedeemScript())
				{
					if (WitnessScript == null)
					{
						error = "Spending p2sh-p2wsh output but witness_script is not set";
						return null;
					}
					if (!prog.VerifyWitnessRedeemScript(WitnessScript))
					{
						error = "Spending p2sh-p2wsh output but witness_script does not match redeem_script";
						return null;
					}
					coin = coin.ToScriptCoin(WitnessScript);
					error = null;
					return coin;
				}
				else
				{
					coin = coin.ToScriptCoin(RedeemScript);
					error = null;
					return coin;
				}
			}
			else
			{
				if (RedeemScript != null)
				{
					error = "Spending non p2sh output but redeem_script is set";
					return null;
				}
				if (PayToWitTemplate.Instance.ExtractScriptPubKeyParameters2(coin.ScriptPubKey) is WitProgramParameters prog
					&& prog.NeedWitnessRedeemScript())
				{
					if (WitnessScript == null)
					{
						error = "Spending p2wsh output but witness_script is not set";
						return null;
					}
					if (!prog.VerifyWitnessRedeemScript(WitnessScript))
					{
						error = "Spending p2wsh output but witness_script does not match the scriptPubKey";
						return null;
					}
					coin = coin.ToScriptCoin(WitnessScript);
					error = null;
					return coin;
				}
				else
				{
					error = null;
					return coin;
				}
			}
		}
	}
}
