namespace NBitcoin.Scripting
{
    /** Interface for public key objects in descriptors. */
    public abstract class  PubkeyProvider
	{
		//! Index of this key expression in the descriptor
		//! E.g. If this PubkeyProvider is key1 in multi(2, key1, key2, key3), then m_expr_index = 0
		protected uint m_expr_index;

		public PubkeyProvider(uint exp_index) 
		{
			m_expr_index = exp_index;
		}

		public abstract ~PubkeyProvider();

		/** Derive a public key.
		*  read_cache is the cache to read keys from (if not null)
		*  write_cache is the cache to write keys to (if not null)
		*  Caches are not exclusive but this is not tested. Currently we use them exclusively
		*/
		public abstract bool GetPubKey(int pos, SigningProvider arg, PubKey key, KeyOriginInfo info, DescriptorCache read_cache = null, DescriptorCache write_cache = null);

		/** Whether this represent multiple public keys at different positions. */
		public abstract bool IsRange();

		/** Get the size of the generated public key(s) in bytes (33 or 65). */
		public abstract uint GetSize();

		/** Get the descriptor string form. */
		public abstract string ToString();

		/** Get the descriptor string form including private data (if available in arg). */
		public abstract bool ToPrivateString(SigningProvider arg, string output);

		/** Derive a private key, if private data is available in arg. */
		public abstract bool GetPrivKey(int pos, SigningProvider arg, Key key);
	};
                } 