#if !NO_RECORDS
#nullable enable
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static NBitcoin.WalletPolicies.MiniscriptNode.ParameterRequirement;
using static NBitcoin.WalletPolicies.MiniscriptNode.Value;

namespace NBitcoin.WalletPolicies
{
	public class FragmentDescriptor
	{
		public bool IsOr() =>
			this == or_b ||
			this == or_c ||
			this == or_d ||
			this == or_i;
		public bool IsAnd() =>
			this == and_v ||
			this == and_b ||
			this == and_n;
		public bool IsHash() =>
			this == sha256 ||
			this == ripemd160 ||
			this == hash256 ||
			this == hash160;
		private static Op HASH160(List<Op> node) => Op.GetPushOp(Hashes.Hash160(node[0].PushData).ToBytes());
		FragmentDescriptor(string name,
			Action<List<Op>[], List<Op>> addOps)
		{
			Name = name;
			AddOps = addOps;
		}
		public readonly static FragmentDescriptor _0 = new(
			"0",
			(v, ops) => ops.Add(OpcodeType.OP_0));
		public readonly static FragmentDescriptor _1 = new(
			"1",
			(v, ops) => ops.Add(OpcodeType.OP_1));


		public readonly static FragmentDescriptor pk_k = new(
			"pk_k",
			(v, ops) => ops.AddRange(v[0]));
		public readonly static FragmentDescriptor pk_h = new(
			"pk_h",
			(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_DUP, OpcodeType.OP_HASH160, HASH160(v[0]), OpcodeType.OP_EQUALVERIFY }));
		public readonly static FragmentDescriptor older = new(
			"older",
			(v, ops) => ops.AddRange(new Op[] { v[0][0], OpcodeType.OP_CHECKSEQUENCEVERIFY }));
		public readonly static FragmentDescriptor after = new(
			"after",
			(v, ops) => ops.AddRange(new Op[] { v[0][0], OpcodeType.OP_CHECKLOCKTIMEVERIFY }));
		public readonly static FragmentDescriptor sha256 = new(
			"sha256",
			(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_SIZE, Op.GetPushOp(0x20), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_SHA256, v[0][0], OpcodeType.OP_EQUAL }));
		public readonly static FragmentDescriptor ripemd160 = new(
			"ripemd160",
			(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_SIZE, Op.GetPushOp(0x20), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_RIPEMD160, v[0][0], OpcodeType.OP_EQUAL }));
		public readonly static FragmentDescriptor hash256 = new(
			"hash256",
			(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_SIZE, Op.GetPushOp(0x20), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_HASH256, v[0][0], OpcodeType.OP_EQUAL }));
		public readonly static FragmentDescriptor hash160 = new(
			"hash160",
			(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_SIZE, Op.GetPushOp(0x20), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_HASH160, v[0][0], OpcodeType.OP_EQUAL }));
		public readonly static FragmentDescriptor and_v = new(
			"and_v",
			(v, ops) =>
			{
				ops.AddRange(v[0]);
				ops.AddRange(v[1]);
			});

		public readonly static FragmentDescriptor and_b = new(
			"and_b",
			(v, ops) =>
			{
				ops.AddRange(v[0]);
				ops.AddRange(v[1]);
				ops.Add(OpcodeType.OP_BOOLAND);
			});
		public readonly static FragmentDescriptor and_n = new(
			"and_n",
			(v, ops) =>
			{
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_NOTIF);
				ops.Add(OpcodeType.OP_0);
				ops.Add(OpcodeType.OP_ELSE);
				ops.AddRange(v[1]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor or_b = new(
			"or_b",
			(v, ops) =>
			{
				ops.AddRange(v[0]);
				ops.AddRange(v[1]);
				ops.Add(OpcodeType.OP_BOOLOR);
			});
		public readonly static FragmentDescriptor or_d = new(
			"or_d",
			(v, ops) =>
			{
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_IFDUP);
				ops.Add(OpcodeType.OP_NOTIF);
				ops.AddRange(v[1]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor or_c = new(
			"or_c",
			(v, ops) =>
			{
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_NOTIF);
				ops.AddRange(v[1]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor or_i = new(
			"or_i",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_IF);
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_ELSE);
				ops.AddRange(v[1]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor andor = new(
			"andor",
			(v, ops) =>
			{
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_NOTIF);
				ops.AddRange(v[2]);
				ops.Add(OpcodeType.OP_ELSE);
				ops.AddRange(v[1]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor thresh = new(
			"thresh",
			(v, ops) =>
			{
				int i = 0;
				i++; // Skip count
				ops.AddRange(v[i++]);
				while (i < v.Length)
				{
					ops.AddRange(v[i++]);
					ops.Add(OpcodeType.OP_ADD);
				}
				ops.Add(v[0][0]);
				ops.Add(OpcodeType.OP_EQUAL);
			});
		public readonly static FragmentDescriptor multi = new(
			"multi",
			(v, ops) =>
			{
				int i = 0;
				while (i < v.Length)
				{
					ops.Add(v[i++][0]);
				}
				ops.Add(Op.GetPushOp(v.Length - 1));
				ops.Add(OpcodeType.OP_CHECKMULTISIG);
			});
		public readonly static FragmentDescriptor multi_a = new(
			"multi_a",
			(v, ops) =>
			{
				int i = 0;
				i++; // Skip count
				ops.Add(v[i++][0]);
				ops.Add(OpcodeType.OP_CHECKSIG);
				while (i < v.Length)
				{
					ops.Add(v[i++][0]);
					ops.Add(OpcodeType.OP_CHECKSIGADD);
				}
				ops.Add(v[0][0]);
				ops.Add(OpcodeType.OP_NUMEQUAL);
			});

		public readonly static FragmentDescriptor a = new(
			"a",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_TOALTSTACK);
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_FROMALTSTACK);
			});
		public readonly static FragmentDescriptor s = new(
			"s",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_SWAP);
				ops.AddRange(v[0]);
			});
		public readonly static FragmentDescriptor c = new(
			"c",
			(v, ops) =>
			{
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_CHECKSIG);
			});
		public readonly static FragmentDescriptor t = new(
			"t",
			(v, ops) =>
			{
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_1);
			});
		public readonly static FragmentDescriptor d = new(
			"d",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_DUP);
				ops.Add(OpcodeType.OP_IF);
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor v = new(
			"v",
			(v, ops) =>
			{
				ops.AddRange(v[0]);
				var lastOp = ops[^1];
				var verify = lastOp.Code switch
				{
					OpcodeType.OP_NUMEQUAL => OpcodeType.OP_NUMEQUALVERIFY,
					OpcodeType.OP_EQUAL => OpcodeType.OP_EQUALVERIFY,
					OpcodeType.OP_CHECKSIG => OpcodeType.OP_CHECKSIGVERIFY,
					OpcodeType.OP_CHECKMULTISIG => OpcodeType.OP_CHECKMULTISIGVERIFY,
					_ => OpcodeType.OP_VERIFY
				};
				if (verify == OpcodeType.OP_VERIFY)
					ops.Add(OpcodeType.OP_VERIFY);
				else
					ops[^1] = verify;
			});
		public readonly static FragmentDescriptor j = new(
			"j",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_SIZE);
				ops.Add(OpcodeType.OP_0NOTEQUAL);
				ops.Add(OpcodeType.OP_IF);
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor n = new(
			"n",
			(v, ops) =>
			{
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_0NOTEQUAL);
			});
		public readonly static FragmentDescriptor l = new(
			"l",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_IF);
				ops.Add(OpcodeType.OP_0);
				ops.Add(OpcodeType.OP_ELSE);
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor u = new(
			"u",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_IF);
				ops.AddRange(v[0]);
				ops.Add(OpcodeType.OP_ELSE);
				ops.Add(OpcodeType.OP_0);
				ops.Add(OpcodeType.OP_ENDIF);
			});

		public readonly static FragmentDescriptor pkh = new(
		"pkh",
		(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_DUP, OpcodeType.OP_HASH160, HASH160(v[0]), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_CHECKSIG }));
		public readonly static FragmentDescriptor wpkh = new(
		"wpkh",
		(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_0, HASH160(v[0]) }));
		public readonly static FragmentDescriptor sh = new(
		"sh",
		(v, ops) =>
		{
			var hash = Hashes.Hash160RawBytes(new Script(v[0]).ToBytes());
			ops.Add(OpcodeType.OP_HASH160);
			ops.Add(Op.GetPushOp(hash));
			ops.Add(OpcodeType.OP_EQUAL);
		});
		public readonly static FragmentDescriptor wsh = new(
		"wsh",
		(v, ops) =>
		{
			var hash = Hashes.SHA256(new Script(v[0]).ToBytes());
			ops.Add(OpcodeType.OP_0);
			ops.Add(Op.GetPushOp(hash));
		});

		public readonly static FragmentDescriptor tr = new(
		"tr",
		(v, ops) =>
		{
			if (v.Length == 1)
			{
				var internalKey = new TaprootInternalPubKey(v[0][0].PushData);
				ops.AddRange(new Op[] { OpcodeType.OP_1, Op.GetPushOp(TaprootFullPubKey.Create(internalKey, null).OutputKey.ToBytes()) });
				return;
			}
			throw new NotSupportedException("tr with more than one parameter isn't supported yet");
		});

		public readonly static FragmentDescriptor pk = new(
		"pk",
		(v, ops) => ops.AddRange(new[] { v[0][0], (Op)OpcodeType.OP_CHECKSIG }));

		public string Name { get; }
		public Action<List<Op>[], List<Op>> AddOps { get; }
		public override string ToString() => Name;
	}
	public record MiniscriptNode
	{
		public record TaprootBranchNode(MiniscriptNode Left, MiniscriptNode Right) : MiniscriptNode
		{
			protected override void ToString(StringBuilder builder)
			{
				builder.Append('{');
				builder.Append(Left.ToString());
				builder.Append(',');
				builder.Append(Right.ToString());
				builder.Append('}');
			}
		}
		/// <summary>
		/// A BIP388 top level tr
		/// </summary>
		/// <param name="InternalKeyNode"></param>
		/// <param name="ScriptTreeRootNode">Can either be a <see cref="Fragment"/> or a <see cref="TaprootBranchNode"/></param>
		public record TaprootNode(MiniscriptNode InternalKeyNode, MiniscriptNode? ScriptTreeRootNode) : Fragment(FragmentDescriptor.tr)
		{
			public override IEnumerable<MiniscriptNode> Parameters
			{
				get
				{
					yield return InternalKeyNode;
					if (ScriptTreeRootNode is { })
						yield return ScriptTreeRootNode;
				}
			}
		}
		public static Value.PubKeyValue Create(PubKey pubkey) => new Value.PubKeyValue(pubkey);
		public static Value.TaprootPubKeyValue Create(TaprootPubKey pubkey) => new Value.TaprootPubKeyValue(pubkey);


		/// <summary>
		/// <para>A multipath expression (<a href="https://github.com/bitcoin/bips/blob/master/bip-0388.mediawiki">BIP0388</a>). Target can either be:</para>
		/// <list type="bullet">
		/// <item><term><see cref="HDKeyNode"/></term><description>A key expression (<a href="https://github.com/bitcoin/bips/blob/master/bip-0380.mediawiki">BIP0380</a>)</description></item>
		/// <item><term><see cref="Parameter"/></term><description>A key placeholders (<a href="https://github.com/bitcoin/bips/blob/master/bip-0388.mediawiki">BIP0388</a>)</description></item>
		/// </list>
		/// <para>It only supports X/**, or X/&lt;depositIndex;changeIndex&gt;/* where X is a key expression or a parameter.</para>
		/// </summary>
		/// <param name="DepositIndex"></param>
		/// <param name="ChangeIndex"></param>
		/// <param name="Target"></param>
		/// <param name="ShortForm">If <0;1>/* path should be be noted /** instead</param>
		public record MultipathNode(int DepositIndex, int ChangeIndex, MiniscriptNode Target, bool ShortForm) : MiniscriptNode
		{
			public static MultipathNode Parse(string str, Network network)
			{
				if (TryParse(str, network, out var node))
					return node;
				throw new FormatException("Invalid MultipathNode");
			}
			public static bool TryParse(string str, Network network, [MaybeNullWhen(false)] out MultipathNode node)
			{
				MiniscriptNode? target = null;
				node = null;

				var match = Regex.Match(str, "(/<(\\d+);(\\d+)>/\\*)|(/\\*\\*)$");
				if (!match.Success)
					return false;
				int depositIndex = 0;
				int changeIndex = 0;
				bool shortForm = match.Value == "/**";
				if (shortForm)
				{
					depositIndex = 0;
					changeIndex = 1;
				}
				else
				{
					if (!int.TryParse(match.Groups[2].Value, out depositIndex) || depositIndex < 0)
						return false;
					if (!int.TryParse(match.Groups[3].Value, out changeIndex) || changeIndex < 0)
						return false;
				}

				str = str[..(match.Index)];
				if (HDKeyNode.TryParse(str, network, out var keyNode))
					target = keyNode;
				else if (TryParseParameter(str, out var parameter))
					target = parameter;

				if (target is null)
					return false;
				node = new MultipathNode(depositIndex, changeIndex, target, shortForm);
				return true;
			}

			private static bool TryParseParameter(string str, [MaybeNullWhen(false)] out Parameter parameter)
			{
				parameter = null;
				var match = Regex.Match(str, @"^@([\d]+)$");
				if (!match.Success)
					return false;
				if (!int.TryParse(match.Groups[1].Value, out var v) || v < 0)
					return false;
				parameter = new Parameter(match.Value, new ParameterRequirement.HDKey());
				return true;
			}
			public bool CanDerive(AddressIntent intent)
			{
				if (Target is not HDKeyNode)
					return false;
				var idx = GetTypeIndex(intent);
				return (idx >> 31) == 0; // Make sure it isn't hardened derivation
			}
			public BitcoinExtPubKey[] Derive(AddressIntent intent, int[] indexes)
			{
				if (Target is not HDKeyNode { PubKey: var pk })
					throw new InvalidOperationException("Impossible to derive this node because the target is a " + Target.GetType().Name);
				var intentIdx = GetTypeIndex(intent);
				var keys = new BitcoinExtPubKey[indexes.Length];
				Array.Fill(keys, pk.Derive((uint)intentIdx));
				Parallel.For(0, indexes.Length, (i) =>
				{
					keys[i] = keys[i].Derive((uint)indexes[i]);
				});
				return keys;
			}
			public BitcoinExtPubKey Derive(AddressIntent intent, int index)
			{
				if (index < 0)
					throw new ArgumentOutOfRangeException(nameof(index));
				return Derive(intent, new[] { index })[0];
			}
			public int GetTypeIndex(AddressIntent intent) => intent switch { AddressIntent.Deposit => DepositIndex, _ => ChangeIndex };
			protected override void ToString(StringBuilder builder)
			{
				if (CanUseShortForm && ShortForm)
				{
					builder.Append($"{Target}/**");
				}
				else
				{
					builder.Append($"{Target}/<{DepositIndex};{ChangeIndex}>/*");
				}
			}

			public bool CanUseShortForm => (DepositIndex, ChangeIndex) == (0, 1);
		}
		public record HDKeyNode(RootedKeyPath RootedKeyPath, BitcoinExtPubKey PubKey) : MiniscriptNode
		{
			public static HDKeyNode Parse(string str, Network network)
			{
				if (TryParse(str, network, out var n))
					return n;
				throw new FormatException("Invalid HDKeyNode");
			}
			public static bool TryParse(string str, Network network, [MaybeNullWhen(false)] out HDKeyNode hdKeyNode)
			{
				ArgumentNullException.ThrowIfNull(str);
				ArgumentNullException.ThrowIfNull(network);
				hdKeyNode = null;
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
				hdKeyNode = new HDKeyNode(rootedKeyPath, pubkey);
				return true;
			}

			protected override void ToString(StringBuilder builder)
			{
				builder.Append($"[{RootedKeyPath}]{PubKey}");
			}
		}
		public abstract record Fragment(FragmentDescriptor Descriptor) : MiniscriptNode
		{
			public abstract IEnumerable<MiniscriptNode> Parameters { get; }

			protected override void ToString(StringBuilder builder)
			{
				builder.Append(Descriptor.Name);
				builder.Append("(");
				bool first = true;
				foreach (var parameter in Parameters)
				{
					if (first)
						first = false;
					else
						builder.Append(",");
					parameter.ToString(builder);
				}
				builder.Append(")");
			}

			public static FragmentThreeParameters Create(FragmentDescriptor descriptor, MiniscriptNode x, MiniscriptNode y, MiniscriptNode z) => new FragmentThreeParameters(descriptor, x, y, z);
			public static FragmentSingleParameter Create(FragmentDescriptor descriptor, MiniscriptNode x) => new FragmentSingleParameter(descriptor, x);
			public static FragmentNoParameter Zero() => new FragmentNoParameter(FragmentDescriptor._0);
			public static FragmentNoParameter One() => new FragmentNoParameter(FragmentDescriptor._1);
		}
		protected virtual void ToString(StringBuilder builder) { }
		public sealed override string ToString()
		{
			var sb = new StringBuilder();
			ToString(sb);
			return sb.ToString();
		}

		public record FragmentNoParameter(FragmentDescriptor Descriptor) : Fragment(Descriptor)
		{
			public readonly static FragmentNoParameter _0 = new FragmentNoParameter(FragmentDescriptor._0);
			public readonly static FragmentNoParameter _1 = new FragmentNoParameter(FragmentDescriptor._1);
			public override IEnumerable<MiniscriptNode> Parameters => Array.Empty<MiniscriptNode>();
			protected override void ToString(StringBuilder builder)
			{
				builder.Append(Descriptor.Name);
			}
		}

		public record FragmentUnboundedParameters : Fragment
		{
			public static FragmentUnboundedParameters thresh(MiniscriptNode[] parameters) => new FragmentUnboundedParameters(FragmentDescriptor.thresh, parameters);
			public static FragmentUnboundedParameters multi(MiniscriptNode[] parameters) => new FragmentUnboundedParameters(FragmentDescriptor.multi, parameters);
			public static FragmentUnboundedParameters multi_a(MiniscriptNode[] parameters) => new FragmentUnboundedParameters(FragmentDescriptor.multi_a, parameters);


			private readonly MiniscriptNode[] parameters;

			public FragmentUnboundedParameters(FragmentDescriptor descriptor, MiniscriptNode[] parameters) : base(descriptor)
			{
				this.parameters = parameters;
			}
			public override IEnumerable<MiniscriptNode> Parameters => parameters;
		}
		public record FragmentThreeParameters(FragmentDescriptor Descriptor, MiniscriptNode X, MiniscriptNode Y, MiniscriptNode Z) : Fragment(Descriptor)
		{
			public static FragmentThreeParameters andor(MiniscriptNode X, MiniscriptNode Y, MiniscriptNode Z) => new FragmentThreeParameters(FragmentDescriptor.andor, X, Y, Z);
			public override IEnumerable<MiniscriptNode> Parameters
			{
				get
				{
					yield return X;
					yield return Y;
					yield return Z;
				}
			}
		}
		public record FragmentTwoParameters(FragmentDescriptor Descriptor, MiniscriptNode X, MiniscriptNode Y) : Fragment(Descriptor)
		{
			public static FragmentTwoParameters and_v(MiniscriptNode X, MiniscriptNode Y) => new FragmentTwoParameters(FragmentDescriptor.and_v, X, Y);
			public static FragmentTwoParameters and_b(MiniscriptNode X, MiniscriptNode Y) => new FragmentTwoParameters(FragmentDescriptor.and_b, X, Y);
			public static FragmentTwoParameters and_n(MiniscriptNode X, MiniscriptNode Y) => new FragmentTwoParameters(FragmentDescriptor.and_n, X, Y);
			public static FragmentTwoParameters or_b(MiniscriptNode X, MiniscriptNode Y) => new FragmentTwoParameters(FragmentDescriptor.or_b, X, Y);
			public static FragmentTwoParameters or_c(MiniscriptNode X, MiniscriptNode Y) => new FragmentTwoParameters(FragmentDescriptor.or_c, X, Y);
			public static FragmentTwoParameters or_d(MiniscriptNode X, MiniscriptNode Y) => new FragmentTwoParameters(FragmentDescriptor.or_d, X, Y);
			public static FragmentTwoParameters or_i(MiniscriptNode X, MiniscriptNode Y) => new FragmentTwoParameters(FragmentDescriptor.or_i, X, Y);
			public override IEnumerable<MiniscriptNode> Parameters
			{
				get
				{
					yield return X;
					yield return Y;
				}
			}
		}

		public record FragmentSingleParameter(FragmentDescriptor Descriptor, MiniscriptNode X) : Fragment(Descriptor)
		{
			public static FragmentSingleParameter pk(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.pk, X);
			public static FragmentSingleParameter pkh(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.pkh, X);
			public static FragmentSingleParameter wpkh(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.wpkh, X);
			public static FragmentSingleParameter tr(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.tr, X);
			public static FragmentSingleParameter pk_k(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.pk_k, X);
			public static FragmentSingleParameter pk_h(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.pk_h, X);
			public static FragmentSingleParameter older(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.older, X);
			public static FragmentSingleParameter after(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.after, X);
			public static FragmentSingleParameter sha256(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.sha256, X);
			public static FragmentSingleParameter ripemd160(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.ripemd160, X);
			public static FragmentSingleParameter hash256(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.hash256, X);
			public static FragmentSingleParameter hash160(MiniscriptNode X) => new FragmentSingleParameter(FragmentDescriptor.hash160, X);
			public override IEnumerable<MiniscriptNode> Parameters
			{
				get
				{
					yield return X;
				}
			}
		}


		public abstract record ParameterRequirement
		{
			public abstract bool Check(MiniscriptNode node);
			public record Fragment : ParameterRequirement
			{
				public override bool Check(MiniscriptNode node)
				{
					return node is MiniscriptNode.Fragment;
				}
				public sealed override string ToString()
				{
					return "A Fragment is expected";
				}
			}
			public record HDKey : ParameterRequirement
			{
				public override bool Check(MiniscriptNode node)
				{
					return node is MiniscriptNode.HDKeyNode;
				}
				public sealed override string ToString()
				{
					return "A HDKeyNode is expected";
				}
			}
			public record Key(KeyType? RequiredType) : ParameterRequirement
			{
				public override bool Check(MiniscriptNode node)
				{
					return (node, RequiredType) switch
					{
						(Value.PubKeyValue, KeyType.Classic) => true,
						(Value.TaprootPubKeyValue, KeyType.Taproot) => true,
						(TaprootPubKeyValue or PubKeyValue, null) => true,
						(MultipathNode, _) => true,
						_ => false
					};
				}

				public sealed override string ToString()
				{
					return RequiredType switch
					{
						KeyType.Classic => "A PubKeyValue (33 bytes) or a MultiPathKeyInformationNode is expected",
						KeyType.Taproot => "A TaprootPubKeyValue (32 bytes) or a MultiPathKeyInformationNode is expected",
						_ => "A PubKeyValue (33 bytes), TaprootPubKeyValue (32 bytes) or a MultiPathKeyInformationNode is expected"
					};
				}
			}
			public record Hash(int RequiredBytes) : ParameterRequirement
			{
				public override bool Check(MiniscriptNode node)
				{
					return node is Value.HashValue h && h.Hash.Length == RequiredBytes;
				}
				public sealed override string ToString()
				{
					return $"A BytesValue of {RequiredBytes} bytes is expected";
				}
			}
			public record Locktime : ParameterRequirement
			{
				public override bool Check(MiniscriptNode node)
				{
					return node is Value.LockTimeValue;
				}
				public sealed override string ToString()
				{
					return "A LockTimeValue is expected";
				}
			}
		}
		public record Parameter(string Name, ParameterRequirement Requirement) : MiniscriptNode
		{
			protected override void ToString(StringBuilder builder)
			{
				builder.Append(Name);
			}
		}

		public abstract record Value : MiniscriptNode
		{
			public abstract Op CreatePushOp();
			public record CountValue(int Count) : Value
			{
				public override Op CreatePushOp() => Op.GetPushOp(Count);
				protected override void ToString(StringBuilder builder)
				{
					builder.Append(Count.ToString(CultureInfo.InvariantCulture));
				}
			}
			public record LockTimeValue(NBitcoin.LockTime LockTime) : Value
			{
				public override Op CreatePushOp() => Op.GetPushOp(LockTime.Value);
				protected override void ToString(StringBuilder builder)
				{
					builder.Append(LockTime.Value.ToString(CultureInfo.InvariantCulture));
				}
			}
			public record HashValue(byte[] Hash) : Value
			{
				public HashValue(uint160 hash) : this(hash.ToBytes()) { }
				public HashValue(uint256 hash) : this(hash.ToBytes()) { }
				public override Op CreatePushOp() => Op.GetPushOp(Hash);
				protected override void ToString(StringBuilder builder)
				{
					builder.Append(Encoders.Hex.EncodeData(Hash));
				}
			}
			public record PubKeyValue(PubKey PubKey) : Value
			{
				public override Op CreatePushOp() => Op.GetPushOp(PubKey.ToBytes());
				protected override void ToString(StringBuilder builder)
				{
					builder.Append(PubKey.ToHex());
				}
			}
			public record TaprootPubKeyValue(TaprootPubKey PubKey) : Value
			{
				public TaprootInternalPubKey AsInternalPubKey() => new TaprootInternalPubKey(PubKey.ToBytes());
				public override Op CreatePushOp() => Op.GetPushOp(PubKey.ToBytes());
				protected override void ToString(StringBuilder builder)
				{
					builder.Append(PubKey.ToString());
				}
			}
		}
		public record Wrapper(FragmentDescriptor Descriptor, MiniscriptNode X) : FragmentSingleParameter(Descriptor, X)
		{
			public static Wrapper a(MiniscriptNode X) => new(FragmentDescriptor.a, X);
			public static Wrapper s(MiniscriptNode X) => new(FragmentDescriptor.s, X);
			public static Wrapper c(MiniscriptNode X) => new(FragmentDescriptor.c, X);
			public static Wrapper t(MiniscriptNode X) => new(FragmentDescriptor.t, X);
			public static Wrapper d(MiniscriptNode X) => new(FragmentDescriptor.d, X);
			public static Wrapper v(MiniscriptNode X) => new(FragmentDescriptor.v, X);
			public static Wrapper j(MiniscriptNode X) => new(FragmentDescriptor.j, X);
			public static Wrapper n(MiniscriptNode X) => new(FragmentDescriptor.n, X);
			public static Wrapper l(MiniscriptNode X) => new(FragmentDescriptor.l, X);
			public static Wrapper u(MiniscriptNode X) => new(FragmentDescriptor.u, X);

			public IEnumerable<Wrapper> FlattenWrappers()
			{
				yield return this;
				Wrapper wrapper = this;
				while (wrapper.X is Wrapper nextWrapper)
				{
					yield return nextWrapper;
					wrapper = nextWrapper;
				}
			}

			protected override void ToString(StringBuilder builder)
			{
				var wrappers = FlattenWrappers().ToArray();
				var wrappersStr = String.Concat(wrappers.Select(w => w.Descriptor.Name));
				builder.Append(wrappersStr);
				builder.Append(":");
				wrappers[^1].X.ToString(builder);
			}
		}
	}
}
#endif
