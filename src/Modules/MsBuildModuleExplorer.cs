using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Microsoft.Build.Construction;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Projects;
using VNLib.Tools.Build.Executor.Constants;
using static VNLib.Tools.Build.Executor.Constants.Config;
using VNLib.Tools.Build.Executor.Extensions;

namespace VNLib.Tools.Build.Executor.Modules
{

    /*
     * Discovers all projects within a dotnet solution file. First it finds the solution file in the 
     * working directory, then it parses the solution file to find all projects within the solution.
     * 
     * Leaf projects are then discovered by searching for all project files within the working directory
     * that are not part of the solution.
     */

    internal sealed class MsBuildModuleExplorer(BuildConfig config, IModuleData Module, DirectoryInfo ModuleDir) : IProjectExplorer
    {
        ///<inheritdoc/>
        public IEnumerable<IProject> DiscoverProjects()
        {
            LinkedList<IProject> projects = new();

            //First load the solution file
            FileInfo? slnFile = ModuleDir.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if(slnFile is not null)
            {
                GetProjectsForSoution(slnFile, projects);
            }
          
            //Capture any leaf projects that are not part of the solution
            IEnumerable<FileInfo> leafProjects = ModuleDir.EnumerateFiles("package.json", SearchOption.AllDirectories).Distinct();

            //Capture them
            foreach (FileInfo leafProjFile in leafProjects)
            {
                //Create relative file path
                string realtivePath = leafProjFile.FullName.Replace(ModuleDir.FullName, string.Empty).TrimStart(Path.DirectorySeparatorChar);

                //If the leaf project is ignored, skip it
                if (Module.Repository.IsFileIgnored(realtivePath))
                {
                    continue;
                }
             
                //Create the leaf project
                LeafProject project = new(config, leafProjFile);

                Log.Verbose("Discovered leaf project in {proj} ", leafProjFile.DirectoryName);

                projects.AddLast(project);
            }

            return projects;
        }

        private static void GetProjectsForSoution(FileInfo slnFile, LinkedList<IProject> projects)
        {
            //Parse solution
            SolutionFile Solution = SolutionFile.Parse(slnFile.FullName);

            //Loop through all artificats within the solution
            foreach (ProjectInSolution proj in Solution.ProjectsInOrder.Where(static p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat))
            {
                //Ignore test projects in a solution
                if(proj.ProjectName.Contains("test", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                //Create the new project artifact
                DotnetProject project = new(new(proj.AbsolutePath), proj.ProjectName);

                Log.Verbose("Discovered project {proj} ", proj.ProjectName);

                projects.AddLast(project);
            }
        }
    }
}