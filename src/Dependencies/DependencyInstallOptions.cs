/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DependencyInstallOptions.cs
*
* DependencyInstallOptions.cs is part of vnbuild which is part of the larger 
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
    /// Describes options for a dependency install operation.
    /// </summary>
    /// <param name="WorkingDirectory"></param>
    /// <param name="TempDirectory"></param>
    /// <param name="ShowProgress"></param>
    /// <param name="Verbose"></param>
    public sealed record DependencyInstallOptions(
        string WorkingDirectory, 
        string TempDirectory, 
        bool ShowProgress, 
        bool Verbose
    );
}