/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: UnzipDependencyExtractor.cs
*
* UnzipDependencyExtractor.cs is part of vnbuild which is part of the larger 
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
    /// Extracts .zip archives using the system <c>unzip</c> command-line tool.
    /// </summary>
    internal sealed class UnzipDependencyExtractor(string unzipCmd = "unzip") : IDependencyExtractor
    {
        /// <inheritdoc/>
        public bool CanExtract(FileInfo archiveFile)
        {
            return archiveFile.Extension.ToLowerInvariant() == ".zip";
        }

        /// <inheritdoc/>
        public async Task<bool> IsAvailableAsync()
        {
            ProcessStartInfo psi = new(unzipCmd, "-v")
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
            ProcessStartInfo psi = new(unzipCmd)
            {
                RedirectStandardOutput  = true,
                RedirectStandardError   = true,
                UseShellExecute         = false,
                CreateNoWindow          = true,
            };

            // Overwrite existing files without prompting
            if (request.AllowOverwrite) psi.ArgumentList.Add("-o");
            if (request.Verbose)        psi.ArgumentList.Add("-v");

            psi.ArgumentList.Add(request.ArchiveFile.FullName);
            psi.ArgumentList.Add("-d");
            psi.ArgumentList.Add(request.DestinationDirectory.FullName);

            await ProcessRunner.RunAndThrowAsync(psi, unzipCmd, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
