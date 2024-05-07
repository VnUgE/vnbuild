using System.Collections.Generic;

namespace VNLib.Tools.Build.Executor.Model
{
    /// <summary>
    /// Represents a project explorer, capable of discovering projects within a module
    /// </summary>
    internal interface IProjectExplorer
    {
        /// <summary>
        /// Discovers all projects within the module
        /// </summary>
        /// <returns>An enumeration of projects discovered</returns>
        IEnumerable<IProject> DiscoverProjects();
    }
}