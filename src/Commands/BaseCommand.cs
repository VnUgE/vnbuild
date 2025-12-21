using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

using Typin;
using Typin.Console;
using Typin.Attributes;
using Typin.Exceptions;

using Serilog;

using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Directories;
using VNLib.Tools.Build.Executor.Extensions;

namespace VNLib.Tools.Build.Executor.Commands
{

    public abstract class BaseCommand : ICommand
    {
        [CommandOption("config", Description = "Sets the build configuration file to use")]
        public string? ConfigFilePath { get; set; }

        [CommandOption("log-dir", Description = "Enables writing a copy of the log output to the desired directory. Should be set if using --silent option")]
        public string? LogDir { get; set; }

        [CommandOption("task-verbose", Description = "Enables verbose go-task output")]
        public bool TaskVerbose { get; set; } = false;

        //Allow users to specify build directory
        [CommandOption("build-dir", 'B', Description = "Sets the global build directory. Similar to CMake -B")]
        public string BuildDir { get; set; } = ".build";

        [CommandOption("source", 'S', Description = "Specifies the working directory to run vnbuild in. (Similar to cmake -S option)")]
        public string WorkingDirectory { get; set; } = ".";

        [CommandOption("force", 'f', Description = "Forces the operation even if steps are required")]
        public bool Force { get; set; }

        [CommandOption("confirm", 'c', Description = "Wait for user input before continuing")]
        public bool Confirm { get; set; }

        [CommandOption("include", 'i', Description = "Only use the specified modules, comma separated list")]
        public string? Modules { get; set; }

        [CommandOption("exclude", 'x', Description = "Ignores the specified modules, comma separated list")]
        public string? Exclude { get; set; }

        [CommandOption("verbose", 'v', Description = "Prints verbose output")]
        public bool Verbose { get; set; }

        [CommandOption("debug", 'd', Description = "Prints debug output")]
        public bool Debug { get; set; }

        [CommandOption("silent", 's', Description = "Disables console output")]
        public bool Silent { get; set; }
       

        public BuildConfig Config { get; private set; } = default!;


        /*
         * Base exec command does basic init and cleanup on startup
         */

        public virtual async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                Config = await ReadConfigOrDefault();
               
                BuildPipeline pipeline = new(Config);

                CancellationToken ct = console.GetCancellationToken();

                string[] modules = Modules?.Split(',') ?? [];
                string[] exclude = Exclude?.Split(',') ?? [];

                //Always load the pipeline
                await pipeline.LoadAsync(modules, exclude);

                if (Confirm)
                {
                    console.Output.WriteLine("---- Pipeline loaded. Press any key to continue ----");
                    await console.Input.ReadLineAsync(ct);

                    await Task.Delay(100, ct);
                    ct.ThrowIfCancellationRequested();
                }

                //Exec steps then exit
                await ExecStepsAsync(console, pipeline);
            }
            catch (OperationCanceledException)
            {               
                throw new CommandException("Operation cancelled", exitCode: 0);
            }
            catch(BuildFailedException be) when (be.InnerException is BuildFailedException bee)
            {
                console.WriteRed($"FATAL: Build step failed {bee.Message}");

                throw new CommandException(exitCode: 1);
            }
            catch(BuildFailedException be)
            {
                console.WriteRed($"FATAL: Build step failed {be.Message}");

                throw new CommandException(exitCode: 1);
            }
        }

        private async Task<BuildConfig> ReadConfigOrDefault()
        {
            BuildConfig conf;
            
            if(string.IsNullOrWhiteSpace(ConfigFilePath))
            {
                conf = new();
            }
            else
            {
                byte[] configData = await File.ReadAllBytesAsync(ConfigFilePath);
                conf = JsonSerializer.Deserialize<BuildConfig>(configData) ?? new();
            }

            //Override the config with the command line options
            conf.Force              = Force;
            conf.DryRun             = false;
            conf.BuildDirectory     = Path.GetFullPath(BuildDir);
            conf.Confirm            = Confirm;
            conf.WorkingDirectory   = Path.GetFullPath(WorkingDirectory);
            conf.TaskVerbose        = TaskVerbose;

            InitLog(conf);

            return conf;
        }

        private void InitLog(BuildConfig config)
        {
            GlobalDirIndex dirs = new(config);
            LoggerConfiguration conf = new();

            if (Verbose)
            {
                //Check for verbose logging level
                conf.MinimumLevel.Verbose();
            }
            else if (Debug)
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
            if (!Silent)
            {
                conf.WriteTo.Console(outputTemplate: config.LogTemplate);
            }

            //Enable file logging if a log directory is set
            if (!string.IsNullOrWhiteSpace(LogDir))
            {
                string logFilePath = Path.Combine(LogDir, $"vnbuild-{DateTimeOffset.Now.ToUnixTimeSeconds()}-log.txt");

                //Setup the log file output
                conf.WriteTo.File(logFilePath, outputTemplate: config.LogTemplate);
            }

            config.Log = conf.CreateLogger();
        }

        public abstract ValueTask ExecStepsAsync(IConsole console, BuildPipeline pipeline);
    }
}