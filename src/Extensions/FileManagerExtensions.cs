using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;
using static VNLib.Tools.Build.Executor.Constants.Config;

namespace VNLib.Tools.Build.Executor.Extensions
{

    internal static class FileManagerExtensions
    {

        private static readonly ConditionalWeakTable<IProject, object> _projectHashes = new();

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
            string[] selfProjects = module.Projects.Select(static p => p.ProjectFile.Name).ToArray();

            return module.Projects.SelectMany(static p => p.GetDependencies()).Where(dep => !selfProjects.Contains(dep)).ToArray();
        }

        /// <summary>
        /// Determines if any source files have changed
        /// </summary>
        /// <param name="commit">The most recent commit hash</param>
        public static async Task CheckSourceChangedAsync(this IModuleFileManager manager, IProject project, BuildConfig config, string commit)
        {
            //Compute current sum
            string sum = await project.GetSourceFileHashAsync(config);

            //Old sum file exists
            byte[]? sumData = await manager.ReadCheckSumAsync(project);

            //Try to read the old sum file
            if (sumData != null)
            {
                //Parse sum file
                using JsonDocument sumDoc = JsonDocument.Parse(sumData);

                //Get the sum
                string? hexSum = sumDoc.RootElement.GetProperty("sum").GetString();

                //Confirm the current sum and the found sum are equal
                if (sum.Equals(hexSum, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Verbose("Project {p} source is {up}", project.ProjectName, "up-to-date");

                    //Project source is up-to-date
                    project.UpToDate = true;

                    //No changes made
                    return;
                }
            }

            Log.Verbose("Project {p} source is {up}", project.ProjectName, "changed");

            //Store sum change
            object sumChange = new Dictionary<string, string>()
            {
                { "sum", sum },
                { "commit", commit },
                { "modified", DateTimeOffset.UtcNow.ToString("s") }
            };

            //Store sum change for later
            _projectHashes.Add(project, sumChange);

            project.UpToDate = false;
        }

        /// <summary>
        /// Creates the module's output directory
        /// </summary>
        /// <param name="manager"></param>
        public static void CreateOutput(this IModuleFileManager manager)
        {
            //Create output directory for solution
            _ = Directory.CreateDirectory(manager.OutputDir);
        }

        /// <summary>
        /// Deletes the output's module directory
        /// </summary>
        /// <param name="manager"></param>
        public static void CleanOutput(this IModuleFileManager manager)
        {
            //Delete output directory for solution
            if (Directory.Exists(manager.OutputDir))
            {
                Directory.Delete(manager.OutputDir, true);
            }
        }
       

        /// <summary>
        /// Writes the source file checksum change to the project's sum file
        /// </summary>
        /// <param name="project"></param>
        /// <param name="project">The project to write the checksum for</param>
        /// <returns>A task that resolves when the sum file has been updated</returns>
        public static Task CommitSumChangeAsync(this IModuleFileManager manager, IProject project)
        {
            if (!_projectHashes.TryGetValue(project, out object? sumChange))
            {
                return Task.CompletedTask;
            }

            byte[] sumData = JsonSerializer.SerializeToUtf8Bytes(sumChange);

            return manager.WriteChecksumAsync(project, sumData);
        }
    }
}