using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class Payload : IBitcoinSerializable
	{
		public string Command
		{
			get
			{
				return PayloadAttribute.GetCommandName(this.GetType());
			}
		}

		#region IBitcoinSerializable Members

		public virtual void ReadWrite(BitcoinStream stream)
		{
			
		}

		#endregion
	}
}
