using System;
using System.Collections.Generic;

namespace VNLib.Tools.Build.Executor.Model
{
    /// <summary>
    /// Represents a collection of taskfile "environment" variables
    /// </summary>
    public sealed class TaskfileVars
    {
        private readonly Dictionary<string, string> vars;

        public TaskfileVars()
        {
            vars = new(StringComparer.OrdinalIgnoreCase);
        }

        private TaskfileVars(IEnumerable<KeyValuePair<string, string>> values)
        {
            vars = new(values, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets all variables as a readonly dictionary
        /// </summary>
        /// <returns>The collection of environment variables</returns>
        public IReadOnlyDictionary<string, string> GetVariables() => vars;

        /// <summary>
        /// Sets a taskfile environment variable
        /// </summary>
        /// <param name="key">The variable name</param>
        /// <param name="value">The optional variable value</param>
        public void Set(string key, string? value) => vars[key] = value ?? string.Empty;

        /// <summary>
        /// Removes a taskfile environment variable
        /// </summary>
        /// <param name="key">The name of the variable to remove</param>
        public void Remove(string key) => vars.Remove(key);

        /// <summary>
        /// Clones the current taskfile variables into an independent instance
        /// </summary>
        /// <returns>The new <see cref="TaskfileVars"/> instance</returns>
        public TaskfileVars Clone() => new (vars);
    }
}