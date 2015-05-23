using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	/// <summary>
	/// Represent any type which represent an underlying ScriptPubKey
	/// </summary>
	public interface IDestination
	{
		Script ScriptPubKey
		{
			get;
		}
	}
}
