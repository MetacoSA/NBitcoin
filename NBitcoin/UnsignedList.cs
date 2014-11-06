using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class UnsignedList<T>: List<T>, IBitcoinSerializable
		where T: IBitcoinSerializable, new()
	{
		public UnsignedList()
		{
		}

		public UnsignedList(IEnumerable<T> collection)
			:base(collection)
		{
		}

		public UnsignedList(int capacity)
			:base(capacity)
		{
		}

		public T this[uint index]
		{
			get { return base[(int)index]; }
			set { base[(int)index] = value; }
		}

		public void ReadWrite(BitcoinStream stream)
		{
			var dataArray = this.ToArray();
			stream.ReadWrite(ref dataArray);
			if (!stream.Serializing)
			{
				this.Clear();
				this.AddRange(dataArray);
			}
		}
	}
}
