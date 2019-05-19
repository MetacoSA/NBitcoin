using FsCheck;
using NBitcoin.Scripting;

namespace NBitcoin.Tests.Generators
{
	public class OutputDescriptorGenerator
	{
		public static Arbitrary<OutputDescriptor> OutputDescriptorArb() =>
			Arb.From(OutputDescriptorGen());

		public static Gen<OutputDescriptor> OutputDescriptorGen() =>
			Gen.OneOf(
				AddrOutputDescriptorGen(),
				RawOutputDescriptorGen(),
				PKOutputDescriptorGen(),
				PKHOutputDescriptorGen(),
				WPKHOutputDescriptorGen(),
				ComboOutputDescriptorGen(),
				MultisigOutputDescriptorGen(),
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

		private static Gen<OutputDescriptor> MultisigOutputDescriptorGen() =>
			from m in Gen.Choose(1, 20).Select(i => (uint)i)
			from pkProviders in Gen.NonEmptyListOf(PubKeyProviderGen())
			select OutputDescriptor.NewMulti(m, pkProviders);

		private static Gen<OutputDescriptor> InnerOutputDescriptorGen() =>
			Gen.OneOf(
				PKOutputDescriptorGen(),
				PKHOutputDescriptorGen(),
				WPKHOutputDescriptorGen(),
				MultisigOutputDescriptorGen()
				);
		private static Gen<OutputDescriptor> SHOutputDescriptorGen() =>
			from inner in Gen.OneOf(InnerOutputDescriptorGen(), WSHOutputDescriptorGen())
			select OutputDescriptor.NewSH(inner);

		private static Gen<OutputDescriptor> WSHOutputDescriptorGen() =>
			from inner in InnerOutputDescriptorGen()
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
