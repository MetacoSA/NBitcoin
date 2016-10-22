using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class MarshalExtensions
    {
        public static byte[] StructureToArray<T>(T a_struct) where T : struct
        {
            int len = Marshal.SizeOf(typeof(T));
            byte[] arr = new byte[len];
            IntPtr ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(a_struct, ptr, true);
            Marshal.Copy(ptr, arr, 0, len);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        public static void ArrayToStruct<T>(byte[] a_bytes, T a_struct) where T : struct
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(a_struct, ptr, true);
            Marshal.Copy(a_bytes, 0, ptr, size);
            object obj = Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
        }

        public static byte[] StructurePtrToArray<T>(IntPtr a_struct) where T : struct
        {
            int len = Marshal.SizeOf(typeof(T));
            byte[] arr = new byte[len];
            Marshal.Copy(a_struct, arr, 0, len);
            return arr;
        }
    }
}