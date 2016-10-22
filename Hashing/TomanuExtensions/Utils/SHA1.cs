using System;

namespace TomanuExtensions.Utils
{
    public static class SHA1
    {
        /// <summary>
        /// Calculate SHA1
        /// </summary>
        /// <param name="a_filePath"></param>
        /// <returns></returns>
        public static string Calculate(string a_filePath)
        {
            return Calculate(FileUtils.ReadFile(a_filePath));
        }

        /// <summary>
        /// Calculate SHA1
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string Calculate(byte[] a_data)
        {
            using (var hasher = System.Security.Cryptography.SHA1.Create())
            {
                byte[] hash = hasher.ComputeHash(a_data);
                return BitConverter.ToString(hash).ToUpper().Replace("-", "");
            }
        }
    }
}