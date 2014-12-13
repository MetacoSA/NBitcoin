using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Security;

namespace NBitcoin
{
	public class AndroidRandom : IRandom
	{
		SecureRandom _Random;
		public AndroidRandom()
		{
			_Random = new SecureRandom();
		}
		#region IRandom Members

		public void GetBytes(byte[] output)
		{
			_Random.NextBytes(output);
		}

		#endregion
	}
	public partial class RandomUtils
	{

		static RandomUtils()
		{
			Random = new AndroidRandom();
		}

	}
}