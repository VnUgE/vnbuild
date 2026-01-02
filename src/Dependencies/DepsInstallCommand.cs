/*
* Copyright (c) 2025 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DepsInstallCommand.cs
*
* DepsInstallCommand.cs is part of vnbuild which is part of the larger 
* VNLib collection of libraries and utilities.
*
* vnbuild is free software: you can redistribute it and/or modify 
* it under the terms of the GNU General Public License as published
* by the Free Software Foundation, either version 2 of the License,
* or (at your option) any later version.
*
* vnbuild is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU 
* General Public License for more details.
*
* You should have received a copy of the GNU General Public License 
* along with vnbuild. If not, see http://www.gnu.org/licenses/.
*/

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Typin;
using Typin.Attributes;
using Typin.Console;
using Typin.Exceptions;

using FluentValidation;
using FluentValidation.Results;

using VNLib.Tools.Build.Executor.Dependencies.Config;
using VNLib.Tools.Build.Executor.Dependencies.Downloaders;
using VNLib.Tools.Build.Executor.Dependencies.Extractors;
using VNLib.Tools.Build.Executor.Dependencies.Validation;

namespace VNLib.Tools.Build.Executor.Dependencies
{
    [Command("deps install", Description = "Install dependencies from a manifest file")]
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
        /// Allows the user to set a temporary directory when installing packages
        /// </summary>
        [CommandOption("temp-dir", Description = "A file path to store temporary files to during the install process")]
        public string TempDir { get; set; } = Path.GetTempPath();

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

        /// <summary>
        /// Gets or sets a value indicating whether to display verbose logging output during
        /// </summary>
        [CommandOption("verbose", Description = "Enables verbose logging output")]
        public bool Verbose { get; set; }

        public virtual async ValueTask ExecuteAsync(IConsole console)
        {
            try
            {
                console.Output.WriteLine("Loading manifest {0} with working dir {1}", ManifestFilePath, WorkDir);

                string manifestPath = ResolvePath(ManifestFilePath, WorkDir);

                // Load the manifest json
                DepsManifestJson deps = await LoadManifestFile(manifestPath, console.GetCancellationToken());

                DepsManifestValidator validator = new();
                validator.ValidateAndThrowEx(deps, console);

                string workDir = ResolvePath(WorkDir, Directory.GetCurrentDirectory());

                DependencyInstaller installer = new(
                    console,
                    new CurlDependencyDownloader(workDir), 
                    [ new TarDependencyExtractor(), new GunzipDependencyExtractor(), new PowershellZipExtractor() ]
                );

                DependencyInstallOptions opts = new(
                    WorkingDirectory: workDir,
                    TempDirectory: TempDir,
                    Atomic: Atomic,
                    NoScripts: NoScripts,
                    Parallel: Parallel,
                    ShowProgress: ShowProgress,
                    Verbose: Verbose
                );

                await installer.InstallAsync(deps, opts, console.GetCancellationToken());
            }
            catch (ValidationException vex)
            {
                console.Error.WriteLine("Manifest validation failed:");
                foreach (ValidationFailure failure in vex.Errors)
                {
                    console.Error.WriteLine(" - {0}: {1}", failure.PropertyName, failure.ErrorMessage);
                }
                throw new CommandException("Manifest validation failed", vex, exitCode: -3);
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
                    throw new FileNotFoundException();
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
            catch(FileNotFoundException)
            {
                throw new CommandException($"Manifest file not found: {manifestPath}", exitCode: -2);
            }
            catch (Exception ex)
            {
                throw new CommandException("Failed to load manifest file", ex, exitCode: -2);
            }
        }

        private static string ResolvePath(string path, string baseDir)
        {
            return Path.IsPathRooted(path) ? path : Path.GetFullPath(Path.Combine(baseDir, path));
        }
    }
}