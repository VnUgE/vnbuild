using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Typin;
using Typin.Attributes;
using Typin.Console;
using Typin.Exceptions;

using VNLib.Tools.Build.Executor.Extensions;

namespace VNLib.Tools.Build.Executor.Dependencies
{

    [Command("deps add", Description = "Adds the desired dependency to the manifest file and nothing else")]
    public class DepsAddCommand : ICommand
    {
        [CommandParameter(1, Description = "The source url to download the package from")]
        public string? SourceUrl { get; set; }

        [CommandParameter(2, Description = "The destination path for the installed package")]
        public string? DestPath { get; set; }

        [CommandOption("checksum", Description = "Specifies an optional checksum for the dependency")]
        public string? Checksum { get; set; }

        [CommandOption("insecure", Description = "If vnbuild should allow insecure server paths when downloading")]
        public bool Insecure { get; set; }

        [CommandOption("post-install", Description = "An optional post-install command to run")]
        public string? PostInstallCommand { get; set; }

        [CommandOption("pre-install", Description = "An optional pre-install command to run")]
        public string? PreInstallCommand { get; set; }

        [CommandOption("overwrite", Description = "Allows overwriting an existing dependency")]
        public bool Overwrite { get; set; }

        /// <summary>
        /// Specifes the path to the manifest file used to manage the dependencies for the operation
        /// </summary>
        [CommandOption("file", 'f', Description = "The dependency manifest file path")]
        public string ManifestFilePath { get; set; } = "deps.json";

        public virtual async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                // Convert to full file path
                ManifestFilePath = Path.GetFullPath(ManifestFilePath);

                console.Output.WriteLine("Adding {0} to manifest", SourceUrl);

                DepsManifestJson deps;

                if (File.Exists(ManifestFilePath))
                {
                    // Load the manifest json
                    deps = await DepsInstallCommand.LoadManifestFile(
                        ManifestFilePath,
                        console.GetCancellationToken()
                    );
                }
                else
                {
                    deps = new();
                }

                DepsInstallCommand.ValidateManifestJson(deps);

                // Add the package to the existing manifest
                AddPackageToManifest(deps);
                console.Output.WriteLine("Added package to manifest");

                console.Output.WriteLine("Writing manifest file to {0}", ManifestFilePath);

                await WriteManifestAsync(deps);

                console.WriteGreen("Successfully added dependency");
            }
            catch (OperationCanceledException)
            {
                throw new CommandException("Operation cancelled", exitCode: 0);
            }
        }

        private void AddPackageToManifest(DepsManifestJson deps)
        {
            DependencyJson newDep = new()
            {
                Source              = SourceUrl!,
                Destination         = DestPath!,
                Sum                 = Checksum,
                Insecure            = Insecure,
                PostInstallCommand  = PostInstallCommand,
                PreInstallCommand   = PreInstallCommand
            };

            bool exists = deps.Dependencies
                .Any(s => string.Equals(s.Source, newDep.Source, StringComparison.OrdinalIgnoreCase));

            // guard overwriting
            if (exists && !Overwrite)
            {
                throw new CommandException("Dependency already exists");
            }

            deps.Dependencies = [.. deps.Dependencies, newDep];
        }

        private async Task WriteManifestAsync(DepsManifestJson deps)
        {
            FileInfo manifest = new(ManifestFilePath);

            // Open new file and allow for creating a new file or overwriting existing file
            using FileStream manifestFile = manifest.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            JsonSerializerOptions opts = new()
            {
                AllowTrailingCommas     = true,
                WriteIndented           = true  //Enable indentation for readability
            };

            await JsonSerializer.SerializeAsync(manifestFile, deps, opts);
        }
    }
}