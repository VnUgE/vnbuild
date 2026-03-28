/*
* Copyright (c) 2026 Vaughn Nugent
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

using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Dependencies.Abstractions;

namespace VNLib.Tools.Build.Executor.Dependencies.Extractors
{
    /// <summary>
    /// Extracts tar-based archives (.tar, .tgz, .tbz2, .txz, .tar.gz, .tar.bz2, .tar.xz)
    /// using the system <c>tar</c> command-line tool.
    /// </summary>
    internal sealed class TarDependencyExtractor(string tarExePath = "tar") : IDependencyExtractor
    {
        /// <inheritdoc/>
        public bool CanExtract(FileInfo archiveFile)
        {
            // FileInfo.Extension returns only the last dot-segment, so multi-dot extensions
            // such as ".tar.gz" must be checked against the full file name.
            string name = archiveFile.Name.ToLowerInvariant();

            return name.EndsWith(".tar")
                || name.EndsWith(".tgz")
                || name.EndsWith(".tbz2")
                || name.EndsWith(".txz")
                || name.EndsWith(".tar.gz")
                || name.EndsWith(".tar.bz2")
                || name.EndsWith(".tar.xz");
        }

        /// <inheritdoc/>
        public async Task<bool> IsAvailableAsync()
        {
            ProcessStartInfo psi = new(tarExePath, "--version")
            {
                RedirectStandardOutput  = true,
                RedirectStandardError   = true,
                UseShellExecute         = false,
                CreateNoWindow          = true,
            };

            return await ProcessRunner.CheckAvailableAsync(psi).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public async Task ExtractAsync(DependencyExtractionRequest request, CancellationToken cancellationToken)
        {
            ProcessStartInfo psi = new(tarExePath)
            {
                RedirectStandardOutput  = true,
                RedirectStandardError   = true,
                UseShellExecute         = false,
                CreateNoWindow          = true,
            };

            if (request.Verbose) psi.ArgumentList.Add("--verbose");

            // Always extract to the specified directory
            psi.ArgumentList.Add("-C");
            psi.ArgumentList.Add(request.DestinationDirectory.FullName);

            psi.ArgumentList.Add("-xf");
            psi.ArgumentList.Add(request.ArchiveFile.FullName);

            // Determine compression flag based on file extension.
            // For multi-dot names (e.g. archive.tar.gz), Extension returns the last
            // segment (.gz), which is sufficient to select the right flag.
            switch (request.ArchiveFile.Extension.ToLowerInvariant())
            {
                case ".tar":
                    // No compression flag needed
                    break;
                case ".tgz":
                case ".gz":
                    psi.ArgumentList.Add("--gzip");
                    break;

                case ".tbz2":
                case ".bz2":
                    psi.ArgumentList.Add("--bzip2");
                    break;

                case ".txz":
                case ".xz":
                    psi.ArgumentList.Add("--xz");
                    break;
            }

            await ProcessRunner.RunAndThrowAsync(psi, tarExePath, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
