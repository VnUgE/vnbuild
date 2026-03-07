/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DependencyJson.cs
*
* DependencyJson.cs is part of vnbuild which is part of the larger 
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
    public sealed class DependencyJson
    {
        /// <summary>
        /// The source url of the package to download and unpack. Source is required.
        /// </summary>
        [JsonPropertyName("source")]
        public required string Source { get; init; }

        /// <summary>
        /// The output directory to write the unpacked source to (chroot) or
        /// to place the artifact if unpacking is disabled
        /// </summary>
        [JsonPropertyName("dest")]
        public required string Destination { get; init; }

        /// <summary>
        /// An optional checksum for the source file to compare against
        /// </summary>
        [JsonPropertyName("checksum")]
        public string? Sum { get; init; }

        /// <summary>
        /// A value that indicates if the http connection used to download the package
        /// respects default SSL/TLS security validations, or disable them. The default
        /// is false. (Use security)
        /// </summary>
        [JsonPropertyName("insecure")]
        public bool Insecure { get; init; }

        /// <summary>
        /// A value that indicates if the dependency should be automatically unpacked 
        /// after downloaded. The default is true.
        /// </summary>
        [JsonPropertyName("unpack")]
        public bool Unpack { get; init; } = true;

        /// <summary>
        /// An optional command string to execute on the terminal after a successful
        /// install.
        /// </summary>
        [JsonPropertyName("post_install_cmd")]
        public string? PostInstallCommand { get; init; }

        /// <summary>
        /// An optional command string to execute on the terminal before the dependency 
        /// is installed.
        /// </summary>
        [JsonPropertyName("pre_install_cmd")]
        public string? PreInstallCommand { get; init; }

        /// <summary>
        /// Allows preventing overwriting destination files or directories when installing.
        /// Defaults to true to maintain current behavior.
        /// </summary>
        [JsonPropertyName("allow_overwrite")]
        public bool AllowOverwrite { get; init; } = true;
    }
}