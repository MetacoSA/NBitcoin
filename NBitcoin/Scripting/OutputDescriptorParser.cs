using NBitcoin.Scripting.Parser;
using static NBitcoin.Scripting.MiniscriptDSLParser;
using P = NBitcoin.Scripting.Parser.Parser<char, NBitcoin.Scripting.OutputDescriptor>;

namespace NBitcoin.Scripting
{
	public static class OutputDescriptorParser
	{
		private static readonly P PBareP2WSHOutputDescriptor =
			from type in (Parse.String("wsh").Token().Select(_ => OutputDescriptorType.Wsh))
			from _l in Parse.Char('(').Token()
			from policy in MiniscriptDSLParser.DSLParser
			from _r in Parse.Char(')').Token()
			select new OutputDescriptor(Miniscript.FromPolicy(policy), type);

		private static readonly P PBareP2WPKHOutputDescriptor =
			from pk in ExprP("wpkh").Then(s => TryConvert(s, c => new PubKey(c)))
			select new OutputDescriptor(pk, OutputDescriptorType.Wpkh);

		private static readonly P PP2SHP2WSHOutputDescriptor =
			from type in Parse.String("sh").Select(_ => OutputDescriptorType.P2ShWsh)
			from _l in Parse.Char('(').Token()
			from inside in PBareP2WSHOutputDescriptor
			from _r in Parse.Char(')').Token()
			select new OutputDescriptor(inside.InnerScript, type);

		private static readonly P PP2SHP2WPKHOutputDescriptor =
			from type in Parse.String("sh").Select(_ => OutputDescriptorType.P2ShWpkh)
			from _l in Parse.Char('(').Token()
			from inside in PBareP2WPKHOutputDescriptor
			from _r in Parse.Char(')').Token()
			select new OutputDescriptor(inside.PubKey, type);

		private static readonly P PP2SHOutputDescriptor =
			from type in Parse.String("sh").Select(_ => OutputDescriptorType.P2Sh)
			from _l in Parse.Char('(').Token()
			from policy in DSLParser
			from _r in Parse.Char(')').Token()
			select new OutputDescriptor(Miniscript.FromPolicy(policy), type);

		private static readonly P PP2PkhOutputDescriptor =
			from pk in ExprP("pkh").Then(s => TryConvert(s, c => new PubKey(c)))
			select new OutputDescriptor(pk, OutputDescriptorType.Pkh);
		private static readonly P PWitnessOutputDescriptor =
			PBareP2WPKHOutputDescriptor
				.Or(PBareP2WSHOutputDescriptor)
				.Or(PP2SHP2WPKHOutputDescriptor)
				.Or(PP2SHP2WSHOutputDescriptor);
		private static readonly P PNonWitnessOutputDescriptor =
			PP2SHOutputDescriptor.Or(PP2PkhOutputDescriptor);

		private static readonly P PBare =
			from policy in DSLParser
			select new OutputDescriptor(Miniscript.FromPolicy(policy), OutputDescriptorType.Bare);
		internal static readonly P POutputDescriptor =
			PNonWitnessOutputDescriptor
				.Or(PWitnessOutputDescriptor)
				.Or(PBare); // this must be last.

		public static OutputDescriptor ParseDescriptor(string descriptor)
			=> POutputDescriptor.Parse(descriptor);
	}
}