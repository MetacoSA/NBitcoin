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
		public static Dogecoin Dogecoin { get; } = Dogecoin.Instance;
		public static Litecoin Litecoin { get; } = Litecoin.Instance;
		public static Feathercoin Feathercoin { get; } = Feathercoin.Instance;
		public static Viacoin Viacoin {get; } = Viacoin.Instance;
		public static Polis Polis { get; } = Polis.Instance;
		public static Monacoin Monacoin { get; } = Monacoin.Instance;
		public static Ufo Ufo { get; } = Ufo.Instance;
		public static Bitcoin Bitcoin { get; } = Bitcoin.Instance;

		public static Groestlcoin Groestlcoin { get; } = Groestlcoin.Instance;

		public static IEnumerable<INetworkSet> GetAll()
		{
			yield return Bitcoin;
			yield return Litecoin;
			yield return Feathercoin;
			yield return Viacoin;
			yield return Dogecoin;
			yield return BCash;
			yield return BGold;
			yield return Polis;
			yield return Monacoin;
			yield return Dash;
			yield return Ufo;
			yield return Groestlcoin;
		}
	}
}
