using System.Diagnostics;
using System.IO;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class FileInfoExtensions
    {
        public static void Rename(this FileInfo a_file_info, string a_file_name)
        {
            var filePath = Path.Combine(Path.GetDirectoryName(a_file_info.FullName), a_file_name);
            a_file_info.MoveTo(filePath);
        }

        public static void RenameFileWithoutExtension(this FileInfo a_file_info, string a_file_name)
        {
            var fileName = string.Concat(a_file_name, a_file_info.Extension);
            a_file_info.Rename(fileName);
        }

        public static void ChangeExtension(this FileInfo a_file_info, string a_file_ext)
        {
            a_file_ext = a_file_ext.EnsureStartsWith(".");
            var fileName = string.Concat(Path.GetFileNameWithoutExtension(a_file_info.FullName), a_file_ext);
            a_file_info.Rename(fileName);
        }
    }
}