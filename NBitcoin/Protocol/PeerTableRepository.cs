using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Protocol
{
	public abstract class PeerTableRepository
	{
		public abstract IEnumerable<Peer> GetPeers();
		public abstract void WritePeers(IEnumerable<Peer> peer);
		public void WritePeer(Peer peer)
		{
			WritePeers(new Peer[] { peer });
		}
	}

	public class SqLitePeerTableRepository : PeerTableRepository, IDisposable
	{
		[DataContract]
		class PeerBlob
		{
			public PeerBlob()
			{

			}
			public PeerBlob(Peer peer)
			{
				Origin = (byte)peer.Origin;
				Address = peer.NetworkAddress.Endpoint.Address.ToString();
				Port = peer.NetworkAddress.Endpoint.Port;
				LastSeen = peer.NetworkAddress.Time;
			}
			public Peer ToPeer()
			{
				return new Peer((PeerOrigin)Origin, new NetworkAddress()
				{
					Time = LastSeen,
					Endpoint = new IPEndPoint(IPAddress.Parse(Address), Port)
				});
			}
			[DataMember]
			public byte Origin
			{
				get;
				set;
			}
			[DataMember]
			public string Address
			{
				get;
				set;
			}
			[DataMember]
			public int Port
			{
				get;
				set;
			}
			[DataMember]
			public DateTimeOffset LastSeen
			{
				get;
				set;
			}
		}

		private readonly SQLiteConnection _Connection;
		public SqLitePeerTableRepository(string fileName)
		{
			SqLiteUtility.EnsureSqLiteInstalled();

			ValiditySpan = TimeSpan.FromDays(1.0);

			SQLiteConnectionStringBuilder builder = new SQLiteConnectionStringBuilder();
			builder.DataSource = fileName;

			if(!File.Exists(fileName))
			{
				SQLiteConnection.CreateFile(fileName);
				_Connection = new SQLiteConnection(builder.ToString());
				_Connection.Open();

				var command = _Connection.CreateCommand();
				command.CommandText = "Create Table PeerTable(LastSeen UNSIGNED INTEGER, Data TEXT)";
				command.ExecuteNonQuery();
			}
			else
			{
				_Connection = new SQLiteConnection(builder.ToString());
				_Connection.Open();
			}

		}

		public TimeSpan ValiditySpan
		{
			get;
			set;
		}

		public override IEnumerable<Peer> GetPeers()
		{
			var command = _Connection.CreateCommand();
			command.CommandText = "Delete from PeerTable Where LastSeen < @d";
			command.Parameters.Add("@d", DbType.UInt64).Value = Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow - ValiditySpan);
			command.ExecuteNonQuery();

			command = _Connection.CreateCommand();
			command.CommandText = "Select * from PeerTable";
			var reader = command.ExecuteReader();
			while(reader.Read())
			{
				yield return Utils.Deserialize<PeerBlob>((string)reader["Data"]).ToPeer();
			}
		}

		public override void WritePeers(IEnumerable<Peer> peers)
		{
			var command = _Connection.CreateCommand();
			StringBuilder builder = new StringBuilder();
			int i = 0;
			foreach(var peer in peers)
			{
				if(new TimeSpan(0, -30, 0) < peer.NetworkAddress.Ago && peer.NetworkAddress.Ago < ValiditySpan)
				{
					builder.AppendLine("Insert INTO PeerTable(LastSeen,Data) Values(@a" + i + ",@b" + i + ");");
					command.Parameters.Add("@a" + i, DbType.UInt64).Value = Utils.DateTimeToUnixTime(peer.NetworkAddress.Time);
					command.Parameters.Add("@b" + i, DbType.String).Value = Utils.Serialize(new PeerBlob(peer));
					i++;
				}
			}
			command.CommandText = builder.ToString();
			if(command.CommandText == "")
				return;
			command.ExecuteNonQuery();
		}

		#region IDisposable Members

		public void Dispose()
		{
			_Connection.Close();
		}

		#endregion
	}
}
