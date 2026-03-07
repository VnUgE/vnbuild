/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: IDependencyDownloader.cs
*
* IDependencyDownloader.cs is part of vnbuild which is part of the larger 
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

using System.Threading;
using System.Threading.Tasks;

namespace VNLib.Tools.Build.Executor.Dependencies.Abstractions
{
    /// <summary>
    /// Defines a downloader that can fetch dependency artifacts.
    /// </summary>
    public interface IDependencyDownloader
    {
        /// <summary>
        /// Returns true if the downloader can execute on the current platform.
        /// </summary>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Downloads the specified dependency artifact to the target file.
        /// </summary>
        Task<DependencyDownloadResult> DownloadAsync(
            DependencyDownloadRequest request,
            CancellationToken cancellationToken
        );
    }
}