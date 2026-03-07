/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DependencyExtractorRegistry.cs
*
* DependencyExtractorRegistry.cs is part of vnbuild which is part of the larger 
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

using System.Collections.Generic;

using VNLib.Tools.Build.Executor.Dependencies.Abstractions;

namespace VNLib.Tools.Build.Executor.Dependencies.Extractors
{
    /// <summary>
    /// Central registry of all available dependency extractors.
    /// To add support for a new archive format, implement <see cref="IDependencyExtractor"/>
    /// and register the instance here.
    /// </summary>
    internal static class DependencyExtractorRegistry
    {
        /// <summary>
        /// Creates the ordered list of dependency extractors. Extractors are evaluated in
        /// order — the first one whose <c>CanExtract</c> returns true and whose tool is
        /// available on the current platform will be used.
        /// </summary>
        public static IReadOnlyList<IDependencyExtractor> CreateExtractors() =>
        [
            new TarDependencyExtractor(),
            new UnzipDependencyExtractor(),     // Unix unzip for .zip files
            new PowershellZipExtractor(),       // PowerShell fallback for .zip on Windows
        ];
    }
}
