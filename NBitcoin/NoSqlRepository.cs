using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public abstract class NoSqlRepository
	{
		public void Put(string key, IBitcoinSerializable obj)
		{
			PutBytes(key, obj == null ? null : obj.ToBytes());
		}

		public T Get<T>(string key) where T : IBitcoinSerializable, new()
		{
			var data = GetBytes(key);
			if(data == null)
				return default(T);
			T obj = new T();
			obj.ReadWrite(data);
			return obj;
		}

		protected abstract byte[] GetBytes(string key);

		protected abstract void PutBytes(string key, byte[] data);
	}

	public class SQLiteNoSqlRepository : NoSqlRepository
	{
		private readonly SQLiteConnection _Connection;
		public SQLiteNoSqlRepository(string fileName)
		{
			SqLiteUtility.EnsureSqLiteInstalled();

			SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder();
			builder.DataSource = fileName;

			if(!File.Exists(fileName))
			{
				SQLiteConnection.CreateFile(fileName);
				_Connection = new SQLiteConnection(builder.ToString());
				_Connection.Open();

				var command = _Connection.CreateCommand();
				command.CommandText = "Create Table Store(Key TEXT UNIQUE, Data BLOB)";
				command.ExecuteNonQuery();
			}
			else
			{
				_Connection = new SQLiteConnection(builder.ToString());
				_Connection.Open();
			}
		}
		protected override byte[] GetBytes(string key)
		{
			var command = _Connection.CreateCommand();
			command.CommandText = "SELECT Data FROM Store WHERE Key = @key";
			command.Parameters.Add("@key", DbType.String).Value = key;
			using(var reader = command.ExecuteReader())
			{
				if(!reader.Read())
					return null;
				return GetBytes(reader);
			}
		}
		static byte[] GetBytes(SQLiteDataReader reader)
		{
			const int CHUNK_SIZE = 2 * 1024;
			byte[] buffer = new byte[CHUNK_SIZE];
			long bytesRead;
			long fieldOffset = 0;
			using(MemoryStream stream = new MemoryStream())
			{
				while((bytesRead = reader.GetBytes(0, fieldOffset, buffer, 0, buffer.Length)) > 0)
				{
					stream.Write(buffer, 0, (int)bytesRead);
					fieldOffset += bytesRead;
				}
				return stream.ToArray();
			}
		}

		protected override void PutBytes(string key, byte[] data)
		{
			if(data != null)
			{
				var command = _Connection.CreateCommand();
				command.CommandText = "INSERT OR REPLACE INTO Store (Key,Data) VALUES (@key,@data)";
				command.Parameters.Add("@key", DbType.String).Value = key;
				command.Parameters.Add("@data", DbType.Binary).Value = data;
				command.ExecuteNonQuery();
			}
			else
			{
				var command = _Connection.CreateCommand();
				command.CommandText = "Delete from Store where Key=@key";
				command.Parameters.Add("@key", DbType.String).Value = key;
				command.ExecuteNonQuery();
			}
		}
	}
}
