using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Tests
{
	[JsonArray]
	public class TestCase : List<object>
	{
		public T GetValue<T>(int i)
		{
			return (T)Convert.ChangeType(this[i], typeof(T));
		}

		public dynamic GetDynamic(int i)
		{
			return this[i];
		}

		public static TestCase[] read_json(string fileName)
		{
			using(var fs = File.Open(fileName, FileMode.Open))
			{
				Newtonsoft.Json.JsonSerializer seria = new Newtonsoft.Json.JsonSerializer();
				var result = (TestCase[])seria.Deserialize(new StreamReader(fs), typeof(TestCase[]));
				for(int i = 0; i < result.Length; i++)
				{
					result[i].Index = i;
				}
				return result;
			}
		}

		public override string ToString()
		{
			return "[" + String.Join(",", this.Select(s => Convert.ToString(s)).ToArray()) + "]";
		}

		public int Index
		{
			get;
			set;
		}
	}
}
