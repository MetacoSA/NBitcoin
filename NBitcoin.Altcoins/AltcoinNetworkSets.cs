using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Altcoins
{
	public class AltNetworkSets
	{
		public static BCash BCash { get; } = BCash.Instance;
		public static BGold BGold { get; } = BGold.Instance;
		public static Dash Dash { get; } = Dash.Instance;
		public static Mogwai Mogwai { get; } = Mogwai.Instance;
		public static Dogecoin Dogecoin { get; } = Dogecoin.Instance;
		public static Dystem Dystem { get; } = Dystem.Instance;
		public static Litecoin Litecoin { get; } = Litecoin.Instance;
		public static Feathercoin Feathercoin { get; } = Feathercoin.Instance;
		public static Viacoin Viacoin {get; } = Viacoin.Instance;
		public static Polis Polis { get; } = Polis.Instance;
		public static Monacoin Monacoin { get; } = Monacoin.Instance;
		public static Ufo Ufo { get; } = Ufo.Instance;
		public static Bitcoin Bitcoin { get; } = Bitcoin.Instance;
		public static Bitcore Bitcore { get; } = Bitcore.Instance;
		public static Groestlcoin Groestlcoin { get; } = Groestlcoin.Instance;
		public static Zclassic Zclassic { get; } = Zclassic.Instance;
		public static Colossus Colossus { get; } = Colossus.Instance;
		public static GoByte GoByte { get; } = GoByte.Instance;
		public static Liquid Liquid { get; } = Liquid.Instance;
		public static Koto Koto { get; } = Koto.Instance;
		public static Bitcoinplus Bitcoinplus { get; } = Bitcoinplus.Instance;
		public static Chaincoin Chaincoin { get; } = Chaincoin.Instance;
		public static Stratis Stratis { get; } = Stratis.Instance;
		public static Particl Particl { get; } = Particl.Instance;

		public static IEnumerable<INetworkSet> GetAll()
		{
			yield return Bitcoin;
			yield return Bitcore;
			yield return Litecoin;
			yield return Feathercoin;
			yield return Viacoin;
			yield return Dogecoin;
			yield return Dystem;
			yield return BCash;
			yield return BGold;
			yield return Polis;
			yield return Monacoin;
			yield return Dash;
			yield return Mogwai;
			yield return Ufo;
			yield return Groestlcoin;
			yield return Zclassic;
			yield return Colossus;
			yield return GoByte;
			yield return Stratis;
			yield return Liquid;
			yield return Koto;
			yield return Bitcoinplus;
			yield return Chaincoin;
			yield return Particl;
		}
	}
}
