/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DependencyValidator.cs
*
* DependencyValidator.cs is part of vnbuild which is part of the larger 
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

using FluentValidation;

using VNLib.Tools.Build.Executor.Dependencies.Config;

namespace VNLib.Tools.Build.Executor.Dependencies.Validation
{
    /// <summary>
    /// Validates a single dependency entry.
    /// </summary>
    internal sealed class DependencyValidator : AbstractValidator<DependencyJson>
    {
        public DependencyValidator()
        {
            RuleFor(d => d.Source)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Matches("^https?://.+")
                .WithMessage(d => $"Dependency source must be a valid http or https URI: {d.Source}");

            RuleFor(d => d.Destination)
                .NotEmpty();

            RuleFor(d => d.Sum)
                .Matches("^(?:(?:sha1|sha256|sha384|sha512|md5):)?(?:[0-9a-fA-F]{2})+$")
                .When(d => !string.IsNullOrWhiteSpace(d.Sum))
                .WithMessage(d => $"Invalid checksum format for dependency {d.Source}");

            RuleFor(d => d.Sum)
                .NotEmpty()
                .When(d => d.Unpack)
                .WithSeverity(Severity.Warning)
                .WithMessage(d => $"Dependency {d.Source} is unpacked without a checksum.");
        }

    }
}