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

		public CachedNoSqlRepository(NoSqlRepository inner)
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

		protected override void PutBytesBatch(IEnumerable<Tuple<string, byte[]>> enumerable)
		{
			foreach(var data in enumerable)
			{
				if(data.Item2 == null)
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

		protected override byte[] GetBytes(string key)
		{
			byte[] result = null;
			if(!_Table.TryGetValue(key, out result))
			{
				var raw = InnerRepository.Get<Raw>(key);
				if(raw != null)
				{
					result = raw.Data;
					_Table.Add(key, raw.Data);
				}
			}
			return result;
		}

		public void Flush()
		{
			InnerRepository
				.PutBatch(_Removed.Select(k => Tuple.Create<string, IBitcoinSerializable>(k, null))
						  .Concat(_Added.Select(k => Tuple.Create<string, IBitcoinSerializable>(k, new Raw(_Table[k])))));
			_Removed.Clear();
			_Added.Clear();
			_Table.Clear();
		}
	}
}
