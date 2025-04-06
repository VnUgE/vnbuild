using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using LibGit2Sharp;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Extensions;
using VNLib.Tools.Build.Executor.Directories;
using VNLib.Tools.Build.Executor.Versioning;

namespace VNLib.Tools.Build.Executor.Modules
{
    /// <summary>
    /// Represents a base class for all modules to inherit from
    /// </summary>
    internal abstract class ModuleBase(BuildConfig build, ModuleConfig config, IDirectoryIndex dirIndex) 
        : IArtifact, IBuildable, IModuleData, ITaskfileScope
    {
        private readonly GitVersionDecoder decoder = new(build.GitversionToolPath);
        private readonly ConditionalWeakTable<IProject, object> _projectHashes = new();
        protected readonly IDirectoryIndex DirIndex = dirIndex;
        protected readonly TaskFile TaskFile = new(build, config);

        private GitVersion? _version;

        ///<inheritdoc/>
        public TaskfileVars TaskVars { get; private set; } = null!;

        ///<inheritdoc/>
        public virtual string ModuleName { get; } = config.ModuleName;

        ///<inheritdoc/>
        public virtual ModuleConfig Config { get; } = config;

        ///<inheritdoc/>
        public ICollection<IProject> Projects { get; } = new LinkedList<IProject>();

        ///<inheritdoc/>
        public IModuleFileManager FileManager { get; private set; } = null!;

        /// <inheritdoc/>
        public string GetVersionString() 
            => _version?.LegacySemVerPadded ?? throw new InvalidOperationException("Version not loaded");

        /// <summary>
        /// The git repository of the module
        /// </summary>
        public Repository Repository { get; } = new(dirIndex.GetDirectory(VnbuildDir.Working, config));

        private void SetVersionVars(GitVersion version)
        {
            TaskVars.Set("SAFE_BRANCH_NAME", version.EscapedBranchName);
            TaskVars.Set("BRANCH_NAME", version.BranchName);
            TaskVars.Set("HEAD_SHA", version.Sha);
            TaskVars.Set("BUILD_VERSION", version.LegacySemVerPadded);
            TaskVars.Set("VERSION", version.LegacySemVerPadded);
            TaskVars.Set("SEMVER", version.FullSemVer);
            TaskVars.Set("ASSEMBLY_SEMVER", version.AssemblySemVer);
            TaskVars.Set("VERSION_MAJOR", version.Major.ToString());
            TaskVars.Set("VERSION_MINOR", version.Minor.ToString());
            TaskVars.Set("VERSION_PATCH", version.Patch.ToString());
            TaskVars.Set("VERSION_BUILD", version.PreReleaseLabel);
            TaskVars.Set("VERSION_CI_NUMBER", $"{version.PreReleaseNumber:0000}");
        }

        ///<inheritdoc/>
        public virtual async Task LoadAsync(TaskfileVars vars)
        {
            if(Repository?.Head?.Tip?.Sha is null)
            {
                throw new BuildStepFailedException("This repository does not have any commit history. Cannot continue");
            }

            FileManager = new ModuleFileManager(build, this);

            //Store paraent vars
            TaskVars = vars;

            //Assign user-defined variables
            foreach (KeyValuePair<string, string> pair in config.TaskVars)
            {
                TaskVars.Set(pair.Key, pair.Value);
            }

            //Load the module version
            _version = await decoder.GetVersionInfoAsync(
                DirIndex.GetDirectory(VnbuildDir.Working, Config), 
                token: default
            );

            SetVersionVars(_version);

            //Build module local environment variables
            TaskVars.Set("MODULE_NAME", ModuleName);
            TaskVars.Set("OUTPUT_DIR", DirIndex.GetDirectory(VnbuildDir.Output, Config));
            TaskVars.Set("MODULE_DIR", DirIndex.GetDirectory(VnbuildDir.Working, Config));
            TaskVars.Set("SCRATCH_DIR", DirIndex.GetDirectory(VnbuildDir.Scratch, Config));

            //Full path to module archive file
            TaskVars.Set("FULL_ARCHIVE_FILE_NAME", Path.Combine(DirIndex.GetDirectory(VnbuildDir.Working, Config), Config.SourceArchiveName));
            TaskVars.Set("ARCHIVE_FILE_NAME", Config.SourceArchiveName);
            TaskVars.Set("ARCHIVE_FILE_FORMAT", Config.SourceArchiveFormat);

            //Remove any previous projects
            Projects.Clear();

            build.Log.Information("Discovering projects in module {sln}", ModuleName);

            //Discover all projects in for the module
            foreach (IProject project in BuildFileExplorer.DiscoverProjects(build.Log, Config, Repository, DirIndex))
            {
                //Store in collection
                Projects.Add(project);
            }

            //Load all projects
            await Projects.RunAllAsync(p => p.LoadAsync(TaskVars.Clone()));

            build.Log.Information("Sucessfully loaded {count} projects into module {sln}", Projects.Count, ModuleName);
            build.Log.Information("{modname} CI build SemVer will be {semver}", ModuleName, _version.LegacySemVerPadded);
        }

        ///<inheritdoc/>
        public virtual async Task DoStepSyncSource()
        {
            build.Log.Information("Checking for source code updates in module {mod}", ModuleName);

            //Do a git pull to update our sources
            await TaskFile.ExecCommandAsync(this, TaskfileComamnd.Update, throwIfFailed: true);

            //Load the module version
            _version = await decoder.GetVersionInfoAsync(
                DirIndex.GetDirectory(VnbuildDir.Working, Config),
                token: default
            );

            SetVersionVars(_version);

            if (build.Log.IsEnabled(Serilog.Events.LogEventLevel.Verbose))
            {
                build.Log.Verbose("{modname} full version information:\n{version}", _version);
            }
            else
            {
                build.Log.Information("{modname} CI build SemVer will now be {semver}", ModuleName, _version.LegacySemVerPadded);
            }
        }

        ///<inheritdoc/>
        public virtual async Task<bool> CheckForChangesAsync()
        {
            //Check source for updates
            await Projects.RunAllAsync(p => CheckSourceChangedAsync(p, Repository.Head.Tip.Sha));

            //Check if any project is not up-to-date
            return Projects.Any(static p => !p.UpToDate);
        }
      
        ///<inheritdoc/>
        public virtual async Task DoStepBuild()
        {
            //Remove and recreate the output directory for the module
            string outputDir = DirIndex.GetDirectory(VnbuildDir.Output, Config);
            if (Directory.Exists(outputDir))
            {
                build.Log.Verbose("Removing module output directory");
                Directory.Delete(outputDir, true);
            }

            Directory.CreateDirectory(outputDir);
            build.Log.Verbose("Created module output directory");

            //Run taskfile to build
            await TaskFile.ExecCommandAsync(this, TaskfileComamnd.Build, throwIfFailed: true);

            //Run build for all projects
            foreach (IProject proj in Projects)
            {
                await BuildSingleProject(proj);
            }
        }

        /// <summary>
        /// Builds a single project within this module
        /// </summary>
        /// <param name="project">The project instance to build</param>
        /// <returns>A task that resolves when the build operation completes</returns>
        public virtual async Task BuildSingleProject(IProject project)
        {
            //Delete delete the project output directory
            DirectoryInfo dir = FileManager.GetArtifactOutputDir(project);
            if (dir.Exists)
            {
                dir.Delete(true);
            }

            //Create the project binary directory if it doesn't exist
            string projDir = DirIndex.GetDirectory(VnbuildDir.Working, Config, project.Config);
            string binDir = Path.Combine(projDir, project.GetBinaryDirectory(Config));
            Directory.CreateDirectory(binDir);

            await TaskFile.ExecCommandAsync(project, TaskfileComamnd.Build, throwIfFailed: true);
        }

        /// <summary>
        /// Builds a single project within this module
        /// </summary>
        /// <param name="project">The project instance to build</param>
        /// <returns>A task that resolves when the build operation completes</returns>
        public virtual async Task PostbuildSingleProject(IProject project, bool success)
        {
            TaskfileComamnd cmd = success
                ? TaskfileComamnd.PostbuildSuccess
                : TaskfileComamnd.PostbuildFailure;

            //Run taskfile postbuild, not required to produce a sucessful result
            await TaskFile.ExecCommandAsync(project, cmd, throwIfFailed: success);
        }

        ///<inheritdoc/>
        public virtual async Task DoStepPostBuild(bool success)
        {
            TaskfileComamnd cmd = success
                ? TaskfileComamnd.PostbuildSuccess
                : TaskfileComamnd.PostbuildFailure;

            //Run taskfile postbuild, not required to produce a sucessful result
            await TaskFile.ExecCommandAsync(this, cmd, throwIfFailed: success);

            Task[] projPostbuild = Projects
                .Select(p => TaskFile.ExecCommandAsync(p, cmd, throwIfFailed: success))
                .ToArray();

            try
            {
                await Task.WhenAll(projPostbuild);
            }
            catch(Exception ex)
            {
                throw new BuildStepFailedException(
                    message: "Failed to run postbuild steps",
                    ex,
                    name: ModuleName
                );
            }

            //Run postbuild for all projects
            await Projects.RunAllAsync(async (p) =>
            {
                //If the operation was a success, commit the sum change
                if (success)
                {
                    build.Log.Verbose("Committing sum change for {sm}", p.Config.ProjectName);
                    //Commit sum changes now that build has completed successfully
                    await CommitSumChangeAsync(p);
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
                await TaskFile.ExecCommandAsync(proj, TaskfileComamnd.Test, failOnError);
            }
        }

        ///<inheritdoc/>
        public virtual async Task CleanAsync()
        {
            try
            {
                //Run taskfile to build
                await TaskFile.ExecCommandAsync(this, TaskfileComamnd.Clean, throwIfFailed: true);

                //Clean all projects
                foreach (IProject proj in Projects)
                {
                    //Clean the project output dir
                    await TaskFile.ExecCommandAsync(proj, TaskfileComamnd.Clean, throwIfFailed: true);
                }

                //Clean module output 
                string outputDir = DirIndex.GetDirectory(VnbuildDir.Output, Config);
                if (Directory.Exists(outputDir))
                {
                    Directory.Delete(outputDir, true);
                }
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

        /// <summary>
        /// Writes the source file checksum change to the project's sum file
        /// </summary>
        /// <param name="project"></param>
        /// <param name="project">The project to write the checksum for</param>
        /// <returns>A task that resolves when the sum file has been updated</returns>
        private Task CommitSumChangeAsync(IProject project)
        {
            if (!_projectHashes.TryGetValue(project, out object? sumChange))
            {
                return Task.CompletedTask;
            }

            byte[] sumData = JsonSerializer.SerializeToUtf8Bytes(sumChange);

            return FileManager.WriteChecksumAsync(project, sumData);
        }

        private async Task CheckSourceChangedAsync(IProject project, string commit)
        {
            //Compute current sum
            string sum = await project.GetSourceFileHashAsync(config, DirIndex);

            //Old sum file exists
            byte[]? sumData = await FileManager.ReadCheckSumAsync(project);

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
                    build.Log.Verbose("Project {p} source is {up}", project.Config.ProjectName, "up-to-date");

                    //Project source is up-to-date
                    project.UpToDate = true;

                    //No changes made
                    return;
                }
            }

            build.Log.Verbose("Project {p} source is {up}", project.Config.ProjectName, "changed");

            //Store sum change
            object sumChange = new
            {
                sum,
                commit,
                modified = DateTimeOffset.UtcNow.ToString("s")
            };

            //Store sum change for later
            _projectHashes.Add(project, sumChange);

            project.UpToDate = false;
        }


        ///<inheritdoc/>
        string? ITaskfileScope.TaskfileName { get; } = config.ModuleTaskFileName;

        ///<inheritdoc/>
        DirectoryInfo ITaskfileScope.WorkingDir { get; } = new(dirIndex.GetDirectory(VnbuildDir.Working, config));

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