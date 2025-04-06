using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Serilog;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Modules;
using VNLib.Tools.Build.Executor.Extensions;
using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Publishing;
using VNLib.Tools.Build.Executor.Directories;

namespace VNLib.Tools.Build.Executor
{
    public sealed class BuildPipeline(BuildConfig config) : IDisposable
    {
        private readonly ILogger Log = config.Log;
        private readonly List<ModuleBase> _allModules = [];
        private readonly List<ModuleBase> _selected = [];
        private readonly LinkedList<ModuleBase> _outdatedModules = new();
        private readonly LinkedList<IProject> _modifiedProjects = new();
        private readonly TaskfileVars _taskVars = new();
        private readonly GlobalDirIndex _dirIndex = new(config);

        /// <summary>
        /// Loads a modules within the working directory
        /// </summary>
        /// <returns>A task that completes when all modules and child projects are loaded</returns>
        public async Task LoadAsync(string[] include, string[] exclude)
        {
            //Directory variables will be overriden in modules and projects
            _taskVars.Set("BUILD_DIR", _dirIndex.GetDirectory(VnbuildDir.Build));
            _taskVars.Set("SCRATCH_DIR", _dirIndex.GetDirectory(VnbuildDir.Scratch));
            _taskVars.Set("OUTPUT_DIR", _dirIndex.GetDirectory(VnbuildDir.Output));
            _taskVars.Set("WORKING_DIR", _dirIndex.GetDirectory(VnbuildDir.Working));

            _taskVars.Set("UNIX_MS", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
            _taskVars.Set("DATE", DateTimeOffset.Now.ToString("d"));

            //Capture all modules within pwd
            Log.Information("Discovering modules in {pwd}", _dirIndex.GetDirectory(VnbuildDir.Working));

            IEnumerable<ModuleConfig> mods = await BuildFileExplorer.DiscoverModulesAsync(config, _dirIndex);

            //Add modules
            foreach (ModuleConfig mod in mods)
            {
                _allModules.Add(new GitCodeModule(config, mod, _dirIndex));
            }

            Log.Information("Found {c} modules, loading modules...", mods.Count());

            //Load all modules async and give them each a copy of our local task variables
            await _allModules.RunAllAsync(p => p.LoadAsync(_taskVars.Clone()));

            //Only include desired modules
            if (include.Length > 0)
            {
                Log.Information("Only including modules {mods}", include);

                ModuleBase[] onlyMods = _allModules
                    .Where(m => include.Contains(m.ModuleName, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
                
                _selected.AddRange(onlyMods);
            }
            //Exclude given modules
            else if (exclude.Length > 0)
            {
                Log.Information("Excluding modules {mods}", exclude);

                ModuleBase[] excludeMods = _allModules
                    .Where(m => exclude.Contains(m.ModuleName, StringComparer.OrdinalIgnoreCase))
                    .ToArray();
                
                _selected.AddRange(_allModules.Except(excludeMods));
            }
            else
            {
                //Just all all modules to the list
                _selected.AddRange(_allModules);
            }

            Log.Information("The following modules will be processed\n{mods}", _selected.Select(m => m.ModuleName));
        }

        /// <summary>
        /// Returns all loaded modules
        /// </summary>
        /// <returns>The collection of all loaded modules</returns>
        public IReadOnlyCollection<IModuleData> GetModules() => _selected;

        /// <summary>
        /// Synchronizes all modules with their respective remote repositories
        /// </summary>
        /// <returns></returns>
        public async Task DoStepUpdateSource()
        {
            //Clear outdated list before syncing sources
            _outdatedModules.Clear();
            _modifiedProjects.Clear();

            //Must sync source serially to prevent git errors
            foreach (ModuleBase module in _selected)
            {
                //Sync source 
                await module.DoStepSyncSource();
            }
        }

        /// <summary>
        /// Prepares the build pipeline for building, finds for changes and determines dependencies
        /// then prepares modules for building
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CheckForChangesAsync()
        {
            //Clear outdated list before syncing sources
            _outdatedModules.Clear();
            _modifiedProjects.Clear();

            //Conccurrently search for changes in all modules
            await _selected.RunAllAsync(async m =>
            {
                if (await m.CheckForChangesAsync())
                {
                    _outdatedModules.AddLast(m);
                    Log.Information("Module {m} MODIFIED. Queued for rebuild", m.ModuleName);
                }
            });

            //if one or more modules have been modified, we need to determine dependencies
            if (_outdatedModules.Count > 0)
            {

                //Get the initial list of projects that will be rebuilt
                string[] outDatedProjects = _outdatedModules
                    .SelectMany(static m => m.Projects.Where(static p => !p.UpToDate).Select(static p => p.Config.ProjectName))
                    .ToArray();

                do
                {

                    /*
                     * Select only up-to-date modules 
                     * that have external project references to outdated
                     * projects
                     */
                    ModuleBase[] dependants = _selected.Where(m => !_outdatedModules.Contains(m))
                                        .Where(
                                            m => m.GetExternalDependencies()
                                            .Where(externProj => outDatedProjects.Contains(externProj))
                                            .Any()
                                        )
                                        .ToArray();

                    //If there are no more dependants, exit loop
                    if (dependants.Length == 0)
                    {
                        break;
                    }

                    //Add modules to oudated list
                    for (int i = 0; i < dependants.Length; i++)
                    {
                        Log.Information("Module {mod} OUTDATED because it depends on out-of-date modules", dependants[i].ModuleName);
                        _outdatedModules.AddLast(dependants[i]);
                    }

                    //update outdated projects list to include projects from the newly outdated modules
                    outDatedProjects = dependants
                        .SelectMany(static p => p.GetExternalDependencies())
                        .ToArray();
                }
                while (true);
            }

            Log.Information("{c} modules detected source code changes", _outdatedModules.Count);
            return _outdatedModules.Count > 0;
        }

        public async Task DoStepBuild(bool force)
        {   
            //Rebuild all modules
            if (force)
            {
                //rebuild all selected modules
                foreach (ModuleBase mod in _selected)
                {
                    //Run each module independently
                    await BuildSingleModule(mod, Log);
                }
            }
            else
            {
                if (_outdatedModules.Count == 0)
                {
                    Log.Information("No modules detected changes");
                }

                //Only rebuild modified modules
                foreach (ModuleBase mod in _outdatedModules)
                {
                    //Run each module independently
                    await BuildSingleModule(mod, Log);
                }
            }
        }

        public async Task BuildSingleProject(string moduleName, string projectName)
        {
            //First find module
            ModuleBase module = _selected.FirstOrDefault(m => string.Equals(m.ModuleName, moduleName, StringComparison.OrdinalIgnoreCase))
                ?? throw new BuildStepFailedException(
                    message: $"Module {moduleName} not found",
                    moduleName
                );

            //Find project
            IProject project = module.Projects.FirstOrDefault(p => string.Equals(p.Config.ProjectName, projectName, StringComparison.OrdinalIgnoreCase))
                ?? throw new BuildStepFailedException(
                    message: $"Project {projectName} not found in module {moduleName}",
                    moduleName
                );

            try
            {
                //Build single project
                await module.BuildSingleProject(project);
            }
            catch
            {
                //failure
                await module.PostbuildSingleProject(project, success: false);
                throw;
            }

            //Completed successfully, await the result of post-build
            await module.PostbuildSingleProject(project, success: true);
        }

        static async Task BuildSingleModule(IBuildable module, ILogger log)
        {
            log.Information("Building module {m}", (module as ModuleBase)!.ModuleName);

            try
            {
                //Build module 
                await module.DoStepBuild();
            }
            catch
            {
                //failure
                await module.DoStepPostBuild(false);
                throw;
            }

            //Completed successfully, await the result of post-build
            await module.DoStepPostBuild(true);
        }
      
        public async Task OnPublishingAsync()
        {
            /*
             * Exec publish step on modules in order incase they 
             * need to access synchronous resources
             */
            
            foreach(ModuleBase module in _selected)
            {
                await module.DoStepPublish();
            }
        }

        public async Task PrepareOutputAsync(BuildPublisher publisher)
        {
            Log.Information("Preparing pipline output");

            if (publisher.SignEnabled)
            {
                //Sign all modules synchronously so gpg-agent doesn't get overloaded
                foreach (IModuleData module in _selected)
                {
                    //Sign module
                    await publisher.PrepareModuleOutput(module);
                }
            }
            else
            {
                await _selected.RunAllAsync(publisher.PrepareModuleOutput);
            }
        }

        /// <summary>
        /// Executes test commands for all loaded modules
        /// </summary>
        /// <returns></returns>
        public async Task ExecuteTestsAsync(bool failOnError)
        {
            foreach (ModuleBase module in _selected)
            {
                await module.DoRunTests(failOnError);
            }
        }

        /// <summary>
        /// Performs a manual upload step
        /// </summary>
        /// <returns></returns>
        public async Task ManualUpload(BuildPublisher publisher, IUploadManager uploads)
        {
            //Upload module output
            foreach (IModuleData module in _selected)
            {
                //Upload module
                await publisher.UploadModuleOutput(uploads, module);
            }
        }

        /// <summary>
        /// Cleans all modules and child projects
        /// </summary>
        /// <returns>A task that resolves when all child projects have been cleaned</returns>
        public async Task DoStepCleanAsync()
        {
            //Clean synchronously
            foreach (IArtifact module in _selected)
            {
                await module.CleanAsync();
            }

            //Delete the build directory and all its contents
            string buildDir = _dirIndex.GetDirectory(VnbuildDir.Build);
            Directory.Delete(buildDir, recursive: true);
        }

        public void Dispose()
        {
            foreach (IArtifact module in _allModules)
            {
                module.Dispose();
            }

            //Cleanup internals
            _outdatedModules.Clear();
        }
      
    }
}