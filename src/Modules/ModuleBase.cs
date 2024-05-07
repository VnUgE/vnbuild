using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using LibGit2Sharp;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Extensions;
using static VNLib.Tools.Build.Executor.Constants.Config;

namespace VNLib.Tools.Build.Executor.Modules
{
    /// <summary>
    /// Represents a base class for all modules to inherit from
    /// </summary>
    internal abstract class ModuleBase : IArtifact, IBuildable, IModuleData, ITaskfileScope
    {
        protected readonly BuildConfig Config;
        protected readonly TaskFile TaskFile;

        ///<inheritdoc/>
        public TaskfileVars TaskVars { get; private set; }

        ///<inheritdoc/>
        public DirectoryInfo WorkingDir { get; }

        ///<inheritdoc/>
        public abstract string ModuleName { get; }

        ///<inheritdoc/>
        public ICollection<IProject> Projects { get; } = new LinkedList<IProject>();

        ///<inheritdoc/>
        public IModuleFileManager FileManager { get; }

        ///<inheritdoc/>
        public string? TaskfileName { get; protected set; } 

        /// <summary>
        /// The git repository of the module
        /// </summary>
        public Repository Repository { get; }

        /// <summary>
        /// The project explorer for the module
        /// </summary>
        public abstract IProjectExplorer ModuleExplorer { get; }

        public ModuleBase(BuildConfig config, DirectoryInfo root)
        {
            WorkingDir = root;
            Config = config;

            //Default to module taskfile name
            TaskfileName = config.ModuleTaskFileName;

            //Init repo for root working dir
            Repository = new(root.FullName);
            FileManager = new ModuleFileManager(config, this);
            TaskVars = null!;

            TaskFile = new(config.TaskExeName, () => ModuleName);
        }

        ///<inheritdoc/>
        public virtual async Task LoadAsync(TaskfileVars vars)
        {
            if(Repository?.Head?.Tip?.Sha is null)
            {
                throw new BuildStepFailedException("This repository does not have any commit history. Cannot continue");
            }

            //Store paraent vars
            TaskVars = vars;

            string moduleSemVer = this.GetModuleCiVersion(Config.DefaultCiVersion, Config.SemverStyle);

            //Build module local environment variables
            TaskVars.Set("MODULE_NAME", ModuleName);
            TaskVars.Set("OUTPUT_DIR", FileManager.OutputDir);
            TaskVars.Set("MODULE_DIR", WorkingDir.FullName);

            //Store current head-sha before update step
            TaskVars.Set("HEAD_SHA", Repository.Head.Tip.Sha);
            TaskVars.Set("BRANCH_NAME", Repository.Head.FriendlyName);
            TaskVars.Set("BUILD_VERSION", moduleSemVer);

            //Full path to module archive file
            TaskVars.Set("FULL_ARCHIVE_FILE_NAME", Path.Combine(WorkingDir.FullName, Config.SourceArchiveName));
            TaskVars.Set("ARCHIVE_FILE_NAME", Config.SourceArchiveName);
            TaskVars.Set("ARCHIVE_FILE_FORMAT", Config.SourceArchiveFormat);

            //Remove any previous projects
            Projects.Clear();

            Log.Information("Discovering projects in module {sln}", ModuleName);

            //Discover all projects in for the module
            foreach (IProject project in ModuleExplorer.DiscoverProjects())
            {
                //Store in collection
                Projects.Add(project);
            }

            //Load all projects
            await Projects.RunAllAsync(p => p.LoadAsync(TaskVars.Clone()));

            Log.Information("Sucessfully loaded {count} projects into module {sln}", Projects.Count, ModuleName);
            Log.Information("{modname} CI build SemVer will be {semver}", ModuleName, moduleSemVer);
        }

        ///<inheritdoc/>
        public virtual async Task DoStepSyncSource()
        {
            Log.Information("Checking for source code updates in module {mod}", ModuleName);

            //Do a git pull to update our sources
            await TaskFile.ExecCommandAsync(this, TaskfileComamnd.Update, true);

            //Set the latest commit sha after an update
            TaskVars.Set("HEAD_SHA", Repository.Head.Tip.Sha);

            //Update lastest build number
            TaskVars.Set("BUILD_VERSION", this.GetModuleCiVersion(Config.DefaultCiVersion, Config.SemverStyle));

            //Update module semver after source sync
            string moduleSemVer = this.GetModuleCiVersion(Config.DefaultCiVersion, Config.SemverStyle);
            TaskVars.Set("BUILD_VERSION", moduleSemVer);
            Log.Information("{modname} CI build SemVer will now be {semver}", ModuleName, moduleSemVer);
        }

        ///<inheritdoc/>
        public virtual async Task<bool> CheckForChangesAsync()
        {
            //Check source for updates
            await Projects.RunAllAsync(p => FileManager.CheckSourceChangedAsync(p, Config, Repository.Head.Tip.Sha));

            //Check if any project is not up-to-date
            return Projects.Any(static p => !p.UpToDate);
        }

        ///<inheritdoc/>
        public virtual async Task DoStepBuild()
        {
            //Clean the output dir
            FileManager.CleanOutput();
            //Recreate the output dir
            FileManager.CreateOutput();

            //Run taskfile to build
            await TaskFile.ExecCommandAsync(this, TaskfileComamnd.Build, true);

            //Run build for all projects
            foreach (IProject proj in Projects)
            {
                //Exec
                await TaskFile.ExecCommandAsync(proj, TaskfileComamnd.Build, true);
            }
        }

        ///<inheritdoc/>
        public virtual async Task DoStepPostBuild(bool success)
        {
            //Run taskfile postbuild, not required to produce a sucessful result
            await TaskFile.ExecCommandAsync(this, success ? TaskfileComamnd.PostbuildSuccess : TaskfileComamnd.PostbuildFailure, false);

            //Run postbuild for all projects
            foreach (IProject proj in Projects)
            {
                //Run postbuild for projects
                await TaskFile.ExecCommandAsync(proj, success ? TaskfileComamnd.PostbuildSuccess : TaskfileComamnd.PostbuildFailure, false);
            }

            //Run postbuild for all projects
            await Projects.RunAllAsync(async (p) =>
            {
                //If the operation was a success, commit the sum change
                if (success)
                {
                    Log.Verbose("Committing sum change for {sm}", p.ProjectName);
                    //Commit sum changes now that build has completed successfully
                    await FileManager.CommitSumChangeAsync(p);
                }
            });
        }

        ///<inheritdoc/>
        public virtual async Task DoStepPublish()
        {
            //Run taskfile postbuild, not required to produce a sucessful result
            await TaskFile.ExecCommandAsync(this, TaskfileComamnd.Publish, true);

            //Run postbuild for all projects
            foreach (IProject proj in Projects)
            {
                //Run postbuild for projects
                await TaskFile.ExecCommandAsync(proj, TaskfileComamnd.Publish, true);
            }
        }

        ///<inheritdoc/>
        public virtual async Task DoRunTests(bool failOnError)
        {
            //Run taskfile to build
            await TaskFile.ExecCommandAsync(this, TaskfileComamnd.Test, failOnError);

            //Run build for all projects
            foreach (IProject proj in Projects)
            {
                //Exec
                await TaskFile.ExecCommandAsync(proj, TaskfileComamnd.Test, failOnError);
            }
        }

        ///<inheritdoc/>
        public virtual async Task CleanAsync()
        {
            try
            {
                //Run taskfile to build
                await TaskFile.ExecCommandAsync(this, TaskfileComamnd.Clean, true);

                //Clean all projects
                foreach (IProject proj in Projects)
                {
                    //Clean the project output dir
                    await TaskFile.ExecCommandAsync(proj, TaskfileComamnd.Clean, true);
                }

                //Clean the output dir
                FileManager.CleanOutput();
            }
            catch (BuildStepFailedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new BuildStepFailedException("Failed to remove the module output directory", ex, ModuleName);
            }
        }

        public override string ToString() => ModuleName;


        public virtual void Dispose()
        {
            //Dispose the respository
            Repository.Dispose();

            //empty list
            Projects.Clear();
        }
    }
}