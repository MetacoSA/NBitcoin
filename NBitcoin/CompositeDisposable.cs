using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class CompositeDisposable : IDisposable
	{
		IDisposable[] _Disposables;
		public CompositeDisposable(params IDisposable[] disposables)
		{
			_Disposables = disposables;
		}
		#region IDisposable Members

		public void Dispose()
		{
			if(_Disposables != null)
				foreach(var dispo in _Disposables)
					dispo.Dispose();
		}

		#endregion
	}
}
