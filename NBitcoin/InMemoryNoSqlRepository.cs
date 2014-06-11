using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class InMemoryNoSqlRepository : NoSqlRepository
	{
		Dictionary<string, byte[]> _Table = new Dictionary<string, byte[]>();

		protected override void PutBytesBatch(IEnumerable<Tuple<string, byte[]>> enumerable)
		{
			foreach(var data in enumerable)
			{
				if(data.Item2 == null)
				{
					_Table.Remove(data.Item1);
				}
				else
					_Table.AddOrReplace(data.Item1, data.Item2);
			}
		}

		protected override byte[] GetBytes(string key)
		{
			byte[] result = null;
			_Table.TryGetValue(key, out result);
			return result;
		}
	}
}
