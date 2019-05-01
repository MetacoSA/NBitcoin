using NBitcoin.Miniscript.Parser;
using static NBitcoin.Miniscript.MiniscriptDSLParser;

namespace NBitcoin.Miniscript
{
	public static class OutputDescriptorParser
	{
		private static readonly Parser<char, OutputDescriptor> PBareP2WSHOutputDescriptor =
			from type in (Parse.String("wsh").Token().Select(_ => OutputDescriptorType.Wsh))
			from _l in Parse.Char('(').Token()
			from policy in MiniscriptDSLParser.DSLParser
			from _r in Parse.Char(')').Token()
			select new OutputDescriptor(Miniscript.FromPolicy(policy), type);

		private static readonly Parser<char, OutputDescriptor> PBareP2WPKHOutputDescriptor =
			from pk in ExprP("wpkh").Then(s => TryConvert(s, c => new PubKey(c)))
			select new OutputDescriptor(pk, OutputDescriptorType.Wpkh);

		private static readonly Parser<char, OutputDescriptor> PP2SHP2WSHOutputDescriptor =
			from type in Parse.String("sh").Select(_ => OutputDescriptorType.P2ShWsh)
			from _l in Parse.Char('(').Token()
			from inside in PBareP2WSHOutputDescriptor
			from _r in Parse.Char(')').Token()
			select new OutputDescriptor(inside.InnerScript, type);

		private static readonly Parser<char, OutputDescriptor> PP2SHP2WPKHOutputDescriptor =
			from type in Parse.String("sh").Select(_ => OutputDescriptorType.P2ShWpkh)
			from _l in Parse.Char('(').Token()
			from inside in PBareP2WPKHOutputDescriptor
			from _r in Parse.Char(')').Token()
			select new OutputDescriptor(inside.PubKey, type);

		private static readonly Parser<char, OutputDescriptor> PP2SHOutputDescriptor =
			from type in Parse.String("sh").Select(_ => OutputDescriptorType.P2Sh)
			from _l in Parse.Char('(').Token()
			from policy in DSLParser
			from _r in Parse.Char(')').Token()
			select new OutputDescriptor(Miniscript.FromPolicy(policy), type);

		private static readonly Parser<char, OutputDescriptor> PP2PkhOutputDescriptor =
			from pk in ExprP("pkh").Then(s => TryConvert(s, c => new PubKey(c)))
			select new OutputDescriptor(pk, OutputDescriptorType.Pkh);
		private static readonly Parser<char, OutputDescriptor> PWitnessOutputDescriptor =
			PBareP2WPKHOutputDescriptor
				.Or(PBareP2WSHOutputDescriptor)
				.Or(PP2SHP2WPKHOutputDescriptor)
				.Or(PP2SHP2WSHOutputDescriptor);
		private static readonly Parser<char, OutputDescriptor> PNonWitnessOutputDescriptor =
			PP2SHOutputDescriptor.Or(PP2PkhOutputDescriptor);

		private static readonly Parser<char, OutputDescriptor> PBare =
			from policy in DSLParser
			select new OutputDescriptor(Miniscript.FromPolicy(policy), OutputDescriptorType.Bare);
		internal static readonly Parser<char, OutputDescriptor> POutputDescriptor =
			PNonWitnessOutputDescriptor
				.Or(PWitnessOutputDescriptor)
				.Or(PBare); // this must be last.

		public static OutputDescriptor ParseDescriptor(string descriptor)
			=> POutputDescriptor.Parse(descriptor);
	}
}