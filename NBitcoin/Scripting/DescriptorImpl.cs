using System.Collections.Generic;

namespace NBitcoin.Scripting
{
    /** Base class for all Descriptor implementations. */
    public class DescriptorImpl : Descriptor
	{
		//! Public key arguments for this descriptor (size 1 for PK, PKH, WPKH; any size for Multisig).
		private List<std::unique_ptr<PubkeyProvider>> m_pubkey_args;
		//! The string name of the descriptor function.
		private string m_name;

		//! The sub-descriptor argument (null for everything but SH and WSH).
		//! In doc/descriptors.m this is referred to as SCRIPT expressions sh(SCRIPT)
		//! and wsh(SCRIPT), and distinct from KEY expressions and ADDR expressions.
		protected std::unique_ptr<DescriptorImpl> m_subdescriptor_arg;

		//! Return a serialization of anything except pubkey and script arguments, to be prepended to those.
		protected virtual string ToStringExtra() { return ""; }

		/** A helper function to construct the scripts for this descriptor.
		*
		*  This function is invoked once for every Script produced by evaluating
		*  m_subdescriptor_arg, or just once in case m_subdescriptor_arg is null.

		*  @param pubkeys The evaluations of the m_pubkey_args field.
		*  @param script The evaluation of m_subdescriptor_arg (or null when m_subdescriptor_arg is null).
		*  @param out A FlatSigningProvider to put scripts or public keys in that are necessary to the solver.
		*             The script arguments to this function are automatically added, as is the origin info of the provided pubkeys.
		*  @return A vector with scriptPubKeys for this descriptor.
		*/
		protected virtual List<Script> MakeScripts(List<PubKey> pubkeys, Script* script, FlatSigningProvider output);

		public DescriptorImpl(List<std::unique_ptr<PubkeyProvider>> pubkeys, std::unique_ptr<DescriptorImpl> script, string name) 
		{
			m_pubkey_args = pubkeys; 
			m_name = name; 
			m_subdescriptor_arg = script;
		}

		public bool IsSolvable() 
		{
			if (m_subdescriptor_arg) {
				if (!m_subdescriptor_arg.IsSolvable()) return false;
			}
			return true;
		}

		public bool IsRange()
		{
			foreach (var pubkey in m_pubkey_args) {
				if (pubkey.IsRange()) return true;
			}
			if (m_subdescriptor_arg) {
				if (m_subdescriptor_arg.IsRange()) return true;
			}
			return false;
		}

		public bool ToStringHelper(SigningProvider* arg, out string output, bool priv)
		{
			string extra = ToStringExtra();
			uint pos = extra.size() > 0 ? 1 : 0;
			string ret = m_name + "(" + extra;
			foreach (var pubkey in m_pubkey_args) {
				if (pos++) ret += ",";
				string tmp;
				if (priv) {
					if (!pubkey.ToPrivateString(*arg, tmp)) return false;
				} else {
					tmp = pubkey.ToString();
				}
				ret += tmp;
			}
			if (m_subdescriptor_arg) {
				if (pos++) ret += ",";
				string tmp;
				if (!m_subdescriptor_arg.ToStringHelper(arg, tmp, priv)) return false;
				ret += tmp;
			}
			output = ret + ")";
			return true;
		}

		public override string ToString()
		{
			string ret;
			ToStringHelper(null, ret, false);
			return AddChecksum(ret);
		}

		public bool ToPrivateString(SigningProvider arg, out string outputput)
		{
			bool ret = ToStringHelper(arg, out, true);
			outpur = AddChecksum(out);
			return ret;
		}

		public bool ExpandHelper(int pos, SigningProvider arg, DescriptorCache read_cache, List<Script> output_scripts, out FlatSigningProvider output, DescriptorCache write_cache)
		{
			List<std::pair<PubKey, KeyOriginInfo>> entries;
			entries.reserve(m_pubkey_args.size());

			// Construct temporary data in `entries` and `subscripts`, to avoid producing output in case of failure.
			for (auto p : m_pubkey_args) {
				entries.emplace_back();
				if (!p.GetPubKey(pos, arg, entries.back().first, entries.back().second, read_cache, write_cache)) return false;
			}
			List<Script> subscripts;
			if (m_subdescriptor_arg) {
				FlatSigningProvider subprovider;
				if (!m_subdescriptor_arg.ExpandHelper(pos, arg, read_cache, subscripts, subprovider, write_cache)) return false;
				out = Merge(out, subprovider);
			}

			List<PubKey> pubkeys;
			pubkeys.reserve(entries.size());
			for (auto entry : entries) {
				pubkeys.push_back(entry.first);
				out.origins.emplace(entry.first.GetID(), std::make_pair<PubKey, KeyOriginInfo>(PubKey(entry.first), entry.second));
			}
			if (m_subdescriptor_arg) {
				for (auto subscript : subscripts) {
					out.scripts.emplace(ScriptID(subscript), subscript);
					List<Script> addscripts = MakeScripts(pubkeys, subscript, output);
					for (auto addscript : addscripts) {
						output_scripts.push_back(addscript);
					}
				}
			} else {
				output_scripts = MakeScripts(pubkeys, null, output);
			}
			return true;
		}

		public bool Expand(int pos, SigningProvider provider, List<Script> output_scripts, FlatSigningProvider out, DescriptorCache write_cache = null)
		{
			return ExpandHelper(pos, provider, null, output_scripts, out, write_cache);
		}

		public bool ExpandFromCache(int pos, DescriptorCache read_cache, List<Script> output_scripts, FlatSigningProvider output)
		{
			return ExpandHelper(pos, DUMMY_SIGNING_PROVIDER, read_cache, output_scripts, out, null);
		}

		public void ExpandPrivate(int pos, SigningProvider provider, FlatSigningProvider output)
		{
			for (auto p : m_pubkey_args) {
				Key key;
				if (!p.GetPrivKey(pos, provider, key)) continue;
				out.keys.emplace(key.GetPubKey().GetID(), key);
			}
			if (m_subdescriptor_arg) {
				FlatSigningProvider subprovider;
				m_subdescriptor_arg.ExpandPrivate(pos, provider, subprovider);
				out = Merge(out, subprovider);
			}
		}

		Optional<OutputType> GetOutputType()  { return nullopt; }
	}
                } 