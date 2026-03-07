/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DepsListCommand.cs
*
* DepsListCommand.cs is part of vnbuild which is part of the larger 
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
using System.IO;
using System.Threading.Tasks;

using Typin;
using Typin.Attributes;
using Typin.Console;

using VNLib.Tools.Build.Executor.Dependencies.Config;
using VNLib.Tools.Build.Executor.Extensions;

namespace VNLib.Tools.Build.Executor.Dependencies.Commands
{
    [Command("deps list", Description = "Lists all dependencies defined in the manifest file")]
    public class DepsListCommand : ICommand
    {
        /// <summary>
        /// Path to the dependency manifest file.
        /// </summary>
        [CommandOption("file", 'f', Description = "The dependency manifest file path")]
        public string ManifestFilePath { get; set; } = "deps.json";

        public virtual async ValueTask ExecuteAsync(IConsole console)
        {
            ManifestFilePath = Path.GetFullPath(ManifestFilePath);

            if (!File.Exists(ManifestFilePath))
            {
                console.Output.WriteLine("No manifest file found at {0}", ManifestFilePath);
                return;
            }

            DepsManifestJson deps = await DepsManifestLoader.LoadManifestAsync(
                ManifestFilePath,
                console.GetCancellationToken()
            );

            if (deps.Dependencies.Length == 0)
            {
                console.Output.WriteLine("Manifest is empty: {0}", ManifestFilePath);
                return;
            }

            console.Output.WriteLine("Manifest: {0}  ({1} dependencies)", ManifestFilePath, deps.Dependencies.Length);

            for (int i = 0; i < deps.Dependencies.Length; i++)
            {
                console.Output.WriteLine();
                WriteDependency(console, deps.Dependencies[i], i + 1);
            }

            console.Output.WriteLine();
            console.WriteGreen($"Total: {deps.Dependencies.Length} dependency(ies)");
        }

        private static void WriteDependency(IConsole console, DependencyJson dep, int index)
        {
            console.Output.WriteLine("[{0}]", index);
            console.Output.WriteLine("  Source  : {0}", dep.Source);
            console.Output.WriteLine("  Dest    : {0}", dep.Destination);

            if (!string.IsNullOrWhiteSpace(dep.Sum))
            {
                console.Output.WriteLine("  Checksum: {0}", dep.Sum);
            }

            string flags = GetFlags(dep);
            if (!string.IsNullOrEmpty(flags))
            {
                console.Output.WriteLine("  Flags   : {0}", flags);
            }
        }

        private static string GetFlags(DependencyJson dep)
        {
            List<string> flags = [];
            if (!dep.Unpack)         flags.Add("no-unpack");
            if (dep.Insecure)        flags.Add("insecure");
            if (!dep.AllowOverwrite) flags.Add("no-overwrite");
            return string.Join(", ", flags);
        }
    }
}

