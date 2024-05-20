using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using NBitcoin.DataEncoders;
#if HAS_SPAN
using NBitcoin.Secp256k1;
#endif

#nullable enable

namespace NBitcoin.Scripting
{
#if HAS_SPAN
	internal static class TaprootExtensions
	{

		public static (int, List<TaprootPubKey>)? FindMultiATemplate(this Script sc)
		{
			var b = sc.ToBytes();
			// Redundant, but very fast and selective test.
			if (b.Length == 0 || b[0] != 32 || b.Last() != (byte)(OpcodeType.OP_NUMEQUAL))
				return null;

			var keySpans = new List<TaprootPubKey>();
			int m = -1;
			var ops = sc.ToOps().ToList();
			var opsLength = ops.Count;
			foreach (var (op, i) in ops.Select((v, i) => (v, i)))
			{
				if (i == 1)
				{
					if (op.Code != OpcodeType.OP_CHECKSIG)
						return null;
				}
				else if (opsLength - 2 == i)
				{
					var maybeM = op.GetInt();
					if (maybeM == null)
						return null;
					m = maybeM.Value;
				}
				else if (opsLength - 1 == i)
				{
					if (op.Code != OpcodeType.OP_NUMEQUAL)
						return null;
				}
				else if (i % 2 == 0)
				{
					// must be pubkey push
					if (!TaprootPubKey.TryCreate(op.PushData, out var pk))
						return null;
					keySpans.Add(pk);
				}
				else
				{
					if (op.Code != OpcodeType.OP_CHECKSIGADD)
						return null;
				}
			}

			return (m, keySpans);
		}

		internal class TreeNode
		{
			internal uint256? Hash { get; set; }
			internal TaprootScriptLeaf? Leaf;
			internal Tuple<TreeNode, TreeNode>? Sub;

			/// <summary>
			/// Whether or not this node has been explored (is known to be a leaf, or known to have children).
			/// </summary>
			internal bool Explored;

			/// <summary>
			/// Whether or not this node is an inner node (unknown until explored = true)
			/// </summary>
			internal bool Inner;
			/// <summary>
			/// Whether or not we have produced output fo this subtree.
			/// </summary>
			internal bool Done;
		}

		public static bool InferTaprootTree(this TaprootSpendInfo info, TaprootPubKey outputKey, [MaybeNullWhen(false)]out List<Tuple<int, TapScript>> result)
		{
			result = null;
			if (!outputKey.CheckTapTweak(info.InternalPubKey, info.MerkleRoot, info.OutputPubKey.OutputKeyParity))
				throw new ArgumentException($"TapTweak mismatch (outputKey: {outputKey}, internalKey: {info.InternalPubKey}, merkleRoot: {info.MerkleRoot})");

			result = new List<Tuple<int, TapScript>>();
			if (info.IsKeyPathOnlySpend)
				return true;

			var root = new TreeNode();
			root.Hash = info.MerkleRoot;

			foreach (var kv in info.ScriptToMerkleProofMap)
			{
				foreach (var control in kv.Value)
				{
					var script = kv.Key;
					var controlB = control.Select(h => h.ToBytes()).SelectMany(x => x).ToArray();
					// Skip script records with invalid control block size.
					if (controlB.Length % TaprootConstants.TAPROOT_CONTROL_NODE_SIZE != 0)
					{
						Debug.Fail($"invalid control block size {controlB.Length}");
						continue;
					}

					var leafHash = new TaprootScriptLeaf(script).LeafHash;

					TreeNode node = root;
					var levels = controlB.Length/ TaprootConstants.TAPROOT_CONTROL_NODE_SIZE;
					for (int depth = 0; depth < levels; depth++)
					{
						// Can't descend into a node which we already know is a leaf.
						Debug.Assert(!(node.Explored && !node.Inner));

						// Extract partner hash from Merkle branch in control block.
						byte[] hashB = new byte[32];
						controlB.AsSpan((levels - 1 - depth) * TaprootConstants.TAPROOT_CONTROL_NODE_SIZE, 32).CopyTo(hashB);
						var hash = new uint256(hashB);
						if (node.Sub?.Item1 is not null)
						{
							bool desc = false;
							// Descend into the existing left or right branch.
							if (node.Sub.Item1.Hash == hash ||
								(node.Sub.Item1.Hash is null && node.Sub.Item2!.Hash != hash))
							{
								node.Sub.Item1!.Hash = hash;
								node = node.Sub.Item2!;
								desc = true;
							}
							else if (node.Sub?.Item2.Hash == hash ||
								(node.Sub?.Item2.Hash is null && node.Sub?.Item1.Hash != hash))
							{
								node.Sub!.Item2.Hash ??= hash;
								node = node.Sub.Item1!;
								desc = true;
							}

							if (!desc) return false;
						}
						else
						{
							node.Explored = true;
							node.Inner = true;
							node.Sub = new Tuple<TreeNode, TreeNode>(new TreeNode(), new TreeNode());
							node.Sub.Item2.Hash = hash;
							node = node.Sub.Item1;
						}
					}

					// cannot turn a known inner node into a leaf.
					if (node.Sub?.Item1 is not null) return false;
					node.Explored = true;
					node.Inner = false;
					node.Leaf = new TaprootScriptLeaf(script);
					node.Hash = leafHash;
				}
			}


			Func<TreeNode, bool> hasIdenticalSubtree = (node) =>
			{
				Debug.Assert(node.Inner);
				using SHA256 sha = new SHA256();
				sha.InitializeTagged("TapBranch");
				sha.Write(node.Sub!.Item2.Hash!.ToBytes());
				sha.Write(node.Sub!.Item2.Hash!.ToBytes());
				var computedParent = sha.GetHash();
				var actualParent = node.Hash!.ToBytes();
				return computedParent.SequenceEqual(actualParent);
			};
			// Recursive processing to turn the tree into flattened output. Use an explicit stack
			// here to aovid overflowing the call stack (the tree may be 128 levels deep).
			var stack = new Stack<TreeNode>();
			stack.Push(root);
			while (stack.Count != 0)
			{
				var node = stack.Peek();
				// Un explored node, which means the tree is incomplete.
				if (!node.Explored)
				{
					Debug.Fail("Unexplored node");
					return false;
				}
				else if (!node.Inner)
				{
					result.Add(new Tuple<int, TapScript>((int)stack.Count - 1, node.Leaf!.Script));
					node.Done = true;
					stack.Pop();
				}
				else if (
					node.Sub is not null &&
					node.Sub.Item1.Done && !node.Sub.Item2.Done &&
					!node.Sub.Item2.Explored && !(node.Sub.Item2.Hash is null) &&
					hasIdenticalSubtree(node)
				)
				{
					// Whenever there are nodes with two identical subtrees under it, we run into a problem:
					// the control blocks for the leaves underneath those will be identical as well, and thus
					// they will all be matched to the same path in the tree. The result is that at the location
					// where the duplicate occurred, the left child will contain a normal tree that can be explored
					// and processed, but the right one will remain unexplored.
					//
					// This situation can be detected, by encountering an inner node with unexplored right subtree
					// with known hash, and H_TapBranch(hash, hash) is equal to the parent node (this node)'s hash.
					//
					// To deal with this, simply process the left tree a second time (set its done flag to false;
					// noting that the done flag of its children have already been set to false after processing
					// those). To avoid ending up in an infinite loop, set the done flag of the right (unexplored)
					// subtree to true.
					node.Sub.Item1.Done = false;
					node.Sub.Item2.Done = true;
				}
				else if (node.Sub is not null && node.Sub.Item1.Done && node.Sub.Item2.Done)
				{
					// an internal node which we're finished with.
					node.Sub.Item1.Done = false;
					node.Sub.Item2.Done = false;
					node.Done = true;
					stack.Pop();
				}
				else if (node.Sub is {} sub)
				{
					if (!sub.Item1.Done)
					{
						// An internal node whose left branch hasn't been processed yet. Do so first.
						stack.Push(sub.Item1);
					}
					else if (!sub.Item2.Done)
					{
						stack.Push(sub.Item2);
					}
				}
			}
			return true;
		}
	}

#endif

	public abstract class OutputDescriptor : IEquatable<OutputDescriptor>
	{
		private OutputDescriptor(Network network)
		{
			Network = network;
		}
		public Network Network { get; }
		#region subtypes
		public class Addr : OutputDescriptor
		{
			public IDestination Address { get; }
			public Addr(IDestination address, Network network) : base(network)
			{
				if (address == null)
					throw new ArgumentNullException(nameof(address));
				Address = address;
			}
		}

		public class Raw : OutputDescriptor
		{
			public Script Script;

			internal Raw(Script script, Network network) : base(network)
			{
				if (script is null)
					throw new ArgumentNullException(nameof(script));
				if (script.Length == 0)
					throw new ArgumentException($"{nameof(script)} must not be empty!");
				Script = script;
			}
		}

		public class PK : OutputDescriptor
		{
			public PubKeyProvider PkProvider;
			internal PK(PubKeyProvider pkProvider, Network network) : base(network)
			{
				if (pkProvider == null)
					throw new ArgumentNullException(nameof(pkProvider));
				PkProvider = pkProvider;
			}
		}

		public class PKH : OutputDescriptor
		{
			public PubKeyProvider PkProvider;
			internal PKH(PubKeyProvider pkProvider, Network network) : base(network)
			{
				if (pkProvider == null)
					throw new ArgumentNullException(nameof(pkProvider));
				PkProvider = pkProvider;
			}
		}

		public class WPKH : OutputDescriptor
		{
			public PubKeyProvider PkProvider;
			internal WPKH(PubKeyProvider pkProvider, Network network) : base(network)
			{
				if (pkProvider == null)
					throw new ArgumentNullException(nameof(pkProvider));
				PkProvider = pkProvider;
			}
		}
		public class Combo : OutputDescriptor
		{
			public PubKeyProvider PkProvider;
			internal Combo(PubKeyProvider pkProvider, Network network) : base(network)
			{
				if (pkProvider == null)
					throw new ArgumentNullException(nameof(pkProvider));
				PkProvider = pkProvider;
			}
		}

		public class Multi : OutputDescriptor
		{
			public List<PubKeyProvider> PkProviders;
			internal Multi(uint threshold, IEnumerable<PubKeyProvider> pkProviders, bool isSorted, Network network, bool isTapScript) : base(network)
			{
				if (pkProviders == null)
					throw new ArgumentNullException(nameof(pkProviders));
				PkProviders = pkProviders.ToList();
				if (PkProviders.Count == 0)
					throw new ArgumentException("Multisig Descriptor can not have empty pubkey providers");
				Threshold = threshold;
				IsSorted = isSorted;
				IsTapScript = isTapScript;
			}

			public uint Threshold { get; }
			public bool IsSorted { get; }
			public bool IsTapScript { get; }
		}

		public class SH : OutputDescriptor
		{
			public OutputDescriptor Inner;
			internal SH(OutputDescriptor inner, Network network) : base(network)
			{
				if (inner == null)
					throw new ArgumentNullException(nameof(inner));
				if (inner.IsTopLevelOnly())
					throw new ArgumentException($"{inner} can not be inner element for SH");
				Inner = inner;
			}
		}

		public class WSH : OutputDescriptor
		{
			public OutputDescriptor Inner;
			internal WSH(OutputDescriptor inner, Network network) : base(network)
			{
				if (inner == null)
					throw new ArgumentNullException(nameof(inner));
				if (inner.IsTopLevelOnly() || inner is WSH)
					throw new ArgumentException($"{inner} can not be inner element for WSH");
				Inner = inner;
			}
		}

#if HAS_SPAN
		/// <summary>
		/// binary tree to represent MAST in Taproot script for descriptor.
		/// </summary>
		public abstract class TapTree
		{
			public class Leaf : TapTree
			{
				public OutputDescriptor Inner { get; }

				internal Leaf(OutputDescriptor inner)
				{
					Inner = inner;
				}
			}

			public class Tree : TapTree
			{
				public TapTree Child1 { get; }
				public TapTree Child2 { get; }

				internal Tree(TapTree child1, TapTree child2)
				{
					Child1 = child1;
					Child2 = child2;
				}
			}

			public static TapTree NewLeaf(OutputDescriptor inner) => new Leaf(inner);
			public static TapTree NewTree(TapTree child1, TapTree child2) => new Tree(child1, child2);

			public override string ToString() => this switch
			{
				Tree self =>
					$"{{{self.Child1},{self.Child2}}}",
				Leaf self =>
					$"{self.Inner.ToStringHelper()}", // we don't need checksum here so do not use `ToString`
				_ => throw new Exception($"Unreachable type {GetType()}")
			};


			private bool TryGetPrivateStringCore(
				ISigningRepository secretProvider,
				StringBuilder sb
			)
			{
				switch (this)
				{
					case Tree self:
					{
						sb.Append("{");
						if (!self.Child1.TryGetPrivateStringCore(secretProvider, sb))
							return false;
						sb.Append(",");
						if (!self.Child2.TryGetPrivateStringCore(secretProvider, sb))
							return false;
						sb.Append("}");
						return true;
					}
					case Leaf self:
					{
						if (!self.Inner.TryGetPrivateStringHelper(secretProvider, out var substring))
							return false;
						sb.Append(substring);
						return true;
					}
				}
				throw new Exception($"Unreachable");
			}

			internal bool TryGetPrivateString(
				ISigningRepository secretProvider,
				[MaybeNullWhen(false)] out string ret
			)
			{
				ret = null;
				var sb = new StringBuilder();
				if (!TryGetPrivateStringCore(secretProvider, sb))
					return false;
				ret = sb.ToString();
				return true;
			}

			private IEnumerable<(OutputDescriptor, int)> IterateScriptsCore(int depth)
			{
				switch (this)
				{
					case Leaf self:
						yield return (self.Inner, depth);
						break;
					case Tree self:
						foreach (var child1Result in self.Child1.IterateScriptsCore(depth + 1))
							yield return child1Result;
						foreach (var child2Result in self.Child2.IterateScriptsCore(depth + 1))
							yield return child2Result;
						break;
				}
			}

			/// <summary>
			/// Iterates all tap script with its depth.
			/// Depth-first.
			/// </summary>
			/// <returns></returns>
			public IEnumerable<(OutputDescriptor, int)> IterateScripts() => IterateScriptsCore(0);

			public static TapTree FromScriptDepths(IList<OutputDescriptor> scripts, IList<int> depths)
			{
				if (scripts == null) throw new ArgumentNullException(nameof(scripts));
				if (depths == null) throw new ArgumentNullException(nameof(depths));
				if (scripts.Count == 0)
					throw new ArgumentException(nameof(scripts));
				if (scripts.Count != depths.Count)
					throw new ArgumentException($"{nameof(scripts)} and {nameof(depths)} must have same length");

				TapTree? tree = null;
				int prevDepth = -1;
				foreach (var (sc, depth) in
				         scripts
					         .Zip(depths, (sc, dep) => (sc, dep))
					         .OrderByDescending<ValueTuple<OutputDescriptor, int>, int>(i => i.Item2))
				{
					if (prevDepth == depth && tree is not null)
					{
						tree = TapTree.NewTree(tree, NewLeaf(sc));
						prevDepth = depth - 1;
					}
					else
					{
						tree = NewLeaf(sc);
						prevDepth = depth;
					}
				}
				if (prevDepth != 0)
					throw new InvalidDataException($"Malformed script and depths. the top most depth was: {prevDepth}");
				return tree!;
			}
		}

		public class Tr : OutputDescriptor
		{
			public PubKeyProvider InnerPubkey;

			public TapTree? TapLeafs;

			public bool IsKeyPathSpendOnly => TapLeafs is null;

			internal Tr(PubKeyProvider innerPubkey, Network network, TapTree? tapLeafs) : base(network)
			{
				InnerPubkey = innerPubkey ?? throw new ArgumentNullException(nameof(innerPubkey));
				TapLeafs = tapLeafs;
			}

			/// <summary>
			/// Get TaprootSpendInfo in case InnerPubKey is not ranged.
			/// </summary>
			/// <param name="taprootSpendInfo"></param>
			/// <returns></returns>
			internal bool TryGetSpendInfo(ISigningRepository repo, [MaybeNullWhen(false)] out TaprootSpendInfo taprootSpendInfo)
			{
				Debug.Assert(!IsRange());
				taprootSpendInfo = null;
				var internalKey = InnerPubkey.GetPubKey(0, _ => null)!.TaprootInternalKey;
				var builder = new TaprootBuilder();
				if (TapLeafs is not null)
				{
					foreach (var (desc, depth) in this.TapLeafs.IterateScripts())
					{
						if (!desc.TryExpand(0, _ => null, repo, out var scripts, true))
							throw new Exception($"Failed to expand descriptor {desc}. This should never happen");

						foreach (var s in scripts)
						{
							builder.AddLeaf((uint)depth, s.ToTapScript(TapLeafVersion.C0));
						}
					}
				}
				taprootSpendInfo = builder.Finalize(internalKey);
				return true;
			}
		}

		public class RawTr : OutputDescriptor
		{
			public PubKeyProvider OutputPubKeyProvider;
			internal RawTr(PubKeyProvider outputPubKeyProvider, Network network) : base(network)
			{
				OutputPubKeyProvider = outputPubKeyProvider ?? throw new ArgumentNullException(nameof(outputPubKeyProvider));
			}
		}
#endif

		public static OutputDescriptor NewAddr(IDestination dest, Network network) => new Addr(dest, network);
		public static OutputDescriptor NewRaw(Script sc, Network network) => new Raw(sc, network);
		public static OutputDescriptor NewPK(PubKeyProvider pk, Network network) => new PK(pk, network);
		public static OutputDescriptor NewPKH(PubKeyProvider pk, Network network) => new PKH(pk, network);
		public static OutputDescriptor NewWPKH(PubKeyProvider pk, Network network) => new WPKH(pk, network);
		public static OutputDescriptor NewCombo(PubKeyProvider pk, Network network) => new Combo(pk, network);
		public static OutputDescriptor NewMulti(uint m, IEnumerable<PubKeyProvider> pks, bool isSorted, Network network, bool isTapScript = false)
			=> new Multi(m, pks, isSorted, network, isTapScript);
		public static OutputDescriptor NewSH(OutputDescriptor inner, Network network) => new SH(inner, network);
		public static OutputDescriptor NewWSH(OutputDescriptor inner, Network network) => new WSH(inner, network);
#if HAS_SPAN
		public static OutputDescriptor NewTr(PubKeyProvider innerPubKey, Network network, TapTree? tapTree = null) =>
			new Tr(innerPubKey, network, tapTree);
		public static OutputDescriptor NewRawTr(PubKeyProvider outputPubkeyProvider, Network network) =>
			new RawTr(outputPubkeyProvider, network);
#endif

		public bool IsTopLevelOnly() => this switch
		{
			Addr _ => true,
			Raw _ => true,
			Combo _ => true,
			SH _ => true,
#if HAS_SPAN
			Tr _ => true,
			RawTr _ => true,
#endif
			_ => false
		};

		#endregion

		#region Descriptor specific things

		/// <summary>
		/// Expand descriptor into actual scriptPubKeys.
		/// </summary>
		/// <param name="pos">position index to expand</param>
		/// <param name="privateKeyProvider">provider to inject private keys in case of hardened derivation</param>
		/// <param name="repo">repository to which to put resulted information.</param>
		/// <param name="outputScripts">resulted scriptPubKey</param>
		/// <returns></returns>
		public bool TryExpand(
			uint pos,
			ISigningRepository repo,
			[MaybeNullWhen(false)] out List<Script> outputScripts,
			IDictionary<uint, ExtPubKey>? cache = null
			)
		{
			return TryExpand(pos, repo.GetPrivateKey, repo, out outputScripts, false, cache);
		}


		/// <summary>
		/// Expand descriptor into actual scriptPubKeys.
		/// TODO: cache
		/// </summary>
		/// <param name="pos">position index to expand</param>
		/// <param name="privateKeyProvider">provider to inject private keys in case of hardened derivation</param>
		/// <param name="repo">repository to which to put resulted information.</param>
		/// <param name="outputScripts">resulted scriptPubKey</param>
		/// <returns></returns>
		public bool TryExpand(
			uint pos,
			Func<KeyId, Key?> privateKeyProvider,
			ISigningRepository repo,
			[MaybeNullWhen(false)] out List<Script> outputScripts,
			bool isTaproot = false,
			IDictionary<uint, ExtPubKey>? cache = null
			)
		{
			if (privateKeyProvider == null) throw new ArgumentNullException(nameof(privateKeyProvider));
			if (repo == null) throw new ArgumentNullException(nameof(repo));
			outputScripts = new List<Script>();
			return TryExpand(pos, privateKeyProvider, repo, outputScripts, isTaproot, cache);
		}

		private bool ExpandPkHelper(
			PubKeyProvider pkP,
			Func<KeyId, Key?> privateKeyProvider,
			uint pos,
			ISigningRepository repo,
			List<Script> outSc,
			bool isTaproot,
			IDictionary<uint, ExtPubKey>? cache = null)
		{
			if (!pkP.TryGetPubKey(pos, privateKeyProvider, out var keyOrigin1, out var pubkey1))
				return false;
			if (keyOrigin1 != null)
			{
				if (!isTaproot)
					repo.SetKeyOrigin(pubkey1.Hash, keyOrigin1);
#if HAS_SPAN
				repo.SetKeyOrigin(pubkey1.TaprootPubKey, keyOrigin1);
#endif
			}

			repo.SetPubKey(pubkey1.Hash, pubkey1);
			outSc.AddRange(MakeScripts(pubkey1, repo, isTaproot));
			return true;
		}

		private bool TryExpand(
			uint pos,
			Func<KeyId, Key?> privateKeyProvider,
			ISigningRepository repo,
			List<Script> outputScripts,
			bool isTaproot,
			IDictionary<uint, ExtPubKey>? cache = null
			)
		{
			switch (this)
			{
				case Addr _:
					return false;
				case Raw _:
					return false;
				case PK self:
					return ExpandPkHelper(self.PkProvider, privateKeyProvider, pos, repo, outputScripts, isTaproot);
				case PKH self:
					return ExpandPkHelper(self.PkProvider, privateKeyProvider, pos, repo, outputScripts, isTaproot);
				case WPKH self:
					return ExpandPkHelper(self.PkProvider, privateKeyProvider, pos, repo, outputScripts, isTaproot);
				case Combo self:
					return ExpandPkHelper(self.PkProvider, privateKeyProvider, pos, repo, outputScripts, isTaproot);
				case Multi self:
					// prepare temporary objects so that it won't affect the result in case
					// it fails in the middle.
					var tmpRepo = new FlatSigningRepository();
					var keys = new PubKey[self.PkProviders.Count];
					for (int i = 0; i < self.PkProviders.Count; ++i)
					{
						var pkP = self.PkProviders[i];
						if (!pkP.TryGetPubKey(pos, privateKeyProvider, out var keyOrigin1, out var pubkey1))
							return false;
						if (keyOrigin1 != null)
							tmpRepo.SetKeyOrigin(pubkey1.Hash, keyOrigin1);
						tmpRepo.SetPubKey(pubkey1.Hash, pubkey1);
						keys[i] = pubkey1;
					}

					if (self.IsSorted)
					{
						keys = keys.OrderBy(x => x).ToArray();
					}
					repo.Merge(tmpRepo);
					outputScripts.Add(PayToMultiSigTemplate.Instance.GenerateScriptPubKey((int)self.Threshold, sort: false, forceSmallSigCount: false, keys));
					return true;
				case SH self:
					var subRepo1 = new FlatSigningRepository();
					if (!self.Inner.TryExpand(pos, privateKeyProvider, subRepo1, out var shInnerResult, false))
						return false;
					repo.Merge(subRepo1);
					foreach (var inner in shInnerResult)
					{
						repo.SetScript(inner.Hash, inner);
						outputScripts.Add(inner.Hash.ScriptPubKey);
					}
					return true;
				case WSH self:
					var subRepo2 = new FlatSigningRepository();
					if (!self.Inner.TryExpand(pos, privateKeyProvider, subRepo2, out var wshInnerResult, false))
						return false;
					repo.Merge(subRepo2);
					foreach (var inner in wshInnerResult)
					{
						repo.SetScript(inner.Hash, inner);
						repo.SetScript(inner.WitHash.HashForLookUp, inner);
						outputScripts.Add(inner.WitHash.ScriptPubKey);
					}
					return true;
#if HAS_SPAN
				case Tr self:
					if (!self.InnerPubkey.TryGetPubKey(pos, privateKeyProvider, out var keyOrigin2, out var pubkey2))
						return false;
					if (keyOrigin2 != null)
						repo.SetKeyOrigin(pubkey2.TaprootPubKey, keyOrigin2);
					var builder = new TaprootBuilder();
					if (self.TapLeafs is not null)
					{
						foreach (var (od, depth) in self.TapLeafs.IterateScripts())
						{
							if (!od.TryExpand(pos, privateKeyProvider, repo, out var subScripts , true))
								return false;
							foreach (var s in subScripts)
							{
								builder.AddLeaf((uint)depth, s.ToTapScript(TapLeafVersion.C0));
							}
						}
					}
					var spendInfo = builder.Finalize(pubkey2.TaprootInternalKey);
					repo.SetTaprootSpendInfo(spendInfo.OutputPubKey, spendInfo);
					outputScripts.Add(spendInfo.OutputPubKey.OutputKey.ScriptPubKey);
					return true;
				case RawTr self:
					return ExpandPkHelper(self.OutputPubKeyProvider, privateKeyProvider, pos, repo, outputScripts, true);
#endif
			}
			throw new Exception("Unreachable");
		}

		/// <summary>
		///  Make output scirptpubkey from expanded pubkey.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="repo"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private List<Script> MakeScripts(PubKey key, ISigningRepository repo, bool isTaproot)
		{
			switch (this)
			{
				case Addr self:
					return new List<Script> { self.Address.ScriptPubKey };
				case Raw self:
					return new List<Script> { self.Script };
				case PK _:
					return
#if HAS_SPAN
						isTaproot ?
						new List<Script>
						{
							new Script(
								Op.GetPushOp(key.TaprootPubKey.ToBytes()),
								OpcodeType.OP_CHECKSIG
							)
						} :
#endif
						new List<Script> { key.ScriptPubKey };
				case PKH _:
					return new List<Script> { key.Hash.ScriptPubKey };
				case WPKH _:
					return new List<Script> { key.WitHash.ScriptPubKey };
				case Combo _:
					var res = new List<Script>
					{
						key.ScriptPubKey,
						key.Hash.ScriptPubKey,
					};
					if (key.IsCompressed)
					{
						res.Add(key.WitHash.ScriptPubKey);
						res.Add(key.WitHash.ScriptPubKey.Hash.ScriptPubKey);
						repo.SetScript(key.WitHash.ScriptPubKey.Hash, key.WitHash.ScriptPubKey);
					}
					return res;
#if HAS_SPAN
				case RawTr _:
					return new List<Script> { key.TaprootPubKey.ScriptPubKey };
#endif
				// Other cases never calls this function. Because this method is just a helper for expanding above cases
			}

			throw new Exception("Unreachable");
		}

		/// <summary>
		/// Output descriptor has `solvability` property.
		/// Which means whether we are able to know how to create ScirptSig (or witness)
		/// for the descriptor.
		/// It is always false for `addr()` and `raw()`, and otherwise true.
		/// But this may change in the future, see: https://github.com/bitcoin/bitcoin/issues/24114
		/// for the discussion.
		/// </summary>
		/// <returns></returns>
		public bool IsSolvable() => this switch
		{
			Addr _ => false,
			Raw _ => false,
			SH self =>
				self.Inner.IsSolvable(),
			WSH self =>
				self.Inner.IsSolvable(),
			_ =>
				true,
		};

		public bool IsRange() => this switch
		{
			Addr _ =>
				false,
			Raw _ =>
				false,
			PK self =>
				self.PkProvider.IsRange(),
			PKH self =>
				self.PkProvider.IsRange(),
			WPKH self =>
				self.PkProvider.IsRange(),
			Combo self =>
				self.PkProvider.IsRange(),
			Multi self =>
				self.PkProviders.Any(pk => pk.IsRange()),
			SH self =>
				self.Inner.IsRange(),
			WSH self =>
				self.Inner.IsRange(),
#if HAS_SPAN
			Tr self =>
				self.InnerPubkey.IsRange() ||
				(self.TapLeafs is not null && self.TapLeafs.IterateScripts().Any(leaf => leaf.Item1.IsRange())),
			RawTr self =>
				self.OutputPubKeyProvider.IsRange(),
#endif
			_ =>
				throw new Exception("Unreachable"),
		};

		public enum ScriptContext
		{
			TOP,
			P2SH,
			P2WSH,
#if HAS_SPAN
			P2TR,
#endif
		}

		private static PubKeyProvider InferPubKey(PubKey pk, ISigningRepository repo)
		{
			var keyProvider = PubKeyProvider.NewConst(pk);
			return
				repo.TryGetKeyOrigin(pk.Hash, out var keyOrigin)
				? PubKeyProvider.NewOrigin(keyOrigin, keyProvider)
				: keyProvider;
		}

		private ScriptPubKeyType? InferTemplate(ScriptTemplate? template) => template switch
		{
			PayToPubkeyHashTemplate _ => ScriptPubKeyType.Legacy,
			PayToPubkeyTemplate _ => ScriptPubKeyType.Legacy,
			PayToWitTemplate _ => ScriptPubKeyType.Segwit,
			// in the case of p2sh, we don't know if it is p2sh or p2sh-p2[wsh|wpkh], so just return null
			_ => null
		};

		/// <summary>
		/// Infer the address type for that descriptor.
		/// When it is impossible, just return null.
		/// e.g. In case of descriptors those are agnostic to the actual scriptpubkey format (e.g. "multi"),
		/// it just returns null.
		/// </summary>
		/// <returns></returns>
		public ScriptPubKeyType? GetScriptPubKeyType() => this switch
		{
			Addr self =>
				InferTemplate(self.Address.ScriptPubKey.FindTemplate()),
			Raw self =>
				InferTemplate(self.Script.FindTemplate()),
			PK _ => null,
			PKH _ => ScriptPubKeyType.Legacy,
			WPKH _ => ScriptPubKeyType.Segwit,
			SH self =>
				self.Inner.GetScriptPubKeyType() switch
				{
					ScriptPubKeyType.Segwit => ScriptPubKeyType.SegwitP2SH,
					_ => ScriptPubKeyType.Legacy,
				},
			WSH _ => ScriptPubKeyType.Segwit,
#if HAS_SPAN
			Tr self =>
				self.TapLeafs is null ?
				ScriptPubKeyType.TaprootBIP86 :
				null,
#endif
			_ => null
		};

#if HAS_SPAN
		private static PubKeyProvider InferXOnlyPubkey(TaprootPubKey xkey, ISigningRepository repo)
		{
			var keyProvider = PubKeyProvider.NewConst(xkey);
			return
				repo.TryGetKeyOrigin(xkey, out var origin) ?
				PubKeyProvider.NewOrigin(origin, keyProvider) :
				keyProvider;
		}

		private static PubKeyProvider InferXOnlyPubkey(TaprootInternalPubKey xkey, ISigningRepository repo)
			=> InferXOnlyPubkey(new TaprootPubKey(xkey.pubkey), repo);


		private static OutputDescriptor? InferMultiA(Script sc, ISigningRepository repo, Network network)
		{

			var match = sc.FindMultiATemplate();
			if (match is null)
				return null;

			(var m, var pks) = match.Value;
			var keys = new List<PubKeyProvider>(pks.Count);

			foreach (var key in pks)
			{
				keys.Add(InferXOnlyPubkey(key, repo));
			}

			return NewMulti((uint)m, keys, isSorted: false, network, isTapScript: true);

		}
#endif

		/// <summary>
		/// Check scriptpubkey and return (estimated) OutputDescriptor for it.
		/// This may (wrongly) return `raw()` or `addr()` if it fails to infer it.
		/// In case of Taproot, all we can do is to return `rawtr()` instead of `tr()`
		/// </summary>
		/// <param name="sc"></param>
		/// <param name="repo"></param>
		/// <param name="network"></param>
		/// <param name="ctx"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public static OutputDescriptor InferFromScript(Script sc, ISigningRepository repo, Network network)
			=> InferFromScript(sc, repo, network, ScriptContext.TOP);

		private static OutputDescriptor InferFromScript(Script sc, ISigningRepository repo, Network network, ScriptContext ctx)
		{
			if (network is null)
				throw new ArgumentNullException(nameof(network));
			if (sc == null) throw new ArgumentNullException(nameof(sc));
			if (repo == null) throw new ArgumentNullException(nameof(repo));


			var template = sc.FindTemplate();
#if HAS_SPAN
			// We need special treatment for tapscript, since it uses xonly pubkeys.
			// It can not be checked by `FindTemplate`
			if (ctx == ScriptContext.P2TR)
			{
				var b = sc.ToBytes();
				if (b.Length == 34 && b[0] == 32 && b[33] == (byte)OpcodeType.OP_CHECKSIG)
				{
					var xonlyKey = new TaprootPubKey(b.AsSpan(1, 32));
					return NewPK(InferXOnlyPubkey(xonlyKey, repo), network);
				}

				var ret = InferMultiA(sc, repo, network);
				if (ret is not null)
					return ret;
			}

			if (template is PayToTaprootTemplate p2trTemplate)
			{
				if (p2trTemplate.ExtractScriptPubKeyParameters(sc) is { } pk)
				{
					if (repo.TryGetTaprootSpendInfo(pk, out var taprootSpendInfo))
					{
						var keyProvider =
							InferXOnlyPubkey(taprootSpendInfo.InternalPubKey, repo);

						if (taprootSpendInfo.InferTaprootTree(pk, out var tree))
						{
							bool ok = true;
							var subScripts = new List<OutputDescriptor>();
							var depths = new List<int>();
							foreach (var (depth, script) in tree)
							{
								OutputDescriptor? subdesc =
									(script.Version == TapLeafVersion.C0)
										? InferFromScript(script, repo, network, ScriptContext.P2TR)
										: null;
								if (subdesc is null)
								{
									ok = false;
									break;
								}
								subScripts.Add(subdesc);
								depths.Add(depth);
							}
							if (ok)
							{
								if (tree.Count != 0)
								{
									var tapTree = TapTree.FromScriptDepths(subScripts, depths);
									return NewTr(keyProvider, network, tapTree);
								}
								return NewTr(keyProvider, network);
							}
						}
					}
					else
					{
						var keyProvider = PubKeyProvider.NewConst(pk);
						keyProvider =
							repo.TryGetKeyOrigin(pk, out var keyOrigin)
								? PubKeyProvider.NewOrigin(keyOrigin, keyProvider)
								: keyProvider;
						return NewRawTr(keyProvider, network);
					}
				}
			}
#endif

			if (template is PayToPubkeyTemplate p2pkTemplate)
			{
				var pk = p2pkTemplate.ExtractScriptPubKeyParameters(sc)!;
				return OutputDescriptor.NewPK(InferPubKey(pk, repo), network);
			}
			if (template is PayToPubkeyHashTemplate p2pkhTemplate)
			{
				var pkHash = p2pkhTemplate.ExtractScriptPubKeyParameters(sc)!;
				if (repo.TryGetPubKey(pkHash, out var pk))
					return OutputDescriptor.NewPKH(InferPubKey(pk, repo), network);
			}
			if (template is PayToMultiSigTemplate p2MultiSigTemplate)
			{
				var data = p2MultiSigTemplate.ExtractScriptPubKeyParameters(sc)!;
				var pks = data.PubKeys;
				var orderedPks = pks.OrderBy(pk => pk);
				var isOrdered = orderedPks.SequenceEqual(pks);
				var providers = pks.Select(pk => InferPubKey(pk, repo));
				return OutputDescriptor.NewMulti((uint)data.SignatureCount, providers, isOrdered, network);
			}
			if (template is PayToScriptHashTemplate p2shTemplate && ctx == ScriptContext.TOP)
			{
				var scriptId = p2shTemplate.ExtractScriptPubKeyParameters(sc)!;
				if (repo.TryGetScript(scriptId, out var nextScript))
				{
					var sub = InferFromScript(nextScript, repo, network, ScriptContext.P2SH);
					return OutputDescriptor.NewSH(sub, network);
				}
			}
			if (template is PayToWitTemplate)
			{
				var witScriptId = PayToWitScriptHashTemplate.Instance.ExtractScriptPubKeyParameters(sc);
				if (witScriptId != null && ctx != ScriptContext.P2WSH)
				{
					if (repo.TryGetScript(witScriptId.HashForLookUp, out var nextScript))
					{
						var sub = InferFromScript(nextScript, repo, network, ScriptContext.P2WSH);
						return OutputDescriptor.NewWSH(sub, network);
					}
				}
				var witKeyId = PayToWitPubKeyHashTemplate.Instance.ExtractScriptPubKeyParameters(sc);
				if (witKeyId != null && ctx != ScriptContext.P2WSH)
				{
					if (repo.TryGetPubKey(witKeyId.AsKeyId(), out var pk))
						return OutputDescriptor.NewWPKH(InferPubKey(pk, repo), network);
				}
			}

			// Incase of unknown witness Output, we recover it to Addr,
			// Otherwise, Raw.
			if (template is PayToWitTemplate unknownWitnessTemplate)
			{
				var dest = unknownWitnessTemplate.ExtractScriptPubKeyParameters(sc)!;
				return OutputDescriptor.NewAddr(dest, network);
			}

			return OutputDescriptor.NewRaw(sc, network);
		}

		#endregion

		# region string (De)serializer

		public override string ToString()
		{
			var inner = ToStringHelper();
			return $"{inner}#{GetCheckSum(inner)}";
		}

		/// <summary>
		/// OutputDescriptor class itself never contains a private key information.
		/// To get an string representation with private key, use this method with a private data DB you have injected
		/// when you were parsing.
		/// </summary>
		/// <param name="secretProvider"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException"></exception>
		public bool TryGetPrivateString(ISigningRepository secretProvider, out string? result)
		{
			if (secretProvider == null) throw new ArgumentNullException(nameof(secretProvider));
			result = null;
			if (!TryGetPrivateStringHelper(secretProvider, out var inner) || inner is null)
				return false;
			result = $"{inner}#{GetCheckSum(inner)}";
			return true;
		}

		private bool TryGetPrivateStringHelper(ISigningRepository secretProvider, out string? result)
		{
			result = null;
			switch (this)
			{
				case Addr _:
				case Raw _:
					result = this.ToStringHelper();
					return true;
				case PK self:
					if (!self.PkProvider.TryGetPrivateString(secretProvider, out var privStr1))
						return false;
					result = $"pk({privStr1})";
					return true;
				case PKH self:
					if (!self.PkProvider.TryGetPrivateString(secretProvider, out var privStr2))
						return false;
					result = $"pkh({privStr2})";
					return true;
				case WPKH self:
					if (!self.PkProvider.TryGetPrivateString(secretProvider, out var privStr3))
						return false;
					result = $"wpkh({privStr3})";
					return true;
				case Combo self:
					if (!self.PkProvider.TryGetPrivateString(secretProvider, out var privStr4))
						return false;
					result = $"combo({privStr4})";
					return true;
				case Multi self:
					var subKeyList = new List<string>();
					foreach (var prov in (self.PkProviders))
					{
						if (!prov.TryGetPrivateString(secretProvider, out var tmp))
							return false;
						subKeyList.Add(tmp);
					}
					result = $"{(self.IsSorted ? "sortedmulti" : "multi")}({self.Threshold},{String.Join(",", subKeyList)})";
					return true;
				case SH self:
					if (!self.Inner.TryGetPrivateStringHelper(secretProvider, out var shInner))
						return false;
					result = $"sh({shInner})";
					return true;
				case WSH self:
					if (!self.Inner.TryGetPrivateStringHelper(secretProvider, out var wshInner))
						return false;
					result = $"wsh({wshInner})";
					return true;
#if HAS_SPAN
				case Tr self:
					var sb = new StringBuilder();
					if (!self.InnerPubkey.TryGetPrivateString(secretProvider, out var internalPubKeyString))
						return false;
					sb.Append($"tr({internalPubKeyString}");
					if (self.TapLeafs is { } tapTree)
					{
						sb.Append(",");
						if (!tapTree.TryGetPrivateString(secretProvider, out var tapTreeString))
							return false;
						sb.Append(tapTreeString);
					}
					sb.Append(")");
					result = sb.ToString();
					return true;
				case RawTr self:
					if (!self.OutputPubKeyProvider.TryGetPrivateString(secretProvider, out var rawTrPrivateString))
						return false;
					result = $"rawtr({rawTrPrivateString})";
					return true;
#endif

			}
			throw new Exception("Unreachable");
		}

		private string ToStringHelper() => this switch
		{
			Addr self =>
				$"addr({self.Address})",
			Raw self =>
				$"raw({self.Script.ToHex()})",
			PK self =>
				$"pk({self.PkProvider})",
			PKH self =>
				$"pkh({self.PkProvider})",
			WPKH self =>
				$"wpkh({self.PkProvider})",
			Combo self =>
				$"combo({self.PkProvider})",
			Multi self =>
				$"{(self.IsSorted ? "sortedmulti" : "multi")}{(self.IsTapScript ? "_a" : "")}({self.Threshold},{String.Join(",", self.PkProviders)})",
			SH self =>
				$"sh({self.Inner.ToStringHelper()})",
			WSH self =>
				$"wsh({self.Inner.ToStringHelper()})",
#if HAS_SPAN
			Tr self =>
				self.IsKeyPathSpendOnly ?
				$"tr({self.InnerPubkey})" :
				$"tr({self.InnerPubkey},{self.TapLeafs})",
			RawTr self =>
				$"rawtr({self.OutputPubKeyProvider})",
#endif
			_ =>
				throw new Exception("unreachable")
		};

		/// <summary>
		/// Parse descriptor from string representation.
		/// OutputDescriptor class does not hold private key data in memory, so if you want to parse
		/// private key, you must pass the reference to the DB with `repo` argument.
		/// Parser will inject private keys they've found into the DB. this can later be used with other methods
		/// such as `TryGetPrivateString`
		/// </summary>
		/// <param name="desc">descriptor to parse</param>
		/// <param name="network">Network for the descriptor.</param>
		/// <param name="requireCheckSum">if true, Do not parse descriptors without checksum. default: false</param>
		/// <param name="repo">repository to inject private key information.</param>
		/// <returns></returns>
		public static OutputDescriptor Parse(string desc, Network network, bool requireCheckSum = false, ISigningRepository? repo = null)
			=> OutputDescriptorParser.ParseOD(desc, network, requireCheckSum, repo);

		/// <summary>
		/// Parse descriptor from string representation.
		/// OutputDescriptor class does not hold private key data in memory, so if you want to parse
		/// private key, you must pass the reference to the DB with `repo` argument.
		/// Parser will inject private keys they've found into the DB. this can later be used with other methods
		/// such as `TryGetPrivateString`
		/// </summary>
		/// <param name="desc">descriptor to parse</param>
		/// <param name="network">Network for the descriptor.</param>
		/// <param name="requireCheckSum">If true, Do not parse descriptors without checksum. Default: false</param>
		/// <param name="repo">repository to inject private key information.</param>
		/// <returns></returns>
		public static bool TryParse(string desc, Network network, out OutputDescriptor? result, bool requireCheckSum = false, ISigningRepository? repo = null)
			=> OutputDescriptorParser.TryParseOD(desc, network, out result, requireCheckSum, repo);

		#endregion

		#region Equatable

		public sealed override bool Equals(object? obj)
			=> Equals(obj as OutputDescriptor);

		public bool Equals(OutputDescriptor? other) => (other != null) && (this) switch
		{
			Addr self =>
				other is Addr o && self.Address.Equals(o.Address),
			Raw self =>
				other is Raw o && self.Script.Equals(o.Script),
			PK self =>
				other is PK o && self.PkProvider.Equals(o.PkProvider),
			PKH self =>
				other is PKH o && self.PkProvider.Equals(o.PkProvider),
			WPKH self =>
				other is WPKH o && self.PkProvider.Equals(o.PkProvider),
			Combo self =>
				other is Combo o && self.PkProvider.Equals(o.PkProvider),
			Multi self =>
				other is Multi o &&
				self.Threshold == o.Threshold &&
				self.PkProviders.SequenceEqual(o.PkProviders) &&
				self.IsSorted == o.IsSorted,
			SH self =>
				other is SH o && self.Inner.Equals(o.Inner),
			WSH self =>
				other is WSH o && self.Inner.Equals(o.Inner),
#if HAS_SPAN
			Tr self =>
				other is Tr o && self.InnerPubkey.Equals(o.InnerPubkey) &&
					((self.TapLeafs is null && o.TapLeafs is null) || self.TapLeafs?.ToString().Equals(o.TapLeafs?.ToString()) == true),
			RawTr self =>
				other is RawTr o && self.OutputPubKeyProvider.Equals(o.OutputPubKeyProvider),
#endif
			_ =>
				throw new Exception("Unreachable!"),
		};

		public override int GetHashCode()
		{
			int num;
			switch (this)
			{
				case Addr self:
					{
						num = 0;
						return -1640531527 + self.Address.GetHashCode() + ((num << 6) + (num >> 2));
					}
				case Raw self:
					{
						num = 1;
						return -1640531527 + self.Script.GetHashCode() + ((num << 6) + (num >> 2));
					}
				case PK self:
					{
						num = 2;
						return -1640531527 + self.PkProvider.GetHashCode() + ((num << 6) + (num >> 2));
					}
				case PKH self:
					{
						num = 3;
						return -1640531527 + self.PkProvider.GetHashCode() + ((num << 6) + (num >> 2));
					}
				case WPKH self:
					{
						num = 4;
						return -1640531527 + self.PkProvider.GetHashCode() + ((num << 6) + (num >> 2));
					}
				case Combo self:
					{
						num = 5;
						return -1640531527 + self.PkProvider.GetHashCode() + ((num << 6) + (num >> 2));
					}
				case Multi self:
					{
						num = 6;
						num = self.Threshold.GetHashCode() + ((num << 6) + (num >> 2));
						num = self.IsSorted.GetHashCode() + ((num << 6) + (num >> 2));
						foreach (var pk in self.PkProviders)
						{
							num = -1640531527 + pk.GetHashCode() + ((num << 6) + (num >> 2));
						}
						return num;
					}
				case SH self:
					{
						num = 7;
						return -1640531527 + self.Inner.GetHashCode() + ((num << 6) + (num >> 2));
					}
				case WSH self:
					{
						num = 8;
						return -1640531527 + self.Inner.GetHashCode() + ((num << 6) + (num >> 2));
					}
#if HAS_SPAN
				case Tr self:
					{
						num = 9;
						num = -1640531527 + self.InnerPubkey.GetHashCode() + ((num << 6) + (num >> 2));
						var iter = self.TapLeafs?.IterateScripts();
						if (iter is null)
							return num;
						foreach (var i in iter)
							num = -1640531527 + i.GetHashCode() + ((num << 6) + (num >> 2));
						return num;
					}
				case RawTr self:
					{
						num = 10;
						return -1640531527 + self.OutputPubKeyProvider.GetHashCode() + ((num << 6) + (num >> 2));
					}
#endif
			default:
					throw new Exception("Unreachable!");
			}
		}

		#endregion

		#region checksum
		/** The character set for the checksum itself (same as bech32). */
		static readonly char[] CHECKSUM_CHARSET = "qpzry9x8gf2tvdw0s3jn54khce6mua7l".ToCharArray();
		static readonly string INPUT_CHARSET_STRING =
		"0123456789()[],'/*abcdefgh@:$%{}" +
        "IJKLMNOPQRSTUVWXYZ&+-.;<=>?!^_|~" +
        "ijklmnopqrstuvwxyzABCDEFGH`#\"\\ ";

		static readonly char[] INPUT_CHARSET = INPUT_CHARSET_STRING.ToCharArray();

		public static string AddChecksum(string desc) => $"{desc}#{GetCheckSum(desc)}";
		public static string GetCheckSum(string desc)
		{
			if (desc is null)
				throw new ArgumentNullException(nameof(desc));
			ulong c = 1;
			int cls = 0;
			int clscount = 0;
			foreach(var ch in desc.ToCharArray())
			{
				var pos = INPUT_CHARSET_STRING.IndexOf(ch);
				if (pos == -1)
					return "";
				c = PolyMod(c, pos & 31);
				cls = cls * 3 + (pos >> 5);
				if (++clscount == 3)
				{
					c = PolyMod(c, cls);
					cls = 0;
					clscount = 0;
				}
			}
			if (clscount > 0) c = PolyMod(c, cls);
			for (int j = 0; j < 8; ++j) c = PolyMod(c, 0);
			c ^= 1;
			var result = new char[8];
			for (int j = 0; j < 8; ++j)
			{
				result[j] = CHECKSUM_CHARSET[(c >> (5 * (7 - j))) & 31];
			}
			return new String(result);
		}
		static ulong PolyMod(ulong c, int val)
		{
			ulong c0 = c >> 35;
			c = ((c & 0x7ffffffffUL) << 5) ^ (ulong)val;
			if ((c0 & 1UL) != 0) c ^= 0xf5dee51989;
			if ((c0 & 2UL) != 0) c ^= 0xa9fdca3312;
			if ((c0 & 4UL) != 0) c ^= 0x1bab10e32d;
			if ((c0 & 8) != 0) c ^= 0x3706b1677a;
			if ((c0 & 16) != 0) c ^= 0x644d626ffd;
			return c;
		}

		#endregion
	}
}
#nullable disable
