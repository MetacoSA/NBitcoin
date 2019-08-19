using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public class Payload : IBitcoinSerializable
	{
		public virtual string Command
		{
			get
			{
				return PayloadAttribute.GetCommandName(this.GetType());
			}
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
			return this.GetType().Name;
		}
	}
}
