/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DepsAddCommand.cs
*
* DepsAddCommand.cs is part of vnbuild which is part of the larger 
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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Typin;
using Typin.Attributes;
using Typin.Console;
using Typin.Exceptions;

using FluentValidation;
using FluentValidation.Results;

using VNLib.Tools.Build.Executor.Extensions;
using VNLib.Tools.Build.Executor.Dependencies.Config;
using VNLib.Tools.Build.Executor.Dependencies.Validation;

namespace VNLib.Tools.Build.Executor.Dependencies.Commands
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

        [CommandOption("overwrite", Description = "Allows overwriting an existing dependency")]
        public bool Overwrite { get; set; }

        /// <summary>
        /// Disables automatic unpacking of the downloaded artifact after download.
        /// </summary>
        [CommandOption("no-unpack", Description = "Disables automatic unpacking after download. The artifact is copied to the destination as-is.")]
        public bool NoUnpack { get; set; }

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
                    deps = await DepsManifestLoader.LoadManifestAsync(ManifestFilePath, console.GetCancellationToken());
                }
                else
                {
                    deps = new();
                }

                DepsManifestValidator validator = new();
                validator.ValidateAndThrowEx(deps, console);

                // Add the package to the existing manifest
                AddPackageToManifest(deps);
                console.Output.WriteLine("Added package to manifest");

                console.Output.WriteLine("Writing manifest file to {0}", ManifestFilePath);

                await WriteManifestAsync(deps);

                console.WriteGreen("Successfully added dependency");
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

        private void AddPackageToManifest(DepsManifestJson deps)
        {
            ArgumentException.ThrowIfNullOrEmpty(SourceUrl);
            ArgumentException.ThrowIfNullOrEmpty(DestPath);

            DependencyJson newDep = new()
            {
                Source              = SourceUrl,
                Destination         = DestPath,
                Sum                 = Checksum,
                Insecure            = Insecure,
                Unpack              = !NoUnpack
            };

            bool exists = deps.Dependencies
                .Any(s => string.Equals(s.Source, newDep.Source, StringComparison.OrdinalIgnoreCase));

            if (exists && !Overwrite)
            {
                throw new CommandException("Dependency already exists");
            }

            // When overwriting, replace the existing entry rather than appending a duplicate
            deps.Dependencies = exists
                ? [.. deps.Dependencies.Where(s => !string.Equals(s.Source, newDep.Source, StringComparison.OrdinalIgnoreCase)), newDep]
                : [.. deps.Dependencies, newDep];
        }

        private async Task WriteManifestAsync(DepsManifestJson deps)
        {
            FileInfo manifest = new(ManifestFilePath);

            // Create or truncate the file to avoid leftover bytes when overwriting a longer manifest
            using FileStream manifestFile = manifest.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            await JsonSerializer.SerializeAsync(manifestFile, deps, DepsManifestLoader.WriteOptions);
        }
    }
}