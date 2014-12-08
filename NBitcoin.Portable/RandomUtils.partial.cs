using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public partial class RandomUtils
	{
#if USEBC && DEBUG
		static RandomUtils()
		{
			Random = new UnsecureRandom();
		}
#endif
	}
}
