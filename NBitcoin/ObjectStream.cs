using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public abstract class ObjectStream<T> : IDisposable where T : class, IBitcoinSerializable, new()
	{
		public IEnumerable<T> Enumerate()
		{
			T o = null;
			while((o = ReadNext()) != null)
			{
				yield return o;
			}
		}

		public T ReadNext()
		{
			var result = ReadNextCore();
			Position++;
			if(result == null)
			{
				if(!EOF)
					throw new InvalidOperationException("EOF should be true if there is no object left in the stream");
			}
			return result;
		}

		public void WriteNext(T obj)
		{
			if(obj == null)
				throw new ArgumentNullException("obj");
			if(!EOF)
				throw new InvalidOperationException("EOF should be true before writing more");
			WriteNextCore(obj);
			Position++;
		}

		public void Rewind()
		{
			RewindCore();
			Position = 0;
		}

		protected abstract void RewindCore();
		protected abstract void WriteNextCore(T obj);
		protected abstract T ReadNextCore();

		public abstract bool EOF
		{
			get;
		}

		#region IDisposable Members

		public virtual void Dispose()
		{

		}

		#endregion

		public int Position
		{
			get;
			private set;
		}

		public void GoTo(int position)
		{
			if(position == Position)
				return;
			if(position == 0)
			{
				Rewind();
				return;
			}
			GoToCore(position);
		}

		protected virtual void GoToCore(int position)
		{
			if(Position < position)
			{
				while(Position < position)
				{
					var obj = ReadNext();
					if(obj == null)
						throw new IndexOutOfRangeException();
				}
			}
			else if(Position > position)
			{
				Rewind();
				GoTo(position);
			}
		}

		public void WriteNext(ObjectStream<T> stream)
		{
			foreach(var o in stream.Enumerate())
			{
				WriteNext(o);
			}
		}
	}
}
