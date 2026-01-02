/*
* Copyright (c) 2025 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DepsManifestJson.cs
*
* DepsManifestJson.cs is part of vnbuild which is part of the larger 
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

using System.Text.Json.Serialization;

namespace VNLib.Tools.Build.Executor.Dependencies.Config
{
    public sealed class DepsManifestJson
    {
        /// <summary>
        /// The array of dependency objects for download/install
        /// </summary>
        [JsonPropertyName("dependencies")]
        public DependencyJson[] Dependencies { get; set; } = [];

        /// <summary>
        /// Allows duplicate destination paths when true.
        /// </summary>
        [JsonPropertyName("allow_duplicates")]
        public bool AllowDuplicates { get; init; }
    }
}