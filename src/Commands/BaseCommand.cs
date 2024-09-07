using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Typin;
using Typin.Console;
using Typin.Attributes;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;
using static VNLib.Tools.Build.Executor.Constants.Config;

namespace VNLib.Tools.Build.Executor.Commands
{
    public abstract class BaseCommand(BuildPipeline pipeline, ConfigManager bm) : ICommand
    {
        [CommandOption("verbose", 'v', Description = "Prints verbose output")]
        public bool Verbose { get; set; }

        [CommandOption("force", 'f', Description = "Forces the operation even if steps are required")]
        public bool Force { get; set; }

        [CommandOption("include", 'i', Description = "Only use the specified modules, comma separated list")]
        public string? Modules { get; set; }

        [CommandOption("exclude", 'x', Description = "Ignores the specified modules, comma separated list")]
        public string? Exclude { get; set; }

        [CommandOption("confirm", 'c', Description = "Wait for user input before continuing")]
        public bool Confirm { get; set; }

        //Allow users to specify build directory
        [CommandOption("build-dir", 'B', Description = "Sets the base build directory to execute operations in")]
        public string BuildDir { get; set; } = Directory.GetCurrentDirectory();

        [CommandOption("max-logs", 'L', Description = "Sets the maximum number of logs to keep")]
        public int MaxLogs { get; set; } = 50;

        public IDirectoryIndex BuildIndex { get; private set; } = default!;

        public BuildConfig Config { get; private set; } = default!;


        /*
         * Base exec command does basic init and cleanup on startup
         */

        public virtual async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                CancellationToken ct = console.GetCancellationToken();

                //Init build index
                BuildIndex = GetIndex();
                Config = await bm.GetOrCreateConfig(BuildIndex, false);

                //Cleanup log files on init
                TrimLogs(BuildIndex, MaxLogs);

                string[] modules = Modules?.Split(',') ?? [];
                string[] exclude = Exclude?.Split(',') ?? [];

                //Always load the pipeline
                await pipeline.LoadAsync(Config, modules, exclude, Feeds);

                if (Confirm)
                {
                    console.Output.WriteLine("---- Pipeline loaded. Press any key to continue ----");
                    await console.Input.ReadLineAsync(ct);

                    await Task.Delay(100, ct);
                    ct.ThrowIfCancellationRequested();
                }

                //Exec steps then exit
                await ExecStepsAsync(console);
            }
            catch (OperationCanceledException)
            {
                console.WithForegroundColor(
                    ConsoleColor.Red, 
                    static o => o.Error.WriteLine("Operation cancelled")
                );
            }
            catch(BuildFailedException be)
            {
                console.WithForegroundColor(
                    ConsoleColor.Red, 
                    o => o.Error.WriteLine("FATAL: Build step failed {0}", be.Message)
                );
            }
        }

        public abstract ValueTask ExecStepsAsync(IConsole console);

        public abstract IFeedManager[] Feeds { get; }

        private Dirs GetIndex() => new()
        {
            BaseDir = new(BuildDir),
            BuildDir = BuildDirs.GetOrCreateDir(BUILD_DIR_NAME),
            LogDir = BuildDirs.GetOrCreateDir(LOG_DIR_NAME),
            ScratchDir = BuildDirs.GetOrCreateDir(SCRATCH_DIR),
            SumDir = BuildDirs.GetOrCreateDir(SUM_DIR),
            OutputDir = BuildDirs.GetOrCreateDir(OUTPUT_DIR)
        };

#nullable disable
        protected sealed class Dirs : IDirectoryIndex
        {
            public DirectoryInfo BaseDir { get; set; }
            public DirectoryInfo BuildDir { get; set; }
            public DirectoryInfo LogDir { get; set; }
            public DirectoryInfo ScratchDir { get; set; }
            public DirectoryInfo SumDir { get; set; }
            public DirectoryInfo OutputDir { get; set; }
        }
#nullable enable
    }
}