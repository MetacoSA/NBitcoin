#if DEBUG && NODEFAULTRNG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public partial class RandomUtils
	{
		static RandomUtils()
		{
			Random = new UnsecureRandom();
		}
	}
}
#endif