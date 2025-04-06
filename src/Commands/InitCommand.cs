using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

using Typin;
using Typin.Console;
using Typin.Attributes;
using Typin.Exceptions;

using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("init", Description = "Initializes a new vnbuild configuration file")]
    public sealed class InitCommand : ICommand
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        [CommandOption(name: "file", Description = "The path to the configuration file to create")]
        public string FilePath { get; set; } = ".vnbuild.json";

        [CommandOption(name: "overwrite", Description = "Overwrites the configuration file if it already exists")]
        public bool Overwrite { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            BuildConfig defaultConfig = new();

            string filePath = Path.GetFullPath(FilePath);

            if (File.Exists(filePath) && !Overwrite)
            {
                throw new CommandException("Configuration file already exists. Use --overwrite to overwrite the file", exitCode: 1);
            }

            using FileStream configFs = File.Create(filePath, bufferSize: 1024, FileOptions.None);
            await JsonSerializer.SerializeAsync(configFs, defaultConfig, _jsonOptions);

            console.Output.WithForegroundColor(
                ConsoleColor.Green,
                o => o.WriteLine("Created new vnbuild configuration file at {0}", filePath)
            );
        }
    }
}