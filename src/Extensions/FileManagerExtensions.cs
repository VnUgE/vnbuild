using System;
using System.IO;
using System.Linq;

using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Extensions
{

    internal static class FileManagerExtensions
    {

        /// <summary>
        /// Gets all external dependencies for the current module
        /// </summary>
        /// <param name="module"></param>
        /// <returns>An array of project names of all the dependencies outside a given module</returns>
        public static string[] GetExternalDependencies(this IModuleData module)
        {
            /*
             * We need to get all child project dependencies that rely on projects
             * outside of the current module.
             * 
             * This assumes all projects within this model are properly linked
             * and assumed to be build together, as is 99% of the case, otherwise
             * custom build impl will happen at the Task level
             */

            //Get the project file names contained in the current module
            string[] selfProjects = module.Projects
                .Select(static p => Path.GetFileName(p.Config.ProjectFilePath))
                .ToArray();

            return module.Projects
                .SelectMany(static p => p.GetDependencies())
                .Where(dep => !selfProjects.Contains(dep))
                .ToArray();
        }
    }
}