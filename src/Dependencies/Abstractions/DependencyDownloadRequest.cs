/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DependencyDownloadRequest.cs
*
* DependencyDownloadRequest.cs is part of vnbuild which is part of the larger 
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

namespace VNLib.Tools.Build.Executor.Dependencies.Abstractions
{
    /// <summary>
    /// Represents a download operation for a dependency artifact.
    /// </summary>
    public sealed class DependencyDownloadRequest
    {
        public required Uri Source { get; init; }

        public required DirectoryInfo TargetDir { get; init; }

        public bool AllowInsecure { get; init; }

        public bool ShowProgress { get; init; }

        public bool Verbose { get; init; }
    }
}