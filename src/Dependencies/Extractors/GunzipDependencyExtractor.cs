/*
* Copyright (c) 2025 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: GunzipDependencyExtractor.cs
*
* GunzipDependencyExtractor.cs is part of vnbuild which is part of the larger 
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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Typin.Exceptions;

using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Dependencies.Abstractions;

namespace VNLib.Tools.Build.Executor.Dependencies.Extractors
{
    internal sealed class GunzipDependencyExtractor(string unzipCmd = "unzip") : IDependencyExtractor
    {
        public bool CanExtract(FileInfo archiveFile)
        {
            switch (archiveFile.Extension.ToLowerInvariant())
            {
                case ".zip":
                    break;
                default:
                    return false;
            }          

            // Test that unzip is installed
            ProcessStartInfo psi = new(unzipCmd, "-v")
            {
                RedirectStandardOutput   = true,
                RedirectStandardError    = true,
                UseShellExecute          = false,
                CreateNoWindow           = true
            };

            using Process? proc = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start unzip command");

            Task all = Task.WhenAll(
              proc.StandardOutput.ReadToEndAsync(),
              proc.StandardError.ReadToEndAsync(),
              proc.WaitForExitAsync()
            );

            // Block to determine if unzip is available
            all.GetAwaiter().GetResult();

            return proc.ExitCode == 0;
        }

        public async Task ExtractAsync(DependencyExtractionRequest request, CancellationToken cancellationToken)
        {
            ProcessStartInfo psi = new(unzipCmd)
            {
                RedirectStandardOutput   = true,
                RedirectStandardError    = true,
                UseShellExecute          = false,
                CreateNoWindow           = true,
                WorkingDirectory         = Directory.GetCurrentDirectory(),
            };

            // Overwrite existing files without prompting
            if (request.AllowOverwrite) psi.ArgumentList.Add("-o");
            if (request.Verbose)        psi.ArgumentList.Add("-v");

            psi.ArgumentList.Add(request.ArchiveFile.FullName);
            psi.ArgumentList.Add("-d");
            psi.ArgumentList.Add(request.DestinationDirectory.FullName);

            using Process? proc = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start unzip command");

            await Task.WhenAll(
                ProcessRunner.ProcessStdOutAsync(proc, unzipCmd, Console.Out, cancellationToken),
                ProcessRunner.ProcessStdErrAsync(proc, unzipCmd, Console.Error, cancellationToken),
                proc.WaitForExitAsync(cancellationToken)
            ).ConfigureAwait(false);

            // pwsh uses exit code 0 for success; any other code indicates failure
            if (proc.ExitCode != 0)
            {
                throw new CommandException(
                    $"Unzip failed with exit code {proc.ExitCode}",
                    exitCode: -2
                );
            }
        }
    }
}
