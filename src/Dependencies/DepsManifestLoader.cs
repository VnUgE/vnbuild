/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DepsManifestLoader.cs
*
* DepsManifestLoader.cs is part of vnbuild which is part of the larger 
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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Typin.Exceptions;

using VNLib.Tools.Build.Executor.Dependencies.Config;

namespace VNLib.Tools.Build.Executor.Dependencies
{
    /// <summary>
    /// Shared utilities for loading and writing dependency manifest files.
    /// </summary>
    internal static class DepsManifestLoader
    {
        /// <summary>
        /// Lenient read options: trailing commas and comments are permitted to make
        /// hand-edited manifests more forgiving.
        /// </summary>
        internal static readonly JsonSerializerOptions ReadOptions = new()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        /// <summary>
        /// Write options: pretty-printed output for human-readable manifest files.
        /// </summary>
        internal static readonly JsonSerializerOptions WriteOptions = new()
        {
            AllowTrailingCommas = true,
            WriteIndented       = true,
        };

        /// <summary>
        /// Loads and deserializes a dependency manifest JSON file from <paramref name="manifestPath"/>.
        /// Throws a <see cref="CommandException"/> if the file is missing, empty, or malformed.
        /// </summary>
        internal static async Task<DepsManifestJson> LoadManifestAsync(string manifestPath, CancellationToken ct)
        {
            try
            {
                FileInfo manifestFile = new(manifestPath);

                if (!manifestFile.Exists)
                {
                    throw new FileNotFoundException();
                }

                using FileStream manifestFileStream = manifestFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

                DepsManifestJson? deps = await JsonSerializer
                    .DeserializeAsync<DepsManifestJson>(manifestFileStream, ReadOptions, ct)
                    .ConfigureAwait(false)
                    ?? throw new JsonException("Manifest file was empty or contained null root");

                return deps;
            }
            catch (FileNotFoundException)
            {
                throw new CommandException($"Manifest file not found: {manifestPath}", exitCode: -2);
            }
            catch (Exception ex) when (ex is not CommandException)
            {
                throw new CommandException("Failed to load manifest file", ex, exitCode: -2);
            }
        }

        /// <summary>
        /// Resolves <paramref name="path"/> relative to <paramref name="baseDir"/> when it is not
        /// already rooted, returning a fully-qualified absolute path in either case.
        /// </summary>
        internal static string ResolvePath(string path, string baseDir)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            ArgumentException.ThrowIfNullOrEmpty(baseDir);

            return Path.IsPathRooted(path)
                ? path
                : Path.GetFullPath(Path.Combine(baseDir, path));
        }
    }
}
