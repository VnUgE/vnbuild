/*
* Copyright (c) 2025 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DependencyInstaller.cs
*
* DependencyInstaller.cs is part of vnbuild which is part of the larger 
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Exceptions;

using VNLib.Tools.Build.Executor.Dependencies.Abstractions;
using VNLib.Tools.Build.Executor.Dependencies.Config;
using VNLib.Tools.Build.Executor.Extensions;

namespace VNLib.Tools.Build.Executor.Dependencies
{

    /// <summary>
    /// Performs dependency installation using provided downloader and extractors.
    /// </summary>
    public sealed class DependencyInstaller(
        IConsole console,
        IDependencyDownloader downloader,
        IEnumerable<IDependencyExtractor> extractors
    )
    {
        private readonly IDependencyDownloader _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        private readonly IReadOnlyList<IDependencyExtractor> _extractors = (extractors ?? throw new ArgumentNullException(nameof(extractors))).ToArray();

        /// <summary>
        /// Installs dependencies defined in <paramref name="manifest"/> using the configured downloader and extractors.
        /// </summary>
        /// <param name="manifest">The dependency manifest to install.</param>
        /// <param name="options">Execution options controlling paths and behavior.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        public async Task InstallAsync(
            DepsManifestJson manifest,
            DependencyInstallOptions options,
            CancellationToken cancellationToken
        )
        {
            ArgumentNullException.ThrowIfNull(manifest);
            ArgumentNullException.ThrowIfNull(options);

            ValidateOptions(options);

            if (manifest.Dependencies.Length == 0)
            {
                return;
            }

            if (!(await _downloader.IsAvailableAsync()))
            {
                throw new CommandException("No dependency downloader is available", exitCode: -2);
            }

            Directory.CreateDirectory(options.TempDirectory);

            foreach (DependencyJson dep in manifest.Dependencies)
            {
                cancellationToken.ThrowIfCancellationRequested();

                console.Output.WriteLine($"Starting installation of: {dep.Source} -> {dep.Destination}");

                await ProcessDependencyAsync(dep, options, cancellationToken)
                    .ConfigureAwait(false);
            }

            console.WriteGreen("Success: All dependencies installed successfully");
        }

        private static void ValidateOptions(DependencyInstallOptions options)
        {
            ArgumentException.ThrowIfNullOrEmpty(options.WorkingDirectory);
            ArgumentException.ThrowIfNullOrEmpty(options.TempDirectory);

            if (!Directory.Exists(options.WorkingDirectory))
            {
                throw new CommandException(
                    $"Working directory '{options.WorkingDirectory}' does not exist",
                    exitCode: -2
                );
            }

            if (File.Exists(options.TempDirectory))
            {
                throw new CommandException(
                    $"Temporary path '{options.TempDirectory}' points to a file",
                    exitCode: -2
                );
            }
        }

        /// <summary>
        /// Processes a single dependency end-to-end.
        /// </summary>
        private async Task ProcessDependencyAsync(
            DependencyJson dependency,
            DependencyInstallOptions options,
            CancellationToken cancellationToken
        )
        {
            ArgumentNullException.ThrowIfNull(dependency);
            ArgumentException.ThrowIfNullOrEmpty(dependency.Source);
            ArgumentException.ThrowIfNullOrEmpty(dependency.Destination);

            string resolvedDestination = ResolvePath(dependency.Destination, options.WorkingDirectory);

            EnsureDestinationWritable(dependency, resolvedDestination);

            console.Output.WriteLine($"Downloading dependency from {dependency.Source}...");

            FileInfo downloadedFile = await DownloadAsync(
                dependency,
                options,
                cancellationToken
            ).ConfigureAwait(false);

            // If checksum is specified, verify the download against the sum
            if (!string.IsNullOrWhiteSpace(dependency.Sum))
            {
                await VerifyChecksumAsync(dependency.Sum, downloadedFile, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (dependency.Unpack)
            {
                console.Output.WriteLine($"Extracting dependency to {resolvedDestination}...");

                await ExtractAsync(
                    dependency,
                    downloadedFile,
                    resolvedDestination,
                    options.Verbose,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            else
            {
                CopyPayload(dependency, downloadedFile, resolvedDestination);
            }

            console.Output.WriteLine($"Installation complete");

            // TODO: pre/post install scripts when download/extract succeed
        }

        private static void EnsureDestinationWritable(DependencyJson dependency, string resolvedDestination)
        {
            if (dependency.AllowOverwrite)
            {
                return;
            }

            if (dependency.Unpack)
            {
                if (File.Exists(resolvedDestination))
                {
                    throw new CommandException(
                        $"Destination {resolvedDestination} is a file and cannot be used as an extraction target",
                        exitCode: -2
                    );
                }

                if (Directory.Exists(resolvedDestination))
                {
                    throw new CommandException(
                        $"Destination {resolvedDestination} already exists and overwrite is disabled",
                        exitCode: -2
                    );
                }
            }
            else
            {
                if (Directory.Exists(resolvedDestination))
                {
                    throw new CommandException(
                        $"Destination {resolvedDestination} is a directory and cannot be overwritten with a file",
                        exitCode: -2
                    );
                }

                if (File.Exists(resolvedDestination))
                {
                    throw new CommandException(
                        $"Destination file {resolvedDestination} already exists and overwrite is disabled",
                        exitCode: -2
                    );
                }
            }
        }

        private async Task<FileInfo> DownloadAsync(
            DependencyJson dependency,
            DependencyInstallOptions options,
            CancellationToken cancellationToken
        )
        {
            if (!Uri.TryCreate(dependency.Source, UriKind.Absolute, out Uri? source))
            {
                throw new CommandException("Dependency source URI is invalid", exitCode: -2);
            }           

            DependencyDownloadRequest request = new()
            {
                Source          = source,
                TargetDir       = new(options.TempDirectory),
                AllowInsecure   = dependency.Insecure,
                ShowProgress    = options.ShowProgress,
                Verbose         = options.Verbose
            };

            DependencyDownloadResult download = await _downloader
                .DownloadAsync(request, cancellationToken)
                .ConfigureAwait(false);

            if (download.DownloadedFile is null || !download.DownloadedFile.Exists)
            {
                throw new CommandException("Download failed to produce a file", exitCode: -2);
            }

            return download.DownloadedFile;
        }

        /// <summary>
        /// Extracts an archive payload to the destination directory using a compatible extractor.
        /// </summary>
        private async Task ExtractAsync(
            DependencyJson dependency,
            FileInfo downloadedFile,
            string resolvedDestination,
            bool verbose,
            CancellationToken cancellationToken
        )
        {
            DirectoryInfo destinationDirectory = new(resolvedDestination);           

            IDependencyExtractor? extractor = _extractors.FirstOrDefault(e => e.CanExtract(downloadedFile)) 
                ?? throw new CommandException($"No extractor found for {downloadedFile.Name}", exitCode: -2);
            
            DependencyExtractionRequest extraction = new()
            {
                ArchiveFile             = downloadedFile,
                AllowOverwrite          = dependency.AllowOverwrite,
                DestinationDirectory    = destinationDirectory,
                Verbose                 = verbose
            };

            Directory.CreateDirectory(destinationDirectory.FullName);

            await extractor.ExtractAsync(extraction, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Copies the downloaded payload to the destination when no extraction is requested.
        /// </summary>
        private static void CopyPayload(DependencyJson dependency, FileInfo downloadedFile, string resolvedDestination)
        {
            string destinationDirectory = Path.GetDirectoryName(resolvedDestination) ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            File.Copy(downloadedFile.FullName, resolvedDestination, dependency.AllowOverwrite);
        }

        private static async Task VerifyChecksumAsync(string rawChecksum, FileInfo file, CancellationToken cancellationToken)
        {
            string trimmed = rawChecksum.Trim();
            int colonIndex = trimmed.IndexOf(':');

            // Get the algorithm name or default to sha256
            string algorithmPart = colonIndex > -1 ? trimmed[..colonIndex] : "sha256";
            string hexPart = colonIndex > -1 ? trimmed[(colonIndex + 1)..] : trimmed;

            byte[] expected = Convert.FromHexString(hexPart);

            await using FileStream fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

            byte[] actual = algorithmPart.ToUpperInvariant() switch
            {
                "SHA1"      => await SHA1.HashDataAsync(fs, cancellationToken).ConfigureAwait(false),
                "SHA256"    => await SHA256.HashDataAsync(fs, cancellationToken).ConfigureAwait(false),
                "SHA384"    => await SHA384.HashDataAsync(fs, cancellationToken).ConfigureAwait(false),
                "SHA512"    => await SHA512.HashDataAsync(fs, cancellationToken).ConfigureAwait(false),
                "MD5"       => await MD5.HashDataAsync(fs, cancellationToken).ConfigureAwait(false),
                _ => throw new CommandException($"Unsupported checksum algorithm: {algorithmPart}", exitCode: -2),
            };
           
            if (!CryptographicOperations.FixedTimeEquals(actual, expected))
            {
                throw new CommandException($"Checksum mismatch for {file.FullName}", exitCode: -2);
            }
        }

        private static string ResolvePath(string path, string baseDir)
        {
            ArgumentException.ThrowIfNullOrEmpty(path);
            ArgumentException.ThrowIfNullOrEmpty(baseDir);

            return Path.IsPathRooted(path) 
                ? path 
                : Path.GetFullPath(Path.Combine(baseDir, path));
        }
    }
}