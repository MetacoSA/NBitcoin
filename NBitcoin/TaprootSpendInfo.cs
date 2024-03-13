using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using NBitcoin.DataEncoders;
#if HAS_SPAN
using NBitcoin.Secp256k1;
#endif
using TaprootMerkleBranch = System.Collections.Generic.List<NBitcoin.uint256>;
using static NBitcoin.TaprootConstants;
using LeafVersion = System.Byte;

namespace NBitcoin
{
#if HAS_SPAN
#nullable enable
	static class EnumerableExtensions
	{
		public static TSource? MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey>? comparer = null)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

			comparer ??= Comparer<TKey>.Default;

			using IEnumerator<TSource> e = source.GetEnumerator();

			if (!e.MoveNext())
			{
				if (default(TSource) is null)
				{
					return default;
				}
				else
				{
					throw new InvalidOperationException($"no elements");
				}
			}

			TSource value = e.Current;
			TKey key = keySelector(value);

			if (default(TKey) is null)
			{
				while (key == null)
				{
					if (!e.MoveNext())
					{
						return value;
					}

					value = e.Current;
					key = keySelector(value);
				}

				while (e.MoveNext())
				{
					TSource nextValue = e.Current;
					TKey nextKey = keySelector(nextValue);
					if (nextKey != null && comparer.Compare(nextKey, key) < 0)
					{
						key = nextKey;
						value = nextValue;
					}
				}
			}
			else
			{
				if (comparer == Comparer<TKey>.Default)
				{
					while (e.MoveNext())
					{
						TSource nextValue = e.Current;
						TKey nextKey = keySelector(nextValue);
						if (Comparer<TKey>.Default.Compare(nextKey, key) < 0)
						{
							key = nextKey;
							value = nextValue;
						}
					}
				}
				else
				{
					while (e.MoveNext())
					{
						TSource nextValue = e.Current;
						TKey nextKey = keySelector(nextValue);
						if (comparer.Compare(nextKey, key) < 0)
						{
							key = nextKey;
							value = nextValue;
						}
					}
				}
			}

			return value;
		}
	}

	public class TaprootScriptLeaf
	{
		internal Script Script { get; }
		internal LeafVersion Version { get; }
		internal TaprootMerkleBranch MerkleBranch { get; } = new ();
		public TaprootScriptLeaf(Script script, LeafVersion version)
		{
			Script = script ?? throw new ArgumentNullException(nameof(script));
			Version = version;
		}

		public uint Depth => (uint)this.MerkleBranch.Count;
		public uint256 LeafHash => Script.TaprootLeafHash(Version);
	}

	/// <summary>
	/// Represents from
	/// </summary>
	public class TaprootNodeInfo
	{
		internal uint256 Hash { get; }
		internal List<TaprootScriptLeaf> Leaves { get; }
		internal bool HasHiddenNodes { get; }

		internal TaprootNodeInfo(uint256 hash, List<TaprootScriptLeaf> leaves, bool hasHiddenNodes)
		{
			Hash = hash ?? throw new ArgumentNullException(nameof(hash));
			Leaves = leaves ?? throw new ArgumentNullException(nameof(leaves));
			HasHiddenNodes = hasHiddenNodes;
		}

		public static TaprootNodeInfo NewLeafWithVersion(Script sc, LeafVersion leafVersion)
		{
			var leaf = new TaprootScriptLeaf
			(
				script: sc,
				version: leafVersion
			);
			return
				new TaprootNodeInfo
				(
					hash: leaf.LeafHash,
					leaves: new List<TaprootScriptLeaf> { leaf },
					hasHiddenNodes: false
				);
		}

		public static TaprootNodeInfo NewHiddenNode(uint256 hash) =>
			new TaprootNodeInfo
			(
				hash: hash,
				leaves: new List<TaprootScriptLeaf>(),
				hasHiddenNodes: true
			);

		/// <summary>
		/// Combines two `NodeInfo` to create a new parent.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static TaprootNodeInfo operator +(TaprootNodeInfo a, TaprootNodeInfo b)
		{
			var allLeaves = new List<TaprootScriptLeaf>(a.Leaves.Count + b.Leaves.Count);
			foreach (var aLeaf in a.Leaves)
			{
				aLeaf.MerkleBranch.Add(b.Hash); // add hashing parameter.
				allLeaves.Add(aLeaf);
			}
			foreach (var bLeaf in b.Leaves)
			{
				bLeaf.MerkleBranch.Add(a.Hash);
				allLeaves.Add(bLeaf);
			}

			using SHA256 sha = new SHA256();
			sha.InitializeTagged("TapBranch");
			if (CompareLexicographic(a.Hash, b.Hash))
			{
				sha.Write(a.Hash.ToBytes());
				sha.Write(b.Hash.ToBytes());
			}
			else
			{
				sha.Write(b.Hash.ToBytes());
				sha.Write(a.Hash.ToBytes());
			}
			return new TaprootNodeInfo
			(
				hash: new uint256(sha.GetHash()),
				leaves: allLeaves,
				hasHiddenNodes: a.HasHiddenNodes || b.HasHiddenNodes
			);
		}
		static bool CompareLexicographic(uint256 a, uint256 b)
		{
			Span<byte> ab = stackalloc byte[32];
			Span<byte> bb = stackalloc byte[32];
			a.ToBytes(ab);
			b.ToBytes(bb);
			for (int i = 0; i < ab.Length && i < bb.Length; i++)
			{
				if (ab[i] < bb[i])
					return true;
				if (bb[i] < ab[i])
					return false;
			}
			return true;
		}
	}

	public class ControlBlock : IEquatable<ControlBlock>
	{
		public byte LeafVersion { get; }
		public bool OutputParityIsOdd { get; }
		public TaprootInternalPubKey InternalPubKey { get; }
		public TaprootMerkleBranch MerkleBranch { get; }

		private static HexEncoder _hex = new HexEncoder();

		public ControlBlock(byte leafVersion, bool outputParityIsOdd, TaprootInternalPubKey internalPubKey, TaprootMerkleBranch merkleBranch)
		{
			LeafVersion = leafVersion;
			OutputParityIsOdd = outputParityIsOdd;
			InternalPubKey = internalPubKey ?? throw new ArgumentNullException(nameof(internalPubKey));
			MerkleBranch = merkleBranch ?? throw new ArgumentNullException(nameof(merkleBranch));
		}

		public static ControlBlock FromHex(string hex)
			=> FromSlice(_hex.DecodeData(hex));
		public static ControlBlock FromSlice(ReadOnlySpan<byte> buffer)
		{
			if (buffer.Length < TAPROOT_CONTROL_BASE_SIZE ||
			    (buffer.Length - TAPROOT_CONTROL_BASE_SIZE) % TAPROOT_CONTROL_NODE_SIZE != 0)
				throw new FormatException($"Invalid control block size {buffer.Length}");

			var outputKeyParity = (buffer[0] & 1) == 1;
			var leafVersion = buffer[0] & (byte)TAPROOT_LEAF_MASK;
			if (((byte)leafVersion & 0b_0000_0001) == 1)
				throw new FormatException($"Invalid LeafBytes: Odd leaf version {leafVersion}");
			if (leafVersion == TAPROOT_LEAF_ANNEX)
				throw new FormatException($"Invalid LeafBytes: annex {leafVersion}");
			if (!TaprootInternalPubKey.TryCreate(buffer[1..TAPROOT_CONTROL_BASE_SIZE], out var internalPubKey))
				throw new FormatException($"Failed to parse Internal pubkey for control block");

			var merkleBranch = new TaprootMerkleBranch();
			int index = TAPROOT_CONTROL_BASE_SIZE;
			while (index <= buffer.Length - TAPROOT_CONTROL_NODE_SIZE)
			{
				var hash = new uint256(buffer.Slice(index, TAPROOT_CONTROL_NODE_SIZE));
				merkleBranch.Add(hash);
				index += TAPROOT_CONTROL_NODE_SIZE;
			}
			return new ControlBlock
			(
				outputParityIsOdd: outputKeyParity,
				leafVersion: (byte)leafVersion,
				internalPubKey: internalPubKey,
				merkleBranch: merkleBranch
			);
		}

		public byte[] ToBytes()
		{
			var buf = new byte[TAPROOT_CONTROL_BASE_SIZE + (TAPROOT_CONTROL_NODE_SIZE * MerkleBranch.Count)];
			buf[0] = (byte)((byte)(OutputParityIsOdd ? 1 : 0) | LeafVersion);
			InternalPubKey.ToBytes().CopyTo(buf, 1);
			foreach (var (hash, i) in MerkleBranch.Select((v, i) => (v, i)))
			{
				var index = (i * TAPROOT_CONTROL_NODE_SIZE);
				hash.ToBytes().CopyTo(buf, TAPROOT_CONTROL_BASE_SIZE + index);
			}
			return buf;
		}

		/// <summary>
		/// Verifies that a control block is correct proof for a given output key and script.
		///
		/// Only checks that script is contained inside the taptree described by output key.
		/// Full verification must also execute the script with witness data.
		/// </summary>
		/// <returns></returns>
		public bool VerifyTaprootCommitment(TaprootFullPubKey outputKey, Script script)
		{
			var merkleRoot = ScriptEvaluationContext.ComputeTaprootMerkleRoot(this.ToBytes(), script.TaprootV1LeafHash);
			return outputKey.CheckTapTweak(InternalPubKey, merkleRoot, OutputParityIsOdd);
		}

		public bool Equals(ControlBlock? other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return LeafVersion == other.LeafVersion && OutputParityIsOdd == other.OutputParityIsOdd && InternalPubKey.Equals(other.InternalPubKey) && MerkleBranch.SequenceEqual(other.MerkleBranch);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((ControlBlock)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(LeafVersion, OutputParityIsOdd, InternalPubKey, MerkleBranch);
		}
	}


	/// <summary>
	/// Mostly taken from [rust-bitcoin](https://github.com/rust-bitcoin/rust-bitcoin/blob/1d0b0e6ed8e80767fef9b00a38c9e5fefc34baa0/bitcoin/src/util/taproot.rs#L184)
	/// </summary>
	public class TaprootSpendInfo
	{
		public TaprootInternalPubKey InternalPubKey { get; }
		public TaprootFullPubKey OutputPubKey { get; }

		/// <summary>
		/// map from script to -> its merkle proof
		/// More than one control block for a given script is only possible if it appears in multiple branches of the tree.
		/// In all cases, keeping one should be enough for spending funds, but we keep all of the paths
		/// so that a full tree can be constructed again from spending data if required.
		/// </summary>
		internal ConcurrentDictionary<(Script, LeafVersion), List<TaprootMerkleBranch>> ScriptToMerkleProofMap { get; }
		public uint256? MerkleRoot { get; }

		public bool IsKeyPathOnlySpend => this.ScriptToMerkleProofMap.IsEmpty;

		public TaprootSpendInfo(
			TaprootInternalPubKey internalPubKey,
			TaprootFullPubKey outputPubKey,
			Dictionary<(Script, LeafVersion), List<TaprootMerkleBranch>> scriptToMerkleProofMap,
			uint256? merkleRoot
			)
		{
			if (internalPubKey is null) throw new ArgumentNullException(nameof(internalPubKey));
			if (outputPubKey == null) throw new ArgumentNullException(nameof(outputPubKey));
			if (scriptToMerkleProofMap == null) throw new ArgumentNullException(nameof(scriptToMerkleProofMap));
			var map = new ConcurrentDictionary<(Script, LeafVersion), List<TaprootMerkleBranch>>();
			foreach (var kv in scriptToMerkleProofMap)
			{
				map.AddOrReplace(kv.Key, kv.Value);
			}
			InternalPubKey = internalPubKey ?? throw new ArgumentNullException(nameof(internalPubKey));
			OutputPubKey = outputPubKey;
			MerkleRoot = merkleRoot;
			ScriptToMerkleProofMap = map;
		}

		public static TaprootSpendInfo CreateKeySpend(TaprootInternalPubKey internalPubKey, uint256? merkleRoot = null)
		{
			var outputKey = internalPubKey.GetTaprootFullPubKey(merkleRoot);
			return new TaprootSpendInfo(internalPubKey, outputKey,
				new Dictionary<(Script, LeafVersion), List<TaprootMerkleBranch>>(), merkleRoot);
		}


		public static TaprootSpendInfo FromNodeInfo(TaprootInternalPubKey internalPubKey, TaprootNodeInfo node)
		{
			var rootHash = node.Hash;
			var info = TaprootSpendInfo.CreateKeySpend(internalPubKey, rootHash);
			foreach (var leaves in node.Leaves)
			{
				var k = (leaves.Script, leaves.Version);
				var v = leaves.MerkleBranch;
				if (info.ScriptToMerkleProofMap.TryGetValue(k, out var set))
					set.Add(v);
				var hashSet = new List<List<uint256>> { v };
				info.ScriptToMerkleProofMap.TryAdd(k, hashSet);
			}
			return info;
		}

		public static TaprootSpendInfo WithHuffmanTree(TaprootInternalPubKey internalPubKey, params (UInt32, Script)[] scriptWeights) =>
			TaprootBuilder.WithHuffmanTree(scriptWeights).Finalize(internalPubKey);

		/// <summary>
		/// Constructs a ControlBlock for particular script with the given version.
		/// </summary>
		/// <returns>
		/// If there are multiple control blocks possible, gets the shortest one.
		/// If the script is not contained in the `TaprootSpendInfo` false.
		/// </returns>
		public bool TryGetControlBlock(Script script, LeafVersion version, [MaybeNullWhen(false)] out ControlBlock controlBlock)
		{
			controlBlock = null;
			if (!this.ScriptToMerkleProofMap.TryGetValue((script, version), out var merkleBranchSet))
				return false;

			// choose the smallest merkle proof.
			var smallest = merkleBranchSet.MinBy(x => x.Count);
			controlBlock = new ControlBlock
			(
				outputParityIsOdd:
					this.OutputPubKey.OutputKeyParity,
				internalPubKey: InternalPubKey,
				leafVersion: version,
				merkleBranch: smallest!
			);
			return true;
		}

		public ControlBlock GetControlBlock(Script script, LeafVersion version)
		{
			if (!TryGetControlBlock(script, version, out var ctrl))
				throw new InvalidDataException($"Failed to get control block for script: {script}, version: {version}");
			return ctrl;
		}

	}

	/// <summary>
	/// Builder for building taproot iteratively.
	/// Mostly taken from TaprootBuilder in r[ust-bitcoin](https://github.com/rust-bitcoin/rust-bitcoin/blob/1d0b0e6ed8e80767fef9b00a38c9e5fefc34baa0/bitcoin/src/util/taproot.rs#L343)
	/// And in [bitcoin](https://github.com/bitcoin/bitcoin/blob/7ef730ca84bd71a06f986ae7070e7b2ac8e47582/src/script/standard.h#L226)
	/// Currently it is only usable in netx.0 (x >=6), because we want to use PriorityQueue in .NET 6
	/// </summary>
	public class TaprootBuilder
	{

		internal List<TaprootNodeInfo?> Branch
		{
			get;
		} = new();

		/// <summary>
		/// Creates a new `TaprootBuilder` from a list of scripts (with default script version) and weights of
		/// satisfaction for that script.
		///
		/// The weights represent the probability of each branch being taken. If probability/weights
		/// for each condition are known, constructing the tree as a Huffman Tree is the optimal way to
		/// minimize average case satisfaction cost. This function takes as input an iterator of
		/// `(uint32, Script)` where `uint32` represents the satisfaction weights of the branch. For
		/// example, [(3, S1), (2, S2), (5, S3)] would construct a TapTree that has optimal satisfaction
		/// weight when probability for S1 is 30%, S2 is 20%, and S3 is 50%.
		///
		/// # Errors:
		///
		/// - When the optimal Huffman Tree has a depth more than 128.
		/// - If the provided list of script weights is empty.
		///
		/// # Edge Cases:
		///
		/// If the script weight calculations overflow, a sub-optimal tree may be generated. This should
		/// not happen unless you are dealing with billions of branches with weights close to 2^32.
		/// </summary>
		/// <returns></returns>
		public static TaprootBuilder WithHuffmanTree(params (UInt32, Script)[] scriptWeights)
		{
			if (scriptWeights == null) throw new ArgumentNullException(nameof(scriptWeights));
			if (scriptWeights.Length == 0) throw new ArgumentException("Scripts has 0 length.", nameof(scriptWeights));
			var nodeWeights = new PriorityQueue<(UInt32, TaprootNodeInfo), UInt32>(initialCapacity:scriptWeights.Length);

			foreach (var (p, leaf) in scriptWeights)
			{
				var nodeInfo = TaprootNodeInfo.NewLeafWithVersion(leaf, (byte)TAPROOT_LEAF_TAPSCRIPT);
				nodeWeights.Enqueue((p, nodeInfo), p);
			}

			while (nodeWeights.Count > 1)
			{
				// combine the last two elements and insert a new node

				var (p1, s1) = nodeWeights.Dequeue();
				var (p2, s2) = nodeWeights.Dequeue();

				// Insert the sum of first two in the tree as a new node
				var p = p1 + p2;
				nodeWeights.Enqueue((p, s1 + s2), p);
			}

			// Every iteration of the loop reduces the nodeWeights.Count by exactly 1,
			// therefore, the loop will eventually terminate with exactly 1 element.
			Debug.Assert(nodeWeights.Count == 1);
			var node = nodeWeights.Dequeue().Item2;
			var builder = new TaprootBuilder();
			builder.Branch.Add(node);
			return builder;
		}


		/// <summary>
		/// Adds a leaf script at `depth` to the builder
		/// </summary>
		/// <param name="depth"></param>
		/// <param name="script"></param>
		/// <param name="version"></param>
		/// <returns></returns>
		public TaprootBuilder AddLeaf(uint depth, Script script, LeafVersion version) =>
			Insert(TaprootNodeInfo.NewLeafWithVersion(script, version), depth);

		public TaprootBuilder AddLeaf(uint depth, Script script) =>
			AddLeaf(depth, script, (byte)TAPROOT_LEAF_TAPSCRIPT);
		/// <summary>
		/// Adds a hidden/omitted node at `depth` to the builder. Errors if the leaves are not provided in DFS walk order.
		/// The depth of the root node is 0.
		/// <param name="depth"></param>
		/// <param name="hash"></param>
		/// <returns></returns>
		public TaprootBuilder AddHiddenNode(uint depth, uint256 hash) =>
			Insert(TaprootNodeInfo.NewHiddenNode(hash), depth);

		public bool IsFinalizable => Branch.Count == 0 || (Branch.Count == 1 && Branch[0] is not null);

		public bool HasHiddenNodes => Branch.Any(node => node is not null && node.HasHiddenNodes);

		/// <summary>
		/// Finalize the builder and returns resulted TaprootSpendInfo.
		/// You might want to check `IsFinalizable` before calling this method.
		/// </summary>
		/// <param name="internalPubKey"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public TaprootSpendInfo Finalize(TaprootInternalPubKey internalPubKey) =>
			Branch.Count == 0
				? TaprootSpendInfo.CreateKeySpend(internalPubKey, null)
				: Branch.Count == 1
					? TaprootSpendInfo.FromNodeInfo(internalPubKey, Branch.Last()!)
					: throw new InvalidOperationException($"Failed to finalize, maybe you forgot to add some scripts? Branch.Count: {Branch.Count}");

		private TaprootBuilder Insert(TaprootNodeInfo node, uint depth)
		{
			if (depth > TAPROOT_CONTROL_MAX_NODE_COUNT)
				throw new InvalidDataException($"Invalid Merkle Tree Depth {depth}");

			// We cannot insert a leaf at a lower depth while a deeper branch is unfinished. Doing
			// so would mean the AddLeaf/AddHidden invocations do not correspond to a DFS traversal
			// of a binary tree
			if (depth + 1u < Branch.Count)
				throw new InvalidDataException();

			while (Branch.Count == depth + 1)
			{
				var child = Branch.Last();
				if (child is null)
					break;
				Branch.RemoveAt(Branch.Count - 1);

				if (depth == 0)
					// we are trying to combine two nodes at root level.
					// Can't propagate further  up than the root
					throw new InvalidOperationException($"We can not combine two nodes at the root level.");
				node = node + child;
				depth--;
			}

			if (Branch.Count < depth + 1)
			{
				// Add enough nodes so that we can insert node at depth `depth`
				var numExtraNodes = depth + 1 - Branch.Count;
				for (var i = 0; i < numExtraNodes; i++)
					Branch.Add(null);
			}
			// Push the last node to the branch
			Branch[(int)depth] = node;
			return this;
		}
	}
#endif
}
