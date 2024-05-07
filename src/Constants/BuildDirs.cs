using System;
using System.IO;

namespace VNLib.Tools.Build.Executor.Constants
{
    internal static class BuildDirs
    {  

        private static DirectoryInfo GetProjectDir()
        {
            //See if dir was specified on command line
            string[] args = Environment.GetCommandLineArgs();

            //Get the build dir
            DirectoryInfo dir = new(args.Length > 1 && Directory.Exists(args[1]) ? args[1] : Directory.GetCurrentDirectory());

            if (!dir.Exists)
            {
                dir.Create();
            }
            return dir;
        }

        public static DirectoryInfo GetOrCreateDir(string @default, string? other = null)
        {
            //Get the scratch dir
            DirectoryInfo logDir = new(Path.Combine(GetProjectDir().FullName, other?? @default));
            if (!logDir.Exists)
            {
                logDir.Create();
            }
            return logDir;
        }
    }
}