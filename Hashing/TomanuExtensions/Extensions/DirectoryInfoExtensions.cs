using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TomanuExtensions
{
    [DebuggerStepThrough]
    public static class DirectoryInfoExtensions
    {
        public static void DeleteAll(this DirectoryInfo a_dir_info)
        {
            if (!a_dir_info.Exists)
                return;

            foreach (FileInfo file_info in a_dir_info.GetFiles())
                file_info.Delete();

            foreach (DirectoryInfo dir_info in a_dir_info.GetDirectories())
                dir_info.DeleteAll();

            a_dir_info.Delete(false);
        }

        public static void DeleteContent(this DirectoryInfo a_dir_info)
        {
            if (!a_dir_info.Exists)
                return;

            foreach (FileInfo file_info in a_dir_info.GetFiles())
                file_info.Delete();

            foreach (DirectoryInfo dir_info in a_dir_info.GetDirectories())
                dir_info.DeleteAll();
        }

        public static void CreateOrEmpty(this DirectoryInfo a_dir_info)
        {
            a_dir_info.DeleteContent();
            a_dir_info.Create();
        }

        public static FileInfo[] FindFilesRecursive(this DirectoryInfo a_dir_info, string a_pattern)
        {
            var foundFiles = new List<FileInfo>();
            FindFilesRecursive(a_dir_info, a_pattern, foundFiles);
            return foundFiles.ToArray();
        }

        private static void FindFilesRecursive(DirectoryInfo a_dir_info, string a_pattern,
            List<FileInfo> a_files)
        {
            a_files.AddRange(a_dir_info.GetFiles(a_pattern));

            foreach (var dir in a_dir_info.GetDirectories())
                FindFilesRecursive(dir, a_pattern, a_files);
        }

        public static string FindExistingDirectory(this DirectoryInfo a_di)
        {
            string str = a_di.FullName;

            for (; ; )
            {
                DirectoryInfo di = new DirectoryInfo(str);

                if (di.Exists)
                    return str;

                if (di.Parent == null)
                    return "";

                str = di.Parent.FullName;
            }
        }

        public static DirectoryInfo Append(this DirectoryInfo a_di, string a_dir)
        {
            return new DirectoryInfo(a_di.FullName + Path.DirectorySeparatorChar + a_dir);
        }
    }
}