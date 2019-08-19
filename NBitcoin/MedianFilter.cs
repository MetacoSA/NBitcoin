
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class MedianFilterInt32
	{
		Queue<Int32> vValues;
		Queue<Int32> vSorted;
		uint nSize;

		public MedianFilterInt32(uint size, Int32 initialValue)
		{
			nSize = size;
			vValues = new Queue<Int32>((int)size);
			vValues.Enqueue(initialValue);
			vSorted = new Queue<Int32>(vValues);
		}

		public Int32 Median
		{
			get
			{
				int size = vSorted.Count;
				if (size <= 0)
					throw new InvalidOperationException("size <= 0");

				var sortedList = vSorted.ToList();
				if (size % 2 == 1)
				{
					return sortedList[size / 2];
				}
				else // Even number of elements
				{
					return (sortedList[size / 2 - 1] + sortedList[size / 2]) / 2;
				}
			}
		}

		public void Input(Int32 value)
		{
			if (vValues.Count == nSize)
			{
				vValues.Dequeue();
			}
			vValues.Enqueue(value);
			vSorted = new Queue<Int32>(vValues.OrderBy(o => o));
		}
	}
	public class MedianFilterInt64
	{
		Queue<Int64> vValues;
		Queue<Int64> vSorted;
		uint nSize;

		public MedianFilterInt64(uint size, Int64 initialValue)
		{
			nSize = size;
			vValues = new Queue<Int64>((int)size);
			vValues.Enqueue(initialValue);
			vSorted = new Queue<Int64>(vValues);
		}

		public Int64 Median
		{
			get
			{
				int size = vSorted.Count;
				if (size <= 0)
					throw new InvalidOperationException("size <= 0");

				var sortedList = vSorted.ToList();
				if (size % 2 == 1)
				{
					return sortedList[size / 2];
				}
				else // Even number of elements
				{
					return (sortedList[size / 2 - 1] + sortedList[size / 2]) / 2;
				}
			}
		}

		public void Input(Int64 value)
		{
			if (vValues.Count == nSize)
			{
				vValues.Dequeue();
			}
			vValues.Enqueue(value);
			vSorted = new Queue<Int64>(vValues.OrderBy(o => o));
		}
	}
}