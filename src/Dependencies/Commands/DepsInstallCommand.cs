/*
* Copyright (c) 2026 Vaughn Nugent
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
using VNLib.Tools.Build.Executor.Dependencies.Validation;
using VNLib.Tools.Build.Executor.Dependencies.Extractors;

namespace VNLib.Tools.Build.Executor.Dependencies.Commands
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
        /// Allows the user to set a temporary directory when installing packages
        /// </summary>
        [CommandOption("temp-dir", Description = "A file path to store temporary files to during the install process")]
        public string TempDir { get; set; } = Path.GetTempPath();

        /// <summary>
        /// Specifies the path to the manifest file used to manage the dependencies for the operation
        /// </summary>
        [CommandOption("file", 'f', Description = "The dependency manifest file path")]
        public string ManifestFilePath { get; set; } = "deps.json";

        /// <summary>
        /// The working directory for the application
        /// </summary>
        [CommandOption("work-dir", 'w', Description = "Sets the working directory for the operation")]
        public string WorkDir { get; set; } = Directory.GetCurrentDirectory();      

        /// <summary>
        /// Gets or sets a value indicating whether to display verbose logging output during
        /// </summary>
        [CommandOption("verbose", 'v', Description = "Enables verbose logging output")]
        public bool Verbose { get; set; }

        // TODO: --parallel and --atomic flags are planned but not yet implemented.

        public virtual async ValueTask ExecuteAsync(IConsole console)
        {
            DepsInstallerConsole depsConsole = new(console);

            try
            {
                depsConsole.WriteLine($"Loading manifest {ManifestFilePath} with working dir {WorkDir}");               

                // Load the dependency manifest file from the disk
                DepsManifestJson deps = await DepsManifestLoader.LoadManifestAsync(
                    DepsManifestLoader.ResolvePath(ManifestFilePath, WorkDir), 
                    console.GetCancellationToken()
                );

                // Validate manifest
                new DepsManifestValidator()
                    .ValidateAndThrowEx(deps, console);

                DependencyInstaller installer = new(
                    depsConsole,
                    new CurlDependencyDownloader(),
                    DependencyExtractorRegistry.CreateExtractors()
                );

                DependencyInstallOptions opts = new(
                    WorkingDirectory:   DepsManifestLoader.ResolvePath(WorkDir, Directory.GetCurrentDirectory()),
                    TempDirectory:      TempDir,
                    ShowProgress:       ShowProgress,
                    Verbose:            Verbose
                );

                await installer.InstallAsync(deps, opts, console.GetCancellationToken());
            }
            catch (ValidationException vex)
            {
                depsConsole.WriteError("Manifest validation failed:");

                foreach (ValidationFailure failure in vex.Errors)
                {
                    depsConsole.WriteError($" - {failure.PropertyName}: {failure.ErrorMessage}");
                }

                throw new CommandException("Manifest validation failed", vex, exitCode: -3);
            }
            catch (OperationCanceledException)
            {
                throw new CommandException("Operation cancelled", exitCode: 0);
            }
        }
    }
}