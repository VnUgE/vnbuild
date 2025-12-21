using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Typin;
using Typin.Attributes;
using Typin.Console;
using Typin.Exceptions;

namespace VNLib.Tools.Build.Executor.Dependencies
{
    [Command("deps install", Description = "Install dependnecies from a manifest file")]
    public class DepsInstallCommand : ICommand
    {
        /// <summary>
        /// Gets or sets a value indicating whether to display progress information during 
        /// command execution.
        /// </summary>
        [CommandOption("show-progress", Description = "Enables download progress meters when downloading dependencies. May cause slowdowns when used with parallel")]
        public bool ShowProgress { get; set; }

        /// <summary>
        /// A flag that disables running pre/post install commands
        /// </summary>
        [CommandOption("no-scripts", Description = "Disables running optional pre/post install scripts")]
        public bool NoScripts { get; set; }

        /// <summary>
        /// Specifes the path to the manifest file used to manage the dependencies for the operation
        /// </summary>
        [CommandOption("file", 'f', Description = "The dependency manifest file path")]
        public string ManifestFilePath { get; set; } = "deps.json";

        /// <summary>
        /// The working directory for the application
        /// </summary>
        [CommandOption("work-dir", 'w', Description = "Sets the working directory for the operation")]
        public string WorkDir { get; set; } = Directory.GetCurrentDirectory();      

        /// <summary>
        /// A flag that allows for downloading/installing dependnecies in parallel
        /// </summary>
        [CommandOption("parallel", 'p', Description = "Allows downloading/installing dependencies in parallel")]
        public bool Parallel { get; set; }

        /// <summary>
        /// A flag that indicates that all operations must succeed otherwise all operations must be 
        /// cleaned up/restored.
        /// </summary>
        [CommandOption("atomic", 'a', Description = "Ensures that ensures all operations succeed or none")]
        public bool Atomic { get; set; }     


        public virtual async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                console.Output.WriteLine("Loading manifest {0} with working dir {1}", ManifestFilePath, WorkDir);
                
                // Move the entire process to the desired working directory
                Directory.SetCurrentDirectory(WorkDir);

                // Load the manifest json
                DepsManifestJson deps = await LoadManifestFile(ManifestFilePath, console.GetCancellationToken());

            }
            catch (OperationCanceledException)
            {
                throw new CommandException("Operation cancelled", exitCode: 0);
            }           
        }

        internal static async Task<DepsManifestJson> LoadManifestFile(string manifestPath, CancellationToken ct)
        {
            try
            {

                FileInfo manifestFile = new(manifestPath);
                if (!manifestFile.Exists)
                {
                    throw new FileNotFoundException($"File {manifestFile.FullName} does not exist");
                }

                // Loosen up the requirment
                JsonSerializerOptions ops = new()
                {
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                };

                // Open manifest file 
                using FileStream manifestFileStream = manifestFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

                // Attempt to deserialize the manifest
                DepsManifestJson? deps = await JsonSerializer.DeserializeAsync<DepsManifestJson>(manifestFileStream, ops, ct) 
                    ?? throw new JsonException("Manifest file was empty, failed to read manifest data");

                return deps;
            }
            catch (Exception ex)
            {
                throw new CommandException("Failed to load manifest file", ex, exitCode: -2);
            }
        }

        internal static void ValidateManifestJson(DepsManifestJson deps)
        {

        }
    }
}