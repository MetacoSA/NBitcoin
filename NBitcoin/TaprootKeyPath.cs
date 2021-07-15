#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class TaprootKeyPath
	{
		public TaprootKeyPath(RootedKeyPath rootedKeyPath) : this(rootedKeyPath, null)
		{

		}
		public TaprootKeyPath(RootedKeyPath rootedKeyPath, uint256[]? leafHashes)
		{
			if (rootedKeyPath == null)
				throw new ArgumentNullException(nameof(rootedKeyPath));
#if HAS_SPAN
			LeafHashes = leafHashes ?? Array.Empty<uint256>();
#else
			LeafHashes = leafHashes ?? new uint256[0];
#endif
			RootedKeyPath = rootedKeyPath;
		}
		public uint256[] LeafHashes { get; }
		public RootedKeyPath RootedKeyPath { get; }

		public override string ToString()
		{
			return $"{RootedKeyPath} with {LeafHashes.Length} leaf hashes";
		}
	}
}
