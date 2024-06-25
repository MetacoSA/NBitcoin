#if !NO_RECORDS
#nullable enable
using NBitcoin.DataEncoders;
using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static NBitcoin.Scripting.MiniscriptError;
using static NBitcoin.Scripting.MiniscriptNode;
using static NBitcoin.Scripting.MiniscriptNode.Parameter;

namespace NBitcoin.Scripting
{
	public enum KeyType
	{
		Classic,
		Taproot
	}
	public record MiniscriptSettings(Network? Network = null, KeyType? KeyType = null);
	public class Miniscript
	{
		public MiniscriptNode RootNode { get; }
		public IReadOnlyDictionary<string, IReadOnlyCollection<MiniscriptNode.Parameter>> Parameters { get; }
		public MiniscriptSettings Settings { get; }

		public Miniscript(MiniscriptNode rootNode, MiniscriptSettings? settings)
		{
			settings ??= new MiniscriptSettings();
			RootNode = rootNode;
			if (!MiniscriptParametersVisitor.TryCreateParameters(rootNode, out var error, out var parameters))
				throw new MiniscriptFormatException(error);
			Parameters = parameters;

			var visitor = new GuessSettingsVisitor(settings);
			visitor.Visit(rootNode);
			if (visitor.Error is { } err)
				throw new MiniscriptFormatException(err);
			Settings = visitor.Settings;
		}
		Miniscript(
			MiniscriptNode rootNode,
			IReadOnlyDictionary<string, IReadOnlyCollection<MiniscriptNode.Parameter>> parameters,
			MiniscriptSettings? settings
			)
		{
			RootNode = rootNode;
			Parameters = parameters;
			Settings = settings ?? new MiniscriptSettings();
		}

		public override string ToString()
		{
			return RootNode.ToString();
		}

		class Context
		{
			internal class Frame : IDisposable
			{
				internal Frame(Context ctx)
				{
					this.ctx = ctx;
				}
				public int ExpectedParameterCount;
				public List<MiniscriptNode> Parameters = new List<MiniscriptNode>();
				private Context ctx;

				public int FragmentIndex { get; internal set; }

				public void Dispose()
				{
					ctx._frames.Pop();
				}
			}
			public Context(string miniscript, MiniscriptSettings? settings)
			{
				Miniscript = miniscript;
				Settings = settings ?? new MiniscriptSettings();
				ExpectedKeyType = Settings.KeyType;
			}
			public string Miniscript { get; }
			public MiniscriptSettings Settings { get; }
			public int Offset { get; private set; }
			public bool Advance(int charCount)
			{
				SkipSpaces();
				if (Offset + charCount > Miniscript.Length)
					return false;
				Offset += charCount;
				SkipSpaces();
				return true;
			}
			public void SkipSpaces()
			{
				while (Offset < Miniscript.Length && char.IsWhiteSpace(Miniscript[Offset]))
				{
					Offset++;
				}
			}
			public char NextChar => Miniscript[Offset];
			public bool IsEnd => Offset >= Miniscript.Length;
			public string Remaining => Miniscript[Offset..];
			Stack<Frame> _frames = new();
			public Frame CurrentFrame => _frames.Peek();
			public Frame PushFrame()
			{
				Frame f = new Frame(this);
				_frames.Push(f);
				return f;
			}
			public KeyType? ExpectedKeyType { get; internal set; }
			public int FragmentIndex { get; internal set; }

			public override string ToString() => Remaining;
		}
		public static bool TryParse(string str, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out Miniscript miniscript) => TryParse(str, null, out error, out miniscript);
		public static bool TryParse(string str, MiniscriptSettings? settings, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out Miniscript miniscript)
		{
			if (TryParseMiniscript(new Context(str, settings), out error, out miniscript))
				return true;
			miniscript = null;
			return false;
		}
		public static bool TryParse(string str, [MaybeNullWhen(false)] out Miniscript miniscript) => TryParse(str, null, out miniscript);
		public static bool TryParse(string str, MiniscriptSettings? settings, [MaybeNullWhen(false)] out Miniscript miniscript)
		{
			return TryParse(str, settings, out _, out miniscript);
		}

		public static Miniscript Parse(string str) => Parse(str, null);
		public static Miniscript Parse(string str, MiniscriptSettings? settings)
		{
			if (TryParseMiniscript(new Context(str, settings), out var error, out var miniscript))
				return miniscript;
			throw new MiniscriptFormatException(error);
		}

		delegate bool Parsing(Context ctx, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out MiniscriptNode node);

		
		private static bool TryParseExpressions(Context ctx, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out MiniscriptNode node)
		{
			node = null;
			error = null;
			if (ctx.CurrentFrame.Parameters.Count == 0)
			{
				if (!TryParseParameterCount(ctx, out error, out node))
					return false;
				if (node is not Value.CountValue count)
					return true;
				ctx.CurrentFrame.ExpectedParameterCount = count.Count + 1;
				return true;
			}
			else
			{
				if (ctx.CurrentFrame.ExpectedParameterCount == ctx.CurrentFrame.Parameters.Count + 1)
					ctx.CurrentFrame.ExpectedParameterCount = -1;
				return TryParseExpression(ctx, out error, out node);
			}
		}
		private static bool TryParsePubKeys<T>(Context ctx, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out MiniscriptNode node)
		{
			node = null;
			error = null;

			var expectedKeyType = typeof(T) == typeof(Value.PubKeyValue) ? KeyType.Classic : KeyType.Taproot;

			if (ctx.ExpectedKeyType is null)
			{
				ctx.ExpectedKeyType = expectedKeyType;
			}
			else if (ctx.ExpectedKeyType != expectedKeyType)
			{
				error = new MiniscriptError.UnsupportedFragment(ctx.FragmentIndex, ctx.ExpectedKeyType.Value);
			}

			if (ctx.CurrentFrame.Parameters.Count == 0)
			{
				if (!TryParseParameterCount(ctx, out error, out node))
					return false;
				if (node is not Value.CountValue count)
					return true;
				ctx.CurrentFrame.ExpectedParameterCount = count.Count + 1;
				return true;
			}
			else
			{
				if (ctx.CurrentFrame.ExpectedParameterCount == ctx.CurrentFrame.Parameters.Count + 1)
					ctx.CurrentFrame.ExpectedParameterCount = -1;
				return TryParseKey(ctx, out error, out node);
			}
		}
		private static bool TryParseParameterCount(Context ctx, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out MiniscriptNode node)
		{
			error = null;
			node = null;
			var match = Regex.Match(ctx.Remaining, @"^[0-9]+");
			if (uint.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) && v < 100 && v > 0)
			{
				ctx.Advance(match.Length);
				node = new Value.CountValue((int)v);
				return true;
			}
			error = new MiniscriptError.CountExpected(ctx.Offset);
			return false;
		}
		private static bool TryParse32Bytes(Context ctx, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out MiniscriptNode node) => TryParseBytes(ctx, 32, out error, out node);
		private static bool TryParse20Bytes(Context ctx, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out MiniscriptNode node) => TryParseBytes(ctx, 20, out error, out node);
		private static bool TryParseBytes(Context ctx, int requiredBytes, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out MiniscriptNode node)
		{
			node = null;
			error = null;
			var match = Regex.Match(ctx.Remaining, @"^[a-f0-9]{17,}");
			if (match.Success && match.Length == requiredBytes * 2)
			{
				ctx.Advance(match.Length);
				node = new Value.HashValue(Encoders.Hex.DecodeData(match.Value));
				return true;
			}

			if (TryParseName(ctx.Remaining, out var name))
			{
				ctx.Advance(name.Length);
				node = new Parameter(name, new ParameterRequirement.Hash(requiredBytes));
				return true;
			}

			error = new MiniscriptError.HashExpected(ctx.Offset, requiredBytes);
			return false;
		}

		private static bool TryParseLocktime(Context ctx, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out MiniscriptNode node)
		{
			node = null;
			error = null;
			var match = Regex.Match(ctx.Remaining, @"^[0-9]+");
			if (uint.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
			{
				ctx.Advance(match.Length);
				var locktime = new LockTime(v);
				node = new Value.LockTimeValue(locktime);
				return true;

			}
			if (TryParseName(ctx.Remaining, out var name))
			{
				ctx.Advance(name.Length);
				node = new Parameter(name, new ParameterRequirement.Locktime());
				return true;
			}
			error = new MiniscriptError.LocktimeExpected(ctx.Offset);
			return false;
		}
		private static bool TryParseKey(Context ctx, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out MiniscriptNode node)
		{
			node = null;
			error = null;
			var key = Regex.Match(ctx.Remaining, @"^[a-f0-9]+");
			if (key is { Success: true, Length: (33 * 2 or 32 * 2) })
			{
				ctx.Advance(key.Length);
				try
				{
					if (key.Length == 33 * 2 && ctx.ExpectedKeyType is (null or KeyType.Classic))
						node = new Value.PubKeyValue(new PubKey(Encoders.Hex.DecodeData(key.Value)));
					else if (key.Length == 32 * 2 && ctx.ExpectedKeyType is (null or KeyType.Classic))
						node = new Value.TaprootPubKeyValue(new TaprootPubKey(Encoders.Hex.DecodeData(key.Value)));
					if (node != null)
						return true;
				}
				catch
				{
					error = new MiniscriptError.KeyExpected(ctx.Offset, ctx.ExpectedKeyType);
					return false;
				}
			}

			if (TryParseName(ctx.Remaining, out var name))
			{
				ctx.Advance(name.Length);
				node = new Parameter(name, new ParameterRequirement.Key(ctx.ExpectedKeyType));
				return true;
			}
			if (TryParseKeyPlaceholder(ctx, out var kp))
			{
				node = MiniscriptNode.Create(kp);
				return true;
			}

			if (ctx.Settings.Network is { } network)
			{
				if (TryParseMultiPathKeyInformation(ctx, network, out var ki))
				{
					node = MiniscriptNode.Create(ki);
					return true;
				}
			}

			error = new MiniscriptError.KeyExpected(ctx.Offset, ctx.ExpectedKeyType);
			return false;
		}

		private static bool TryParseMultiPathKeyInformation(Context ctx, Network network, [MaybeNullWhen(false)] out MultiPathKeyInformation ki)
		{
			ki = null;
			var match = Regex.Match(ctx.Remaining, @"^[\[\]a-z0-9A-Z'/<>;*]+");
			if (!match.Success)
				return false;
			if (!MultiPathKeyInformation.TryParse(match.Value, network, out ki))
				return false;
			ctx.Advance(match.Length);
			return true;
		}

		private static bool TryParseKeyPlaceholder(Context ctx, [MaybeNullWhen(false)] out KeyPlaceholder kp)
		{
			kp = null;
			var remaining = ctx.Remaining;
			var match = Regex.Match(remaining, @"^@[\d<>;*/]*");
			if (!match.Success)
				return false;
			if (!KeyPlaceholder.TryParse(match.Value, out kp))
				return false;
			ctx.Advance(match.Length);
			return true;
		}

		private static bool TryParseName(string str, [MaybeNullWhen(false)] out string name)
		{
			var key = Regex.Match(str, @"^[a-zA-Z0-9_]+");
			if (key is { Success: true, Length: <= 16 * 2 })
			{
				name = key.Value;
				return true;
			}
			name = null;
			return false;
		}
		private static bool TryParseMiniscript(Context ctx, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out Miniscript miniscript)
		{
			miniscript = null;
			if (TryParseExpression(ctx, out error, out var node) &&
				MiniscriptParametersVisitor.TryCreateParameters(node, out error, out var parameters))
			{
				var settings = ctx.Settings;
				var visitor = new GuessSettingsVisitor(settings);
				visitor.Visit(node);
				if (visitor.Error is { } err)
				{
					error = err;
					return false;
				}
				miniscript = new Miniscript(node, parameters, visitor.Settings);
				return true;
			}
			miniscript = null;
			return false;
		}
		private static bool TryParseExpression(Context ctx, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out MiniscriptNode node)
		{
			node = null;
			error = null;

			ctx.SkipSpaces();

			int endOffset = ctx.Offset;
			var match = Regex.Match(ctx.Remaining, @"^([a-z0-9_]+?:)?([a-zA-Z0-9_]+)");

			var wrapperGroup = match.Groups[1];
			var fragmentName = match.Groups[2];

			if (fragmentName.Success)
			{
				using var frame = ctx.PushFrame();
				frame.FragmentIndex = ctx.Offset + fragmentName.Index;
				ctx.Advance(fragmentName.Index + fragmentName.Length);
				node = fragmentName.Value switch
				{
					"0" => FragmentNoParameter._0,
					"1" => FragmentNoParameter._1,
					"pk_k" => TryParseParameters(ctx, 1, TryParseKey, out error, out var p) ? FragmentSingleParameter.pk_k(p[0]) : null,
					"pk_h" => TryParseParameters(ctx, 1, TryParseKey, out error, out var p) ? FragmentSingleParameter.pk_h(p[0]) : null,
					"pk" => TryParseParameters(ctx, 1, TryParseKey, out error, out var p) ? FragmentSingleParameter.pk(p[0]) : null,
					"pkh" => TryParseParameters(ctx, 1, TryParseKey, out error, out var p) ? FragmentSingleParameter.pkh(p[0]) : null,
					"older" => TryParseParameters(ctx, 1, TryParseLocktime, out error, out var p) ? FragmentSingleParameter.older(p[0]) : null,
					"after" => TryParseParameters(ctx, 1, TryParseLocktime, out error, out var p) ? FragmentSingleParameter.after(p[0]) : null,
					"sha256" => TryParseParameters(ctx, 1, TryParse32Bytes, out error, out var p) ? FragmentSingleParameter.sha256(p[0]) : null,
					"ripemd160" => TryParseParameters(ctx, 1, TryParse20Bytes, out error, out var p) ? FragmentSingleParameter.ripemd160(p[0]) : null,
					"hash256" => TryParseParameters(ctx, 1, TryParse32Bytes, out error, out var p) ? FragmentSingleParameter.hash256(p[0]) : null,
					"hash160" => TryParseParameters(ctx, 1, TryParse20Bytes, out error, out var p) ? FragmentSingleParameter.hash160(p[0]) : null,
					"andor" => TryParseParameters(ctx, 3, TryParseExpression, out error, out var p) ? FragmentThreeParameters.andor(p[0], p[1], p[2]) : null,
					"and_v" => TryParseParameters(ctx, 2, TryParseExpression, out error, out var p) ? FragmentTwoParameters.and_v(p[0], p[1]) : null,
					"and_b" => TryParseParameters(ctx, 2, TryParseExpression, out error, out var p) ? FragmentTwoParameters.and_b(p[0], p[1]) : null,
					"and_n" => TryParseParameters(ctx, 2, TryParseExpression, out error, out var p) ? FragmentTwoParameters.and_n(p[0], p[1]) : null,
					"or_b" => TryParseParameters(ctx, 2, TryParseExpression, out error, out var p) ? FragmentTwoParameters.or_b(p[0], p[1]) : null,
					"or_c" => TryParseParameters(ctx, 2, TryParseExpression, out error, out var p) ? FragmentTwoParameters.or_c(p[0], p[1]) : null,
					"or_d" => TryParseParameters(ctx, 2, TryParseExpression, out error, out var p) ? FragmentTwoParameters.or_d(p[0], p[1]) : null,
					"or_i" => TryParseParameters(ctx, 2, TryParseExpression, out error, out var p) ? FragmentTwoParameters.or_i(p[0], p[1]) : null,
					"thresh" => TryParseParameters(ctx, 1, TryParseExpressions, out error, out var p) ? FragmentUnboundedParameters.thresh(p) : null,
					"multi" => TryParseParameters(ctx, 1, TryParsePubKeys<Value.PubKeyValue>, out error, out var p) ? FragmentUnboundedParameters.multi(p) : null,
					"multi_a" => TryParseParameters(ctx, 1, TryParsePubKeys<Value.TaprootPubKeyValue>, out error, out var p) ? FragmentUnboundedParameters.multi_a(p) : null,
					_ => null
				};
				if (node is null && error is null)
				{
					if (ctx.IsEnd || (ctx.NextChar != '(' && fragmentName.Length <= 16))
					{
						node = new Parameter(fragmentName.Value, new ParameterRequirement.Fragment());
					}
					else
					{
						error = new UnknownFragmentName(frame.FragmentIndex, fragmentName.Value);
						return false;
					}
				}
			}

			if (node is null)
			{
				error ??= new IncompleteExpression(ctx.Offset);
				return false;
			}

			if (wrapperGroup.Success)
			{
				for (var i = wrapperGroup.Value.Length - 2; i >= 0; i--)
				{
					Wrapper? wrapper =
						wrapperGroup.Value[i] switch
						{
							'a' => Wrapper.a(node),
							'c' => Wrapper.c(node),
							'd' => Wrapper.d(node),
							'j' => Wrapper.j(node),
							'l' => Wrapper.l(node),
							'n' => Wrapper.n(node),
							's' => Wrapper.s(node),
							't' => Wrapper.t(node),
							'u' => Wrapper.u(node),
							'v' => Wrapper.v(node),
							_ => null
						};
					if (wrapper is null)
					{
						error = new MiniscriptError.InvalidWrapper(wrapperGroup.Value[i], ctx.Offset + i);
						return false;
					}
					node = wrapper;
				}
			}
			error = null;
			return true;
		}

		private static bool TryParseParameters(Context ctx, int reqParams, Parsing parse, [MaybeNullWhen(true)] out MiniscriptError error, [MaybeNullWhen(false)] out MiniscriptNode[] parameters)
		{
			var frame = ctx.CurrentFrame;
			error = null;
			parameters = null;
			if (ctx.IsEnd)
			{
				error = new IncompleteExpression(ctx.Offset - 1);
				return false;
			}
			if (ctx.NextChar != '(')
			{
				error = new UnexpectedToken(ctx.Offset);
				parameters = null;
				return false;
			}
			ctx.Advance(1);

			frame.ExpectedParameterCount = reqParams;
			while (parse(ctx, out error, out var node))
			{
				frame.Parameters.Add(node);
				if (frame.Parameters.Count == frame.ExpectedParameterCount)
				{
					if (ctx.IsEnd)
					{
						error = new IncompleteExpression(ctx.Offset - 1);
						return false;
					}
					if (ctx.NextChar != ')')
					{
						error = new TooManyParameters(ctx.Offset, frame.ExpectedParameterCount);
						return false;
					}
					ctx.Advance(1);
					break;
				}
				else
				{
					if (ctx.IsEnd)
					{
						error = new IncompleteExpression(ctx.Offset - 1);
						return false;
					}
					if (frame.ExpectedParameterCount == -1 && ctx.NextChar == ')')
					{
						ctx.Advance(1);
						break;
					}
					if (ctx.NextChar != ',')
					{
						error = new TooFewParameters(ctx.Offset, frame.ExpectedParameterCount);
						return false;
					}
					ctx.Advance(1);
				}
			}
			if (error is not null)
				return false;
			parameters = frame.Parameters.ToArray();
			return true;
		}

		public SatisfactionPathBuilder BuildSatisfactionPath() => new SatisfactionPathBuilder(this);

		public Miniscript ReplaceParameters(Dictionary<string, MiniscriptNode> values, bool skipRequirements = false)
		{
			var v = new MiniscriptParameterReplacementVisitor(values);
			v.SkipRequirements = skipRequirements;
			var newNode = v.Visit(RootNode);
			if (!MiniscriptParametersVisitor.TryCreateParameters(newNode, out var error, out var parameters))
				throw new MiniscriptFormatException(error);

			var visitor = new GuessSettingsVisitor(Settings);
			visitor.Visit(newNode);
			if (visitor.Error is { } err)
				throw new MiniscriptFormatException(err);
			return new Miniscript(newNode, parameters, visitor.Settings);
		}

		public Script ToScript()
		{
			if (Parameters.Count > 0)
				throw new InvalidOperationException("Impossible to generate a script while parameters exists. Use ToScriptString() if you want to have a string representation instead.");
			List<Op> ops = new List<Op>();
			((Fragment)RootNode).AddOps(ops);
			return new Script(ops.ToArray());
		}

		public string ToScriptString()
		{
			Dictionary<string, MiniscriptNode> values = new();
			List<(string From, string To)> replacements = new();
			// Since ToScript isn't possible so long as there are parameters, we need to replace them with values.
			// Then we replace those values with the original parameter names.
			foreach (var param in Parameters)
			{
				var parameter = param.Value.First();
				var requirement = parameter.Requirement;
				if (requirement is MiniscriptNode.ParameterRequirement.Key or MiniscriptNode.ParameterRequirement.KeyInformation)
				{
					var requiredType = requirement switch
					{
						MiniscriptNode.ParameterRequirement.Key k => k.RequiredType,
						_ => Settings.KeyType
					};
					requiredType ??= Settings.KeyType;
					var paramName = param.Key;
					if (parameter is Parameter.KeyPlaceholderParameter kp)
						paramName = kp.ToString();

					var pk = new Key().PubKey;
					if (requiredType is KeyType.Taproot)
					{
						var pkk = pk.TaprootPubKey;
						replacements.Add((pkk.ToString(), $"<{paramName}>"));
						values.Add(param.Key, new Value.TaprootPubKeyValue(pkk));
					}
					else
					{
						replacements.Add((pk.ToString(), $"<{paramName}>"));
						replacements.Add((pk.Hash.ToString(), $"<HASH160({paramName})>"));
						values.Add(param.Key, new Value.PubKeyValue(pk));
					}
				}
				else if (requirement is MiniscriptNode.ParameterRequirement.Hash h)
				{
					var bytes = RandomUtils.GetBytes(h.RequiredBytes);
					replacements.Add((Encoders.Hex.EncodeData(bytes), $"<{param.Key}>"));
					values.Add(param.Key, new Value.HashValue(bytes));
				}
				else if (requirement is MiniscriptNode.ParameterRequirement.Locktime l)
				{
					var rand = new LockTime(RandomUtils.GetUInt32());
					replacements.Add((Encoders.Hex.EncodeData(Op.GetPushOp(rand.Value).PushData), $"<{param.Key}>"));
					values.Add(param.Key, new Value.LockTimeValue(rand));
				}
				else if (requirement is MiniscriptNode.ParameterRequirement.Fragment)
				{
					var pkStr = new Key().PubKey.ToString();
					var pk = $"pk_k({pkStr})";
					replacements.Add((pkStr, $"<{param.Key}>"));
					values.Add(param.Key, Miniscript.Parse(pk, Settings).RootNode);
				}
				else
					throw new NotSupportedException($"Unable to generate the script's string with a requirement of type {requirement.GetType().Name}");
			}
			var miniscript2 = ReplaceParameters(values, true);
			var script = new StringBuilder(miniscript2.ToScript().ToString());
			foreach (var replacement in replacements)
			{
				script.Replace(replacement.From, replacement.To);
			}
			return script.ToString();
		}

		public Miniscript Rewrite(MiniscriptRewriterVisitor rewriterVisitor)
		{
			var newNode = rewriterVisitor.Visit(RootNode);
			return new Miniscript(newNode, Settings);
		}
		public void Visit(MiniscriptVisitor visitor)
		{
			visitor.Visit(RootNode);
		}

		public Miniscript Derive(AddressIntent intent, int index, KeyType? keyType)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index), "index should be positive");

			keyType ??= Settings.KeyType;
			if (keyType is null)
				throw new InvalidOperationException("The parameter 'keyType' should be set to call this method, as it not possible to guess the keytype to use from the context.");
			var visitor = new DeriveVisitor(intent, index, keyType.Value);
			var newNode = visitor.Visit(RootNode);
			return new Miniscript(newNode, Settings with { KeyType = keyType });
		}

		/// <summary>
		/// Make the miniscript more readable by removing and <see cref="KeyInformation"/> (such as [fingerprint]xpub/<0;1>/*) by <see cref="KeyPlaceholder"/> (such as @0/<0;1>/*)
		/// </summary>
		/// <param name="preferShortForm">If `true`, then the <see cref="KeyPlaceholder"/> @i/<0;1>/* will take the form @i/** </param>
		/// <returns>The modified <see cref="Miniscript"/></returns>
		public Miniscript ReplaceKeyInformationByPlaceholder(bool preferShortForm = true)
		{
			return Rewrite(new RemoveKeyInformation(preferShortForm));
		}

		public SatisfactionRequirement[] GetRequiredSatisfactions(SatisfactionPath satisfactionPath)
		{
			var visitor = new SatisfactionRequirementVisitor(satisfactionPath);
			visitor.Visit(this.RootNode);
			return visitor.Requirements.ToArray();
		}
	}
	public record MiniscriptError
	{
		public record CountExpected(int Index) : MiniscriptError
		{
			public override string ToString() => $"Count expected at index {Index}";
		}
		public record MixedParameterType(string parameterName) : MiniscriptError
		{
			public override string ToString() => $"Mixed parameter type '{parameterName}'";
		}
		public record MixedNetworks : MiniscriptError
		{
			public override string ToString() => "Mixed Networks detected in the expression";
		}
		public record MixedKeyTypes : MiniscriptError
		{
			public override string ToString() => "Mixed KeyTypes detected in the expression";
		}
		public record IncompleteExpression(int Index) : MiniscriptError
		{
			public override string ToString() => $"Incomplete expression at index {Index}";
		}
		public record UnexpectedToken(int Index) : MiniscriptError
		{
			public override string ToString() => $"Unexpected token at index {Index}";
		}
		public record HashExpected(int Index, int RequiredBytes) : MiniscriptError
		{
			public override string ToString() => $"Hash of length {RequiredBytes} is expected at index {Index}";
		}
		public record LocktimeExpected(int Index) : MiniscriptError
		{
			public override string ToString() => $"Locktime expected at index {Index}";
		}
		public record UnsupportedFragment(int Index, KeyType KeyType) : MiniscriptError
		{
			public override string ToString() => $"Only fragments using KeyType {KeyType} are supported in this context";
		}
		public record KeyExpected(int Index, KeyType? Type) : MiniscriptError
		{
			public override string ToString() =>
				Type switch
				{
					KeyType.Taproot => $"Taproot PubKey expected at index {Index} (32 bytes)",
					KeyType.Classic => $"PubKey expected at index {Index} (33 bytes)",
					_ => $"Key expected at index {Index} (33 or 32 bytes)"
				};
		}
		public record UnknownFragmentName(int Index, string FragmentName) : MiniscriptError
		{
			public override string ToString() => $"Unknown fragment name '{FragmentName}' at index {Index}";
		}
		public record TooManyParameters(int Index, int Expected) : MiniscriptError
		{
			public override string ToString() => $"Too many parameters at index {Index}, expected {Expected}";
		}
		public record TooFewParameters(int Index, int Expected) : MiniscriptError
		{
			public override string ToString() => $"Too few parameters at index {Index}, expected {Expected}";
		}
		public record InvalidWrapper(char wrapper, int Index) : MiniscriptError
		{
			public override string ToString() => $"Invalid wrapper '{wrapper}' at index {Index}";
		}
	}
	public class MiniscriptFormatException : FormatException
	{
		public MiniscriptFormatException(MiniscriptError error) : base(error.ToString())
		{
			Error = error;
		}
		public MiniscriptError Error { get; }
	}
}
#endif
