using System;
using System.IO;
using System.Linq;

using Serilog;
using Serilog.Core;

using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Constants
{

    internal static class Config
    {

        //relative local directores to the project root
        public const string BUILD_DIR_NAME = ".build";
        public const string LOG_DIR_NAME = ".build/log";
        public const string BUILD_CONFIG = "build.conf.json";
        public const string SCRATCH_DIR = ".build/scratch";
        public const string SUM_DIR = ".build/sums";
        public const string OUTPUT_DIR = ".build/output";
        public const string SLEET_DIR = ".build/feed";

        /// <summary>
        /// Gets the system wide <see cref="Logger"/> log writer instance
        /// </summary>
        public static Logger Log { get; } = GetLog();

        const string Template = "{Message:lj}{NewLine}{Exception}";

        private static Logger GetLog()
        {
            string[] args = Environment.GetCommandLineArgs();

            LoggerConfiguration conf = new();

            if (args.Contains("-v") || args.Contains("--verbose"))
            {
                //Check for verbose logging level
                conf.MinimumLevel.Verbose();
            }
            else if (args.Contains("-d") || args.Contains("--debug"))
            {
                //Check for debug
                conf.MinimumLevel.Debug();
            }
            else
            {
                //Default information level
                conf.MinimumLevel.Information();
            }

            //Create a console logger unless the silent flag is set
            if (!args.Contains("-s"))
            {
                conf.WriteTo.Console(outputTemplate: Template);
            }

            //Creat the new log file
            string logFilePath = Path.Combine(LOG_DIR_NAME, $"{DateTimeOffset.Now.ToUnixTimeSeconds()}-log.txt");

            //Setup the log file output
            conf.WriteTo.File(logFilePath, outputTemplate: Template);

            return conf.CreateLogger();
        }

        /// <summary>
        /// Cleans up old build log files, so that only 100 log files remain in the log directory
        /// </summary>
        public static void TrimLogs(IDirectoryIndex dirIndex, int maxLogs)
        {
            try
            {
                //Get all log files in the log directory and cleanup any files after the max log count
                FileInfo[] toDelete = dirIndex.LogDir.EnumerateFiles("*.txt", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(static f => f.LastWriteTimeUtc)
                    .Skip(maxLogs)
                    .ToArray();

                foreach (FileInfo file in toDelete)
                {
                    file.Delete();
                }

                Log.Debug("Cleaned {file} log files", toDelete.Length);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to cleanup log files");
            }
        }
    }
}