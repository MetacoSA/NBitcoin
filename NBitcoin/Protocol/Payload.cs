using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	/// <summary>
	/// A P2P Bitcoin payload
	/// </summary>
	public abstract class Payload : IBitcoinSerializable
	{
		public abstract string Command
		{
			get;
		}

		#region IBitcoinSerializable Members

		public void ReadWrite(BitcoinStream stream)
		{
			using (stream.SerializationTypeScope(SerializationType.Network))
			{
				ReadWriteCore(stream);
			}
		}
		public virtual void ReadWriteCore(BitcoinStream stream)
		{

		}

		#endregion

		public override string ToString()
		{
			return Command;
		}
	}
}
