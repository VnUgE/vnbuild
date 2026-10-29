/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: IDependencyExtractor.cs
*
* IDependencyExtractor.cs is part of vnbuild which is part of the larger 
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
using System.Threading;
using System.Threading.Tasks;

namespace VNLib.Tools.Build.Executor.Dependencies.Abstractions
{
    /// <summary>
    /// Defines an extractor that can unpack dependency archives.
    /// </summary>
    public interface IDependencyExtractor
    {
        /// <summary>
        /// Returns true if this extractor handles the given archive file type based on extension.
        /// This check is purely based on the file name; it does not verify tool availability.
        /// </summary>
        bool CanExtract(FileInfo archiveFile);

        /// <summary>
        /// Returns true if the underlying extraction tool is installed and available on the current platform.
        /// </summary>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Extracts the archive into the destination directory.
        /// </summary>
        Task ExtractAsync(
            DependencyExtractionRequest request,
            CancellationToken cancellationToken
        );
    }
}