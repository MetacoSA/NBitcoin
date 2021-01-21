#if HAS_SPAN
#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Secp256k1
{

#if SECP256K1_LIB
	public
#endif
	enum ECMultiImplementation
	{
		/// <summary>
		/// Pick the optimum algorithm depending on the size of the batch
		/// </summary>
		Auto,
		Pippenger,
		Strauss,
		Simple
	}
#if SECP256K1_LIB
	public
#endif
	class MultBatchOptions
	{
		public MultBatchOptions()
		{

		}
		public MultBatchOptions(ECMultiImplementation implementation)
		{
			Implementation = implementation;
		}
		/// <summary>
		/// The number of scalars until the Auto implementation pick pippenger algorithm over strauss (Default: 88)
		/// </summary>
		public int PippengerThreshold { get; set; } = 88;
		/// <summary>
		/// The implementation to pick
		/// </summary>
		public ECMultiImplementation Implementation { get; set; } = ECMultiImplementation.Auto;
	}
}
#nullable restore
#endif
