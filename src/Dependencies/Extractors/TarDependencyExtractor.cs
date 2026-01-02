/*
* Copyright (c) 2025 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: TarDependencyExtractor.cs
*
* TarDependencyExtractor.cs is part of vnbuild which is part of the larger 
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

    internal sealed class TarDependencyExtractor(string tarExePath = "tar") : IDependencyExtractor
    {
        public bool CanExtract(FileInfo archiveFile)
        {
            return archiveFile.Extension.ToLowerInvariant() switch
            {
                ".tar"      => true,
                ".gz"       => true,
                ".bz2"      => true,
                ".xz"       => true,
                ".tgz"      => true,
                ".tbz2"     => true,
                ".txz"      => true,
                ".tar.gz"   => true,
                ".tar.bz2"  => true,
                ".tar.xz"   => true,
                _           => false,
            };
        }

        public async Task ExtractAsync(DependencyExtractionRequest request, CancellationToken cancellationToken)
        {
            ProcessStartInfo psi = new(tarExePath)
            {
                RedirectStandardOutput   = true,
                RedirectStandardError    = true,
                UseShellExecute          = false,
                CreateNoWindow           = true,
                WorkingDirectory         = Directory.GetCurrentDirectory(),
            };

            if(request.Verbose) psi.ArgumentList.Add("--verbose");

            // Always extract to the specified directory
            psi.ArgumentList.Add("-C");
            psi.ArgumentList.Add(request.DestinationDirectory.FullName);

            psi.ArgumentList.Add("-xf");
            psi.ArgumentList.Add(request.ArchiveFile.FullName);

            // Determine compression based on file extension
            switch (request.ArchiveFile.Extension.ToLowerInvariant())
            {
                case ".tar":
                    // No compression
                    break;
                case ".tgz":
                case ".gz":
                case ".tar.gz":
                    psi.ArgumentList.Add("--gzip");
                    break;

                case ".tbz2":
                case ".bz2":
                case ".tar.bz2":
                    psi.ArgumentList.Add("--bzip2");
                    break;
                
                case ".txz":
                case ".xz":
                case ".tar.xz":
                    psi.ArgumentList.Add("--xz");
                    break;
            }

            using Process? proc = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start tar command");

            await Task.WhenAll(
                ProcessRunner.ProcessStdOutAsync(proc, tarExePath, Console.Out, cancellationToken),
                ProcessRunner.ProcessStdErrAsync(proc, tarExePath, Console.Error, cancellationToken),
                proc.WaitForExitAsync(cancellationToken)
            ).ConfigureAwait(false);

            // curl uses exit code 0 for success; any other code indicates failure
            if (proc.ExitCode != 0)
            {
                throw new CommandException(
                    $"tar failed with exit code {proc.ExitCode}",
                    exitCode: -2
                );
            }
        }
    }
}
