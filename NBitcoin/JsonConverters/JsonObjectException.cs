#if !NOJSONNET
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.JsonConverters
{
#if !NOJSONNET
	public
#else
	internal
#endif
	class JsonObjectException : Exception
	{
		public JsonObjectException(Exception inner, JsonReader reader)
			: base(inner.Message, inner)
		{
			Path = reader.Path;
		}
		public JsonObjectException(string message, JsonReader reader)
			: base(message)
		{
			Path = reader.Path;
		}
		public JsonObjectException(Exception inner, string path)
			: base(inner.Message, inner)
		{
			Path = path;
		}
		public JsonObjectException(string message, string path)
			: base(message)
		{
			Path = path;
		}

		public string Path
		{
			get;
			private set;
		}
	}
}
#endif
