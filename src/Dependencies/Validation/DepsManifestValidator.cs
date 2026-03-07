/*
* Copyright (c) 2026 Vaughn Nugent
* 
* Library: VNLib
* Package: vnbuild
* File: DepsManifestValidator.cs
*
* DepsManifestValidator.cs is part of vnbuild which is part of the larger 
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
using System.Linq;

using FluentValidation;
using FluentValidation.Results;

using Typin.Console;

using VNLib.Tools.Build.Executor.Dependencies.Config;
using VNLib.Tools.Build.Executor.Extensions;

namespace VNLib.Tools.Build.Executor.Dependencies.Validation
{
    /// <summary>
    /// Validates a dependency manifest and enforces manifest-level rules.
    /// </summary>
    internal sealed class DepsManifestValidator : AbstractValidator<DepsManifestJson>
    {
        public DepsManifestValidator()
        {
            RuleFor(m => m.Dependencies)
                .NotNull()
                .WithMessage("Dependency array was set to null");

            RuleForEach(m => m.Dependencies)
                .SetValidator(new DependencyValidator());

            RuleFor(m => m.Dependencies)
                .Must(NotContainDuplicateDestinations)
                .When(m => !m.AllowDuplicates)
                .WithMessage("Duplicate destination detected in manifest.");
        }

        public void ValidateAndThrowEx(DepsManifestJson instance, IConsole console)
        {
            ValidationResult result = Validate(instance);
            
            if (result.IsValid)
            {
                return;
            }

            // Fail with hard errors
            if (result.Errors.Any(e => e.Severity == Severity.Error))
            {
                throw new ValidationException(result.Errors);
            }

            // Print warning and info messages to console
            foreach (ValidationFailure failure in result.Errors)
            {
                console.WriteYellow($"{failure.Severity}: {failure.ErrorMessage}");
            }
        }

        private static bool NotContainDuplicateDestinations(DependencyJson[]? deps)
        {
            if (deps == null)
            {
                return true;
            }

            IEnumerable<string> destinations = deps
                .Select(static d => d?.Destination?.Trim())
                .Where(static d => !string.IsNullOrWhiteSpace(d))
                .Select(static d => d!);

            HashSet<string> set = new(StringComparer.OrdinalIgnoreCase);

            foreach (string dest in destinations)
            {
                if (!set.Add(dest))
                {
                    return false;
                }
            }

            return true;
        }
    }
}