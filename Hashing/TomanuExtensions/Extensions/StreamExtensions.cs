using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class StreamExtensions
    {
        /// <summary>
        /// Get stream for resource.
        /// </summary>
        /// <param name="a_type"></param>
        /// <param name="a_res_name"></param>
        /// <param name="a_res_subfolder"></param>
        /// <returns></returns>
        public static Stream FromResource(Type a_type, string a_res_name, string a_res_subfolder = "")
        {
            if (a_res_subfolder != "")
                a_res_subfolder = "." + a_res_subfolder;

            string res = a_type.GetParentFullName() + a_res_subfolder + "." + a_res_name;
            return Assembly.GetAssembly(a_type).GetManifestResourceStream(res);
        }

        /// <summary>
        /// Read all bytes from stream.
        /// </summary>
        /// <param name="a_stream"></param>
        /// <returns></returns>
        public static byte[] ReadAll(this Stream a_stream)
        {
            byte[] res = new byte[a_stream.Length];
            a_stream.Read(res, 0, res.Length);
            return res;
        }

        public static Stream SeekToBegin(this Stream a_stream)
        {
            a_stream.Seek(0, SeekOrigin.Begin);
            return a_stream;
        }

        public static Stream SeekToEnd(this Stream a_stream)
        {
            a_stream.Seek(0, SeekOrigin.End);
            return a_stream;
        }
    }
}