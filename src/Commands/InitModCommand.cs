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
    [Command("init module", Description = "Initializes a new vnbuild configuration file")]
    public sealed class InitModCommand : ICommand
    {
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            AllowTrailingCommas = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        [CommandParameter(1, Description = "The path of the module to create the module config file in")]
        public string ModulePath { get; set; } = null!;

        [CommandOption(name: "file", Description = "The name of the configuration file")]
        public string FilePath { get; set; } = ".vnbuild-module.json";

        [CommandOption(name: "overwrite", Description = "Overwrites the configuration file if it already exists")]
        public bool Overwrite { get; set; }

        public async ValueTask ExecuteAsync(IConsole console)
        {
            ModuleConfig modConfig = new();

            string filePath = Path.Combine(ModulePath, FilePath);

            if (File.Exists(filePath) && !Overwrite)
            {
                throw new CommandException("Configuration file already exists. Use --overwrite to overwrite the file", exitCode: 1);
            }

            using FileStream configFs = File.Create(filePath, bufferSize: 1024, FileOptions.None);
            await JsonSerializer.SerializeAsync(configFs, modConfig, _jsonOptions);

            console.Output.WithForegroundColor(
                ConsoleColor.Green,
                o => o.WriteLine("Created new module configuration file at {0}", filePath)
            );
        }
    }
}