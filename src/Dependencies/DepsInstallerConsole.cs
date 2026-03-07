/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DepsInstallerConsole.cs
*
* DepsInstallerConsole.cs is part of vnbuild which is part of the larger 
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

using Typin.Console;

using VNLib.Tools.Build.Executor.Extensions;

namespace VNLib.Tools.Build.Executor.Dependencies
{
    /// <summary>
    /// Wraps a Typin <see cref="IConsole"/> to implement <see cref="IDepsConsole"/>,
    /// isolating the dependency installer from the CLI framework's console type.
    /// </summary>
    internal sealed class DepsInstallerConsole(IConsole console) : IDepsConsole
    {
        /// <inheritdoc/>
        public void WriteLine(string message) => console.Output.WriteLine(message);

        /// <inheritdoc/>
        public void WriteError(string message) => console.Error.WriteLine(message);

        /// <inheritdoc/>
        public void WriteSuccess(string message) => console.WriteGreen(message);
    }
}
