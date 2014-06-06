using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class StreamObjectStream<T> : ObjectStream<T> where T : class, IBitcoinSerializable, new()
	{
		public StreamObjectStream(Stream stream)
		{
			if(stream == null)
				throw new ArgumentNullException("stream");
			_Stream = stream;
		}
		public StreamObjectStream():this(new MemoryStream())
		{

		}
		private readonly Stream _Stream;
		public Stream Stream
		{
			get
			{
				return _Stream;
			}
		}
		public override void Rewind()
		{
			_Stream.Position = 0;
		}

		protected override void WriteNextCore(T obj)
		{
			obj.ReadWrite(_Stream, true);
			_Stream.Flush();
		}

		protected override T ReadNextCore()
		{
			if(EOF)
				return null;
			var obj = new T();
			obj.ReadWrite(_Stream, false);
			return obj;
		}

		public override bool EOF
		{
			get
			{
				return _Stream.Position == _Stream.Length;
			}
		}

		public override void Dispose()
		{
			_Stream.Dispose();
		}
	}
}
