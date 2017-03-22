using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace NBitcoin
{
	public abstract class NoSqlRepository
	{

		public Task PutAsync(string key, IBitcoinSerializable obj)
		{
			return PutBytes(key, obj == null ? null : obj.ToBytes());
		}

		public void Put(string key, IBitcoinSerializable obj)
		{
			try
			{
				PutAsync(key, obj).Wait();
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
			}
		}

		public async Task<T> GetAsync<T>(string key) where T : IBitcoinSerializable, new()
		{
			var data = await GetBytes(key).ConfigureAwait(false);
			if(data == null)
				return default(T);
			T obj = new T();
			obj.ReadWrite(data);
			return obj;
		}

		public T Get<T>(string key) where T : IBitcoinSerializable, new()
		{
			try
			{
				return GetAsync<T>(key).Result;
			}
			catch(AggregateException aex)
			{
				ExceptionDispatchInfo.Capture(aex.InnerException).Throw();
				return default(T);
			}
		}

		public virtual Task PutBatch(IEnumerable<Tuple<string, IBitcoinSerializable>> values)
		{
			return PutBytesBatch(values.Select(s => new Tuple<string, byte[]>(s.Item1, s.Item2 == null ? null : s.Item2.ToBytes())));
		}

		protected abstract Task PutBytesBatch(IEnumerable<Tuple<string, byte[]>> enumerable);
		protected abstract Task<byte[]> GetBytes(string key);

		protected virtual Task PutBytes(string key, byte[] data)
		{
			return PutBytesBatch(new[] { new Tuple<string, byte[]>(key, data) });
		}
	}
}
