using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin
{
	public class SqLiteUtility
	{
		/// Return Type: HMODULE->HINSTANCE->HINSTANCE__*
		///lpLibFileName: LPCWSTR->WCHAR*
		[System.Runtime.InteropServices.DllImportAttribute("kernel32.dll", EntryPoint = "LoadLibraryW")]
		public static extern System.IntPtr LoadLibraryW([System.Runtime.InteropServices.InAttribute()] [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)] string lpLibFileName);

		[DllImport("kernel32", SetLastError = true)]
		static extern bool FreeLibrary(IntPtr hModule);

		


		static object SqliteInstallLock = new object();
		volatile static IntPtr ptr;


		/// <summary>
		/// Visual studio test runner process does not unload between testing session, keeping a lock on sql lite dll.
		/// I initially tried to Free the library on AppDomain unload, but it seems another component take a lock on the dll.
		/// The final solution was to change native dll Copy To Output to "Only if newer", so a rebuild don't need to remove the locked files
		/// I keep this uninstall in case someone find a solution to properly unload them.
		/// </summary>
		public static void UninstallSqLite()
		{
			lock(SqliteInstallLock)
			{
				if(ptr != IntPtr.Zero)
				{
					var r = FreeLibrary(ptr);
					ptr = IntPtr.Zero;
				}
			}
		}
		public static void EnsureSqLiteInstalled()
		{
			lock(SqliteInstallLock)
			{
				try
				{
					new SQLiteConnection();
				}
				catch(DllNotFoundException)
				{

					var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", ""));
					if(Environment.Is64BitProcess)
					{
						path = Path.Combine(path, "X64");
					}
					else
					{
						// X32
						path = Path.Combine(path, "X86");
					}
					path = Path.Combine(path, "SQLite.Interop.dll");
					ptr = LoadLibraryW(path);
				}
			}
		}
	}
}
