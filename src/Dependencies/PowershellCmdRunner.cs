/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: PowershellCmdRunner.cs
*
* PowershellCmdRunner.cs is part of vnbuild which is part of the larger 
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

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Dependencies
{
    /// <summary>
    /// Wraps the powershell terminal to expose running basic commands against
    /// it for internal operations.
    /// </summary>
    /// <param name="pwshExe">The powershell executable file path</param>
    internal sealed class PowershellCmdRunner(string pwshExe = "pwsh")
    {
        /// <summary>
        /// Determines if the platform has the powershell terminal 
        /// available
        /// </summary>
        /// <param name="pwshExe">The powershell executable file path</param>
        /// <returns>True if powershell is available</returns>
        public static Task<bool> IsAvailableAsync(string pwshExe = "pwsh")
        {
            ProcessStartInfo psi = new(pwshExe)
            {
                RedirectStandardOutput  = true,
                RedirectStandardError   = true,
                UseShellExecute         = false,
                CreateNoWindow          = true,
            };

            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add("$PSVersionTable.PSVersion");

            return ProcessRunner.CheckAvailableAsync(psi);
        }

        /// <summary>
        /// Determines if the platform has the powershell terminal 
        /// available at the configured executable path
        /// </summary>
        /// <returns>True if powershell is available</returns>
        public Task<bool> IsAvailableAsync() => IsAvailableAsync(pwshExe);

        /// <summary>
        /// Executes a powershell command on the configured powershell executable
        /// </summary>
        /// <param name="command">The command to run on the powershell terminal</param>
        /// <param name="cancellationToken">A token to cancel the async operation</param>
        public Task ExecCommandAsync(string command, CancellationToken cancellationToken)
        {
            ProcessStartInfo psi = new(pwshExe)
            {
                RedirectStandardOutput  = true,
                RedirectStandardError   = true,
                UseShellExecute         = false,
                CreateNoWindow          = true,
            };

            psi.ArgumentList.Add("-Command");
            psi.ArgumentList.Add(command);

            return ProcessRunner.RunAndThrowAsync(psi, pwshExe, cancellationToken);
        }
    }
}
