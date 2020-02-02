using NBitcoin.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class CachedNoSqlRepository : NoSqlRepository
	{
		class Raw : IBitcoinSerializable
		{
			public Raw()
			{

			}
			public Raw(byte[] data)
			{
				var str = new VarString();
				str.FromBytes(data);
				_Data = str.GetString(true);
			}
			private byte[] _Data = new byte[0];
			public byte[] Data
			{
				get
				{
					return _Data;
				}
			}
			#region IBitcoinSerializable Members

			public void ReadWrite(BitcoinStream stream)
			{
				stream.ReadWriteAsVarString(ref _Data);
			}

			#endregion
		}

		public CachedNoSqlRepository(NoSqlRepository inner): base(Network.Main.Consensus.ConsensusFactory)
		{
			_InnerRepository = inner;
		}
		private readonly NoSqlRepository _InnerRepository;
		public NoSqlRepository InnerRepository
		{
			get
			{
				return _InnerRepository;
			}
		}
		Dictionary<string, byte[]> _Table = new Dictionary<string, byte[]>();
		HashSet<string> _Removed = new HashSet<string>();
		HashSet<string> _Added = new HashSet<string>();
		ReaderWriterLock @lock = new ReaderWriterLock();

		public override async Task PutBatch(IEnumerable<Tuple<string, IBitcoinSerializable>> values)
		{
			await base.PutBatch(values).ConfigureAwait(false);
			await _InnerRepository.PutBatch(values).ConfigureAwait(false);
		}

		protected override Task PutBytesBatch(IEnumerable<Tuple<string, byte[]>> enumerable)
		{
			using (@lock.LockWrite())
			{
				foreach (var data in enumerable)
				{
					if (data.Item2 == null)
					{
						_Table.Remove(data.Item1);
						_Removed.Add(data.Item1);
						_Added.Remove(data.Item1);
					}
					else
					{
						_Table.AddOrReplace(data.Item1, data.Item2);
						_Removed.Remove(data.Item1);
						_Added.Add(data.Item1);
					}
				}
			}
			return Task.FromResult(true);
		}

		protected override async Task<byte[]> GetBytes(string key)
		{
			byte[] result = null;
			bool found;
			using (@lock.LockRead())
			{
				found = _Table.TryGetValue(key, out result);
			}
			if (!found)
			{
				var raw = await InnerRepository.GetAsync<Raw>(key).ConfigureAwait(false);
				if (raw != null)
				{
					result = raw.Data;
					using (@lock.LockWrite())
					{
						_Table.AddOrReplace(key, raw.Data);
					}
				}
			}
			return result;
		}

		public void Flush()
		{
			using (@lock.LockWrite())
			{
				InnerRepository
					.PutBatch(_Removed.Select(k => Tuple.Create<string, IBitcoinSerializable>(k, null))
							.Concat(_Added.Select(k => Tuple.Create<string, IBitcoinSerializable>(k, new Raw(_Table[k])))))
					.GetAwaiter().GetResult();
				_Removed.Clear();
				_Added.Clear();
				_Table.Clear();
			}
		}
	}
}
