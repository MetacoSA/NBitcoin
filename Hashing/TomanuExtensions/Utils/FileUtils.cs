using System;
using System.IO;

namespace TomanuExtensions.Utils
{
    public static class FileUtils
    {
        public static bool IsFilePathValid(string a_path)
        {
            if (String.IsNullOrEmpty(a_path.Trim()))
            {
                return false;
            }

            string pathname;
            string filename;

            try
            {
                pathname = Path.GetPathRoot(a_path);
                filename = Path.GetFileName(a_path);
            }
            catch (ArgumentException)
            {
                // GetPathRoot() and GetFileName() above will throw exceptions
                // if pathname/filename could not be parsed.

                return false;
            }

            // Make sure the filename part was actually specified
            if (String.IsNullOrEmpty(filename.Trim()))
            {
                return false;
            }

            // Not sure if additional checking below is needed, but no harm done
            if (pathname.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                return false;
            }

            if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            return true;
        }

        public static byte[] ReadFile(string a_path)
        {
            return File.ReadAllBytes(a_path);
        }
    }
}