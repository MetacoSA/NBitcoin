#if !NO_RECORDS
#nullable enable
using NBitcoin.Crypto;
using NBitcoin.DataEncoders;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.Scripting.MiniscriptNode.ParameterRequirement;
using static NBitcoin.Scripting.MiniscriptNode.Value;

namespace NBitcoin.Scripting
{
	public class FragmentDescriptor
	{
		public bool IsOr() =>
			this == or_b ||
			this == or_c ||
			this == or_d ||
			this == or_i;
		public bool IsHash() =>
			this == sha256 ||
			this == ripemd160 ||
			this == hash256 ||
			this == hash160;
		private static Op HASH160(MiniscriptNode node) => Op.GetPushOp(Hashes.Hash160(PushOp(node).PushData).ToBytes());
		private static Op PushOp(MiniscriptNode node) => ((MiniscriptNode.Value)node).CreatePushOp();
		private static void AddFragment(List<Op> ops, MiniscriptNode node)
		{
			var fragment = ((MiniscriptNode.Fragment)node);
			if (fragment is MiniscriptNode.FragmentNoParameter)
				fragment.Descriptor.AddOps(Array.Empty<MiniscriptNode.Value>(), ops);
			else
			{
				var parameters = fragment.Parameters.ToArray();
				fragment.Descriptor.AddOps(parameters, ops);
			}
		}
		FragmentDescriptor(string name,
			Action<MiniscriptNode[], List<Op>> addOps)
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
			(v, ops) => ops.Add(PushOp(v[0])));
		public readonly static FragmentDescriptor pk_h = new(
			"pk_h",
			(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_DUP, OpcodeType.OP_HASH160, HASH160(v[0]), OpcodeType.OP_EQUALVERIFY }));
		public readonly static FragmentDescriptor older = new(
			"older",
			(v, ops) => ops.AddRange(new Op[] { PushOp(v[0]), OpcodeType.OP_CHECKSEQUENCEVERIFY }));
		public readonly static FragmentDescriptor after = new(
			"after",
			(v, ops) => ops.AddRange(new Op[] { PushOp(v[0]), OpcodeType.OP_CHECKLOCKTIMEVERIFY }));
		public readonly static FragmentDescriptor sha256 = new(
			"sha256",
			(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_SIZE, Op.GetPushOp(0x20), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_SHA256, PushOp(v[0]), OpcodeType.OP_EQUAL }));
		public readonly static FragmentDescriptor ripemd160 = new(
			"ripemd160",
			(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_SIZE, Op.GetPushOp(0x20), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_RIPEMD160, PushOp(v[0]), OpcodeType.OP_EQUAL }));
		public readonly static FragmentDescriptor hash256 = new(
			"hash256",
			(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_SIZE, Op.GetPushOp(0x20), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_HASH256, PushOp(v[0]), OpcodeType.OP_EQUAL }));
		public readonly static FragmentDescriptor hash160 = new(
			"hash160",
			(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_SIZE, Op.GetPushOp(0x20), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_HASH160, PushOp(v[0]), OpcodeType.OP_EQUAL }));
		public readonly static FragmentDescriptor and_v = new(
			"and_v",
			(v, ops) =>
			{
				AddFragment(ops, v[0]);
				AddFragment(ops, v[1]);
			});

		public readonly static FragmentDescriptor and_b = new(
			"and_b",
			(v, ops) =>
			{
				AddFragment(ops, v[0]);
				AddFragment(ops, v[1]);
				ops.Add(OpcodeType.OP_BOOLAND);
			});
		public readonly static FragmentDescriptor and_n = new(
			"and_n",
			(v, ops) => 
			{
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_NOTIF);
				ops.Add(OpcodeType.OP_0);
				ops.Add(OpcodeType.OP_ELSE);
				AddFragment(ops, v[1]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor or_b = new(
			"or_b",
			(v, ops) =>
			{
				AddFragment(ops, v[0]);
				AddFragment(ops, v[1]);
				ops.Add(OpcodeType.OP_BOOLOR);
			});
		public readonly static FragmentDescriptor or_d = new(
			"or_d",
			(v, ops) =>
			{
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_IFDUP);
				ops.Add(OpcodeType.OP_NOTIF);
				AddFragment(ops, v[1]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor or_c = new(
			"or_c",
			(v, ops) =>
			{
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_NOTIF);
				AddFragment(ops, v[1]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor or_i = new(
			"or_i",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_IF);
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_ELSE);
				AddFragment(ops, v[1]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor andor = new(
			"andor",
			(v, ops) =>
			{
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_NOTIF);
				AddFragment(ops, v[2]);
				ops.Add(OpcodeType.OP_ELSE);
				AddFragment(ops, v[1]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor thresh = new(
			"thresh",
			(v, ops) =>
			{
				int i = 0;
				i++; // Skip count
				AddFragment(ops, v[i++]);
				while (i < v.Length)
				{
					AddFragment(ops, v[i++]);
					ops.Add(OpcodeType.OP_ADD);
				}
				ops.Add(PushOp(v[0]));
				ops.Add(OpcodeType.OP_EQUAL);
			});
		public readonly static FragmentDescriptor multi = new(
			"multi",
			(v, ops) =>
			{
				int i = 0;
				while (i < v.Length)
				{
					ops.Add(PushOp(v[i++]));
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
				ops.Add(PushOp(v[i++]));
				ops.Add(OpcodeType.OP_CHECKSIG);
				while (i < v.Length)
				{
					ops.Add(PushOp(v[i++]));
					ops.Add(OpcodeType.OP_CHECKSIGADD);
				}
				ops.Add(PushOp(v[0]));
				ops.Add(OpcodeType.OP_NUMEQUAL);
			});

		public readonly static FragmentDescriptor a = new(
			"a",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_TOALTSTACK);
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_FROMALTSTACK);
			});
		public readonly static FragmentDescriptor s = new(
			"s",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_SWAP);
				AddFragment(ops, v[0]);
			});
		public readonly static FragmentDescriptor c = new(
			"c",
			(v, ops) =>
			{
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_CHECKSIG);
			});
		public readonly static FragmentDescriptor t = new(
			"t",
			(v, ops) =>
			{
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_1);
			});
		public readonly static FragmentDescriptor d = new(
			"d",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_DUP);
				ops.Add(OpcodeType.OP_IF);
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor v = new(
			"v",
			(v, ops) =>
			{
				AddFragment(ops, v[0]);
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
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor n = new(
			"n",
			(v, ops) =>
			{
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_0NOTEQUAL);
			});
		public readonly static FragmentDescriptor l = new(
			"l",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_IF);
				ops.Add(OpcodeType.OP_0);
				ops.Add(OpcodeType.OP_ELSE);
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_ENDIF);
			});
		public readonly static FragmentDescriptor u = new(
			"u",
			(v, ops) =>
			{
				ops.Add(OpcodeType.OP_IF);
				AddFragment(ops, v[0]);
				ops.Add(OpcodeType.OP_ELSE);
				ops.Add(OpcodeType.OP_0);
				ops.Add(OpcodeType.OP_ENDIF);
			});

		public readonly static FragmentDescriptor pkh = new(
		"pkh",
		(v, ops) => ops.AddRange(new Op[] { OpcodeType.OP_DUP, OpcodeType.OP_HASH160, HASH160(v[0]), OpcodeType.OP_EQUALVERIFY, OpcodeType.OP_CHECKSIG }));
		public readonly static FragmentDescriptor pk = new(
		"pk",
		(v, ops) => ops.AddRange(new[] { PushOp(v[0]), (Op)OpcodeType.OP_CHECKSIG }));

		public string Name { get; }
		public Action<MiniscriptNode[], List<Op>> AddOps { get; }
		public override string ToString() => Name;
	}
	public record MiniscriptNode
	{
		public static Parameter.KeyPlaceholderParameter Create(KeyPlaceholder keyPlaceholder) => new Parameter.KeyPlaceholderParameter(keyPlaceholder, new ParameterRequirement.KeyInformation());
		public static KeyInformationNode Create(KeyInformation keyInformation) => new KeyInformationNode(keyInformation);
		public static Value.PubKeyValue Create(PubKey pubkey) => new Value.PubKeyValue(pubkey);
		public static Value.TaprootPubKeyValue Create(TaprootPubKey pubkey) => new Value.TaprootPubKeyValue(pubkey);
		public static MultiPathKeyInformationNode Create(MultiPathKeyInformation keyInformation) => new MultiPathKeyInformationNode(keyInformation);
		public record MultiPathKeyInformationNode(MultiPathKeyInformation KeyInformation) : MiniscriptNode
		{
			protected override void ToString(StringBuilder builder)
			{
				builder.Append(KeyInformation.ToString());
			}
		}
		public record KeyInformationNode(KeyInformation KeyInformation) : MiniscriptNode
		{
			protected override void ToString(StringBuilder builder)
			{
				builder.Append(KeyInformation.ToString());
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

			public void AddOps(List<Op> ops)
			{
				Descriptor.AddOps(Parameters.ToArray(), ops);
			}

			public static FragmentThreeParameters Create(FragmentDescriptor descriptor, MiniscriptNode x, MiniscriptNode y, MiniscriptNode z) => new FragmentThreeParameters(descriptor, x, y, z);
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
			public record KeyInformation : ParameterRequirement
			{
				public override bool Check(MiniscriptNode node)
				{
					return node is MiniscriptNode.KeyInformationNode;
				}
				public sealed override string ToString()
				{
					return "A KeyInformation is expected";
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
						(MultiPathKeyInformationNode, _) => true,
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
			public record KeyPlaceholderParameter(KeyPlaceholder KeyPlaceholder, ParameterRequirement.KeyInformation KeyInformationRequirement) : Parameter($"@{KeyPlaceholder.KeyIndex}", KeyInformationRequirement)
			{
				protected override void ToString(StringBuilder builder)
				{
					builder.Append(KeyPlaceholder.ToString());
				}
			}
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
				public override Op CreatePushOp() => Op.GetPushOp(PubKey.ToBytes());
				protected override void ToString(StringBuilder builder)
				{
					builder.Append(PubKey.ToString());
				}
			}
		}
		public record Wrapper(FragmentDescriptor Descriptor, MiniscriptNode X) : FragmentSingleParameter(Descriptor, X)
		{
			public static Wrapper a(MiniscriptNode X) => new (FragmentDescriptor.a, X);
			public static Wrapper s(MiniscriptNode X) => new (FragmentDescriptor.s, X);
			public static Wrapper c(MiniscriptNode X) => new (FragmentDescriptor.c, X);
			public static Wrapper t(MiniscriptNode X) => new (FragmentDescriptor.t, X);
			public static Wrapper d(MiniscriptNode X) => new (FragmentDescriptor.d, X);
			public static Wrapper v(MiniscriptNode X) => new (FragmentDescriptor.v, X);
			public static Wrapper j(MiniscriptNode X) => new (FragmentDescriptor.j, X);
			public static Wrapper n(MiniscriptNode X) => new (FragmentDescriptor.n, X);
			public static Wrapper l(MiniscriptNode X) => new (FragmentDescriptor.l, X);
			public static Wrapper u(MiniscriptNode X) => new (FragmentDescriptor.u, X);

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
