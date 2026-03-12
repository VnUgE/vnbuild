/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: PowershellZipExtractor.cs
*
* PowershellZipExtractor.cs is part of vnbuild which is part of the larger 
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

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Dependencies.Abstractions;

namespace VNLib.Tools.Build.Executor.Dependencies.Extractors
{
    /// <summary>
    /// Extracts .zip archives on Windows using the PowerShell <c>Expand-Archive</c> cmdlet.
    /// </summary>
    internal sealed class PowershellZipExtractor(PowershellCmdRunner runner) : IDependencyExtractor
    {
        private readonly PowershellCmdRunner _runner = runner;

        /// <inheritdoc/>
        public bool CanExtract(FileInfo archiveFile) 
            => archiveFile.Extension.Equals(".zip", System.StringComparison.InvariantCultureIgnoreCase);

        /// <inheritdoc/>
        public async Task<bool> IsAvailableAsync() 
            => await _runner.IsAvailableAsync().ConfigureAwait(false);

        /// <inheritdoc/>
        public async Task ExtractAsync(DependencyExtractionRequest request, CancellationToken cancellationToken)
        {

            StringBuilder sb = new();
            
            sb.Append("Expand-Archive");

            sb.Append(" -Path '");
            sb.Append(request.ArchiveFile.FullName);
            sb.Append('\'');

            sb.Append(" -DestinationPath '");
            sb.Append(request.DestinationDirectory.FullName);
            sb.Append('\'');

            if (request.AllowOverwrite) sb.Append(" -Force");
            if (request.Verbose) sb.Append(" -Verbose");

            await _runner.ExecCommandAsync(sb.ToString(), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
