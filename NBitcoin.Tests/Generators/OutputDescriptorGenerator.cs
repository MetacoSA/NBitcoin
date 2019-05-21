using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NBitcoin.Scripting;

namespace NBitcoin.Tests.Generators
{
	public class OutputDescriptorGenerator
	{
		public static Arbitrary<OutputDescriptor> OutputDescriptorArb() =>
			Arb.From(OutputDescriptorGen());

		public class ArbitraryOutputDescriptor : Arbitrary<OutputDescriptor>
		{
			public override Gen<OutputDescriptor> Generator { get { return OutputDescriptorGen(); } }

			public override IEnumerable<OutputDescriptor> Shrinker(OutputDescriptor parent)
			{
				switch (parent)
				{
					case OutputDescriptor.MultisigDescriptor p:
						foreach (var prov in p.PkProviders)
						{
							yield return OutputDescriptor.NewPK(prov);
							yield return OutputDescriptor.NewPKH(prov);
							yield return OutputDescriptor.NewWPKH(prov);
						}
						if (p.PkProviders.Count > 2)
							yield return OutputDescriptor.NewMulti(2, p.PkProviders.Take(2).ToArray());
						foreach (var i in Arb.Shrink(p.PkProviders))
						{
							if (i.Count > 2)
								yield return OutputDescriptor.NewMulti(2, i.ToList());
						}
						yield break;
					default:
						yield break;
				}
			}
		}

		public static Gen<OutputDescriptor> OutputDescriptorGen() =>
			Gen.OneOf(
				AddrOutputDescriptorGen(),
				RawOutputDescriptorGen(),
				PKOutputDescriptorGen(),
				PKHOutputDescriptorGen(),
				WPKHOutputDescriptorGen(),
				ComboOutputDescriptorGen(),
				MultisigOutputDescriptorGen(20),
				SHOutputDescriptorGen(),
				WSHOutputDescriptorGen()
				);
		private static Gen<OutputDescriptor> AddrOutputDescriptorGen() =>
			from addr in AddressGenerator.RandomAddress()
			select OutputDescriptor.NewAddr(addr);

		private static Gen<OutputDescriptor> RawOutputDescriptorGen() =>
			from addr in ScriptGenerator.RandomScriptSig()
			select OutputDescriptor.NewRaw(addr);
		private static Gen<OutputDescriptor> PKOutputDescriptorGen() =>
			from pkProvider in PubKeyProviderGen()
			select OutputDescriptor.NewPK(pkProvider);

		private static Gen<OutputDescriptor> PKHOutputDescriptorGen() =>
			from pkProvider in PubKeyProviderGen()
			select OutputDescriptor.NewPKH(pkProvider);

		private static Gen<OutputDescriptor> WPKHOutputDescriptorGen() =>
			from pkProvider in PubKeyProviderGen()
			select OutputDescriptor.NewWPKH(pkProvider);

		private static Gen<OutputDescriptor> ComboOutputDescriptorGen() =>
			from pkProvider in PubKeyProviderGen()
			select OutputDescriptor.NewCombo(pkProvider);

		private static Gen<OutputDescriptor> MultisigOutputDescriptorGen(int maxN) =>
			from n in Gen.Choose(2, maxN)
			from m in Gen.Choose(2, n).Select(i => (uint)i)
			from pkProviders in Gen.ArrayOf(n, PubKeyProviderGen())
			select OutputDescriptor.NewMulti(m, pkProviders);

		private static Gen<OutputDescriptor> WSHInnerGen(int maxMultisigN) =>
			Gen.OneOf(
				PKOutputDescriptorGen(),
				PKHOutputDescriptorGen(),
				MultisigOutputDescriptorGen(maxMultisigN)
				);
		private static Gen<OutputDescriptor> InnerOutputDescriptorGen(int maxMultisigN) =>
			Gen.OneOf(
				WPKHOutputDescriptorGen(),
				WSHInnerGen(maxMultisigN)
				);

		// For sh-nested script, max multisig Number is 15.
		private static Gen<OutputDescriptor> SHOutputDescriptorGen() =>
			from inner in Gen.OneOf(InnerOutputDescriptorGen(15), WSHOutputDescriptorGen())
			select OutputDescriptor.NewSH(inner);

		private static Gen<OutputDescriptor> WSHOutputDescriptorGen() =>
			from inner in WSHInnerGen(20)
			select OutputDescriptor.NewWSH(inner);

		#region pubkey providers

		private static Gen<PubKeyProvider> PubKeyProviderGen() =>
			Gen.OneOf(OriginPubKeyProviderGen(), ConstPubKeyProviderGen(), HDPubKeyProviderGen());

		private static Gen<PubKeyProvider> OriginPubKeyProviderGen() =>
			from keyOrigin in CryptoGenerator.RootedKeyPath()
			from inner in Gen.OneOf(ConstPubKeyProviderGen(), HDPubKeyProviderGen())
			select PubKeyProvider.NewOrigin(keyOrigin, inner);

		private static Gen<PubKeyProvider> ConstPubKeyProviderGen() =>
			from pk in CryptoGenerator.PublicKey()
			select PubKeyProvider.NewConst(pk);

		private static Gen<PubKeyProvider> HDPubKeyProviderGen() =>
			from extPk in CryptoGenerator.BitcoinExtPubKey()
			from kp in CryptoGenerator.KeyPath()
			from t in Arb.Generate<PubKeyProvider.DeriveType>()
			select PubKeyProvider.NewHD(extPk, kp, t);

		# endregion
	}
}
