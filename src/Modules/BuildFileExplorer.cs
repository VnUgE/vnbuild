using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

using Serilog.Core;

using Microsoft.Build.Construction;

using LibGit2Sharp;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Projects;
using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Extensions;
using VNLib.Tools.Build.Executor.Directories;

namespace VNLib.Tools.Build.Executor.Modules
{

    /*
     * Discovers all projects within a dotnet solution file. First it finds the solution file in the 
     * working directory, then it parses the solution file to find all projects within the solution.
     * 
     * Leaf projects are then discovered by searching for all project files within the working directory
     * that are not part of the solution.
     */

    internal sealed class BuildFileExplorer
    {
        public static async Task<IEnumerable<ModuleConfig>> DiscoverModulesAsync(BuildConfig config, IDirectoryIndex index)
        {
            LinkedList<ModuleConfig> modules = new();

            //Find all child dirs that contian a .git directory
            IEnumerable<string> moduleDirs = Directory.EnumerateDirectories(
                path: index.GetDirectory(VnbuildDir.Working),
                searchPattern: config.GitDirName,
                SearchOption.AllDirectories
            ).Select(dir => Directory.GetParent(dir)!.FullName);

            //Capture all modules
            foreach (string moduleDir in moduleDirs)
            {
                ModuleConfig mod = await ReadModConfigAsync(config, moduleDir);

                //Always override the name and directory before use
                mod.ModuleName = GetSolutionFileNameIfExists(moduleDir, mod.SolutionFilePattern ?? "*.sln") ?? Path.GetFileName(moduleDir);
                mod.ModuleDirectory = moduleDir;

                config.Log.Verbose("Discovered module {mod} ", mod.ModuleName);
                modules.AddLast(mod);
            }
            
            return modules;
        }

        /// <summary>
        /// Gets the name of the solution file if it exists in the module directory,
        /// and returns the clean name without the .build extension and .snl extension.
        /// </summary>
        /// <param name="modDir">The directory containing the module</param>
        /// <param name="mod">The module configuration object</param>
        /// <returns></returns>
        internal static string? GetSolutionFileNameIfExists(string dir, string slnPattern)
        {
            string? slnFileName = Directory.EnumerateFiles(
                path: dir,
                searchPattern: slnPattern,
                SearchOption.TopDirectoryOnly
            ).FirstOrDefault();

            if(slnFileName is null)
            {
                return null;
            }

            slnFileName = slnFileName.Replace(".build", string.Empty);

            return Path.GetFileNameWithoutExtension(slnFileName);
        }

        private static async Task<ModuleConfig> ReadModConfigAsync(BuildConfig config, string modDir)
        {
            string? modFilePath = Path.Combine(modDir, config.ModuleConfigFileName);

            if (File.Exists(modFilePath))
            {
                byte[] bytes = await File.ReadAllBytesAsync(modFilePath);
                return JsonSerializer.Deserialize<ModuleConfig>(bytes)!;
            }

            return new ModuleConfig();
        }


        ///<inheritdoc/>
        public static IEnumerable<IProject> DiscoverProjects(Logger log, ModuleConfig mod, Repository repo, IDirectoryIndex index)
        {
            LinkedList<IProject> projects = new();

            //Load the module soltuion file if it has one (might not be a .NET project)
            string? slnFile = Directory.EnumerateFiles(
                path: index.GetDirectory(VnbuildDir.Working, mod),
                searchPattern: mod.SolutionFilePattern,
                SearchOption.TopDirectoryOnly
            ).FirstOrDefault();           

            if(slnFile is not null)
            {
                GetProjectsForSoution(mod, index, slnFile, projects);

                log.Verbose("Discovered {num} projects in solution {mod}\n{projects}",
                    projects.Count,
                    mod.ModuleName,
                    projects.Select(static p => p.Config.ProjectName)
                );
            }


            //Find unique leaf projects by their file names
            IEnumerable<string> leafProjects = mod.ProjectSearchPatterns
                .SelectMany((projSearchFile) => Directory.EnumerateFiles(
                        path: index.GetDirectory(VnbuildDir.Working, mod),
                        searchPattern: projSearchFile,
                        SearchOption.AllDirectories
                    )
                ).Distinct();

            //Capture them
            foreach (string leafProjFile in leafProjects)
            {
                //Create relative file path
                string realtivePath = leafProjFile
                    .Replace(index.GetDirectory(VnbuildDir.Working, mod), string.Empty)
                    .TrimStart(Path.DirectorySeparatorChar);

                //If the leaf project is ignored, skip it
                if (repo.IsFileIgnored(realtivePath))
                {
                    continue;
                }

                DirectoryInfo projectDir = new(Path.GetDirectoryName(leafProjFile)!);

                ProjectConfig conf = new()
                {
                    ProjectFilePath     = leafProjFile,
                    ProjectDirectory    = projectDir.FullName,
                    ProjectName         = projectDir.Name
                };

                //Create the leaf project
                LeafProject project = new(mod, conf, index);

                log.Verbose("Discovered leaf project in {proj} ", conf.ProjectDirectory);

                projects.AddLast(project);
            }

            return projects;
        }

        private static void GetProjectsForSoution(ModuleConfig mod, IDirectoryIndex index, string slnFile, LinkedList<IProject> projects)
        {
            //Parse solution
            SolutionFile Solution = SolutionFile.Parse(slnFile);

            //Loop through all artificats within the solution
            foreach (ProjectInSolution proj in Solution.ProjectsInOrder.Where(static p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat))
            {
                //Ignore test projects in a solution
                if(proj.ProjectName.Contains("test", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                ProjectConfig conf = new()
                {
                    ProjectDirectory    = Path.GetDirectoryName(proj.AbsolutePath)!,
                    ProjectFilePath     = proj.AbsolutePath,
                    ProjectName         = proj.ProjectName
                };

                //Create the new project artifact
                DotnetProject project = new(mod, conf, index);

                projects.AddLast(project);
            }
        }
    }
}