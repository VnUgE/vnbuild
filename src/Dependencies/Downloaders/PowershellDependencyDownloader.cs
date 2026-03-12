/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: PowershellDependencyDownloader.cs
*
* PowershellDependencyDownloader.cs is part of vnbuild which is part of the larger 
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Typin.Exceptions;

using VNLib.Tools.Build.Executor.Dependencies.Abstractions;

namespace VNLib.Tools.Build.Executor.Dependencies.Downloaders
{
    internal sealed class PowershellDependencyDownloader(PowershellCmdRunner runner) : IDependencyDownloader
    {
        private readonly PowershellCmdRunner _runner = runner;      

        ///<inheritdoc/>
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
                CurlDependencyDownloader.GetFileExtensionFromUrl(request.Source)
            );

            FileInfo targetFile = new(targetFileName);

            await DownloadAsyncCore(request, targetFile, cancellationToken)
                .ConfigureAwait(false);

            // Refresh file info to get accurate metadata after download
            targetFile.Refresh();

            // Verify the file was actually created by curl
            if (!targetFile.Exists)
            {
                throw new CommandException(
                    "powershell Invoke-WebRequest completed but target file was not created",
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

        private Task DownloadAsyncCore(
            DependencyDownloadRequest request,
            FileInfo targetFile,
            CancellationToken cancellationToken
        )
        {
            StringBuilder cmd = new();

            cmd.Append("Invoke-WebRequest");

            cmd.Append(" -Uri '");
            cmd.Append(request.Source);
            cmd.Append('\'');

            cmd.Append(" -OutFile '");
            cmd.Append(targetFile.FullName);
            cmd.Append('\'');

            cmd.Append(" -Method Get");           

            if (request.AllowInsecure) cmd.Append(" -SkipCertificateCheck");
            if (request.Verbose) cmd.Append(" -Verbose");

            return _runner.ExecCommandAsync(cmd.ToString(), cancellationToken);
        }
    }
}
