using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class InMemoryNoSqlRepository : NoSqlRepository
	{
		public InMemoryNoSqlRepository(): base(Network.Main.Consensus.ConsensusFactory)
		{

		}
		public InMemoryNoSqlRepository(ConsensusFactory consensusFactory): base(consensusFactory)
		{

		}
		Dictionary<string, byte[]> _Table = new Dictionary<string, byte[]>();

		protected override Task PutBytesBatch(IEnumerable<Tuple<string, byte[]>> enumerable)
		{
			foreach (var data in enumerable)
			{
				if (data.Item2 == null)
				{
					_Table.Remove(data.Item1);
				}
				else
					_Table.AddOrReplace(data.Item1, data.Item2);
			}
			return Task.FromResult(true);
		}

		protected override Task<byte[]> GetBytes(string key)
		{
			byte[] result = null;
			_Table.TryGetValue(key, out result);
			return Task.FromResult(result);
		}
	}
}
