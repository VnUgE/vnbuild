/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: CurlDependencyDownloader.cs
*
* CurlDependencyDownloader.cs is part of vnbuild which is part of the larger 
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

namespace VNLib.Tools.Build.Executor.Dependencies.Downloaders
{
    /// <summary>
    /// Implements dependency downloading using the curl command-line tool.
    /// Checks for curl availability at construction time and caches the result.
    /// </summary>
    internal sealed class CurlDependencyDownloader(string curlExe = "curl"): IDependencyDownloader
    {

        /// <inheritdoc/>
        public static async Task<bool> IsAvailableAsync(string curlExe = "curl")
        {
            ProcessStartInfo psi = new(curlExe, "--version")
            {
                RedirectStandardOutput  = true,
                RedirectStandardError   = true,
                UseShellExecute         = false,
                CreateNoWindow          = true,
            };

            return await ProcessRunner.CheckAvailableAsync(psi).ConfigureAwait(false);
        }

        internal static string GetFileExtensionFromUrl(Uri uri)
        {
            string path = uri.AbsolutePath;
            string extension = Path.GetExtension(path);
            return string.IsNullOrEmpty(extension) ? ".bin" : extension;
        }

        /// <inheritdoc/>
        public async Task<DependencyDownloadResult> DownloadAsync(
           DependencyDownloadRequest request,
           CancellationToken cancellationToken
        )
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(request.Source);
            ArgumentNullException.ThrowIfNull(request.TargetDir);            

            // Determine file extension based on content type
            string targetFileName = Path.ChangeExtension(
                Path.Combine(
                    request.TargetDir.FullName,
                    Path.GetRandomFileName()                
                ),
                GetFileExtensionFromUrl(request.Source)
            );

            FileInfo targetFile = new(targetFileName);

            await DownloadInternalAsync(request, targetFile, cancellationToken)
                .ConfigureAwait(false);

            // Refresh file info to get accurate metadata after download
            targetFile.Refresh();

            // Verify the file was actually created by curl
            if (!targetFile.Exists)
            {
                throw new CommandException(
                    "curl completed but target file was not created",
                    exitCode: -2
                );
            }

            DependencyDownloadResult result = new()
            {
                DownloadedFile  = targetFile,
                ContentLength   = targetFile.Length
            };

            return result;
        }
        
        private async Task DownloadInternalAsync(
            DependencyDownloadRequest request,
            FileInfo targetFileName,
            CancellationToken cancellationToken
        )
        {
            ProcessStartInfo psi = new(curlExe)
            {
                RedirectStandardOutput  = true,
                RedirectStandardError   = true,
                UseShellExecute         = false,
                CreateNoWindow          = true,
            };

            psi.ArgumentList.Add("--location");         // Support redirects by default
            psi.ArgumentList.Add("--fail");             // enable curl fail-fast mode 
            psi.ArgumentList.Add("--compressed");       // Enable curl response compression support
            psi.ArgumentList.Add("--output");
            psi.ArgumentList.Add(targetFileName.FullName);

            if (request.ShowProgress)
            {
                psi.ArgumentList.Add("--progress-bar");
            }
            else
            {
                psi.ArgumentList.Add("--silent");
                psi.ArgumentList.Add("--show-error");
            }

            if (request.AllowInsecure)  psi.ArgumentList.Add("--insecure");
            if (request.Verbose)        psi.ArgumentList.Add("--verbose");

            psi.ArgumentList.Add(request.Source.AbsoluteUri);

            await ProcessRunner.RunAndThrowAsync(psi, curlExe, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
