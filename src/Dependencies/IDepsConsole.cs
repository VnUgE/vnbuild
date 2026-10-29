/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: IDepsConsole.cs
*
* IDepsConsole.cs is part of vnbuild which is part of the larger 
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

namespace VNLib.Tools.Build.Executor.Dependencies
{
    /// <summary>
    /// Abstracts console output for the dependency installer, decoupling
    /// domain logic from the CLI framework's console interface.
    /// </summary>
    internal interface IDepsConsole
    {
        /// <summary>
        /// Writes an informational line to standard output.
        /// </summary>
        void WriteLine(string message);

        /// <summary>
        /// Writes an error line to standard error.
        /// </summary>
        void WriteError(string message);

        /// <summary>
        /// Writes a success message, typically highlighted in green.
        /// </summary>
        void WriteSuccess(string message);
    }
}
