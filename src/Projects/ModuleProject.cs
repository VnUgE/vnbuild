using System;
using System.IO;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Extensions;
using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Directories;

namespace VNLib.Tools.Build.Executor.Projects
{
    internal abstract class ModuleProject(ModuleConfig mod, ProjectConfig config, IDirectoryIndex index) : IProject
    {
        ///<inheritdoc/>
        public FileInfo ProjectFile { get; } = new(config.ProjectFilePath);

        ///<inheritdoc/>
        public abstract IProjectData ProjectData { get; }

        ///<inheritdoc/>
        public bool UpToDate { get; set; }

        ///<inheritdoc/>
        public DirectoryInfo WorkingDir { get; } = new(index.GetDirectory(VnbuildDir.Working, mod, config));

        ///<inheritdoc/>
        public TaskfileVars TaskVars { get; protected set; }

        ///<inheritdoc/>
        public ProjectConfig Config { get; } = config;

        ///<inheritdoc/>
        public string? TaskfileName { get; protected set; }

        /// <summary>
        /// Gets the package info file for the project
        /// </summary>
        protected virtual FileInfo? PackageInfoFile { get; } = new(config.ProjectFilePath);

        ///<inheritdoc/>
        public virtual async Task LoadAsync(TaskfileVars vars)
        {
            TaskVars = vars;

            await LoadProjectDom();

            //Set some local environment variables

            //Set local environment variables
            TaskVars.Set("BINARY_DIR", this.GetBinaryDirectory(mod));
            TaskVars.Set("SCRATCH_DIR", index.GetDirectory(VnbuildDir.Scratch, mod, Config));

            TaskVars.Set("PROJECT_NAME", config.ProjectName);
            TaskVars.Set("PROJECT_DIR", WorkingDir.FullName);
            TaskVars.Set("PROJECT_FILE", ProjectFile.FullName);
            TaskVars.Set("IS_PROJECT", bool.TrueString);

            //Store project vars
            TaskVars.Set("PROJ_VERSION", ProjectData.VersionString ?? string.Empty);
            TaskVars.Set("PROJ_DESCRIPTION", ProjectData.Description ?? string.Empty);
            TaskVars.Set("PROJ_AUTHOR", ProjectData.Authors ?? string.Empty);
            TaskVars.Set("PROJ_COPYRIGHT", ProjectData.Copyright ?? string.Empty);
            TaskVars.Set("PROJ_COMPANY", ProjectData.CompanyName ?? string.Empty);
            TaskVars.Set("RPOJ_URL", ProjectData.RepoUrl ?? string.Empty);

            TaskVars.Set("SAFE_PROJ_NAME", this.GetSafeProjectName());
        }

        /// <summary>
        /// Loads the project's XML dom from its msbuild project file
        /// </summary>
        /// <returns>A task that resolves when the dom is built</returns>
        public async Task LoadProjectDom()
        {
            using MemoryStream ms = new();

            FileInfo dom = ProjectFile;

            if (PackageInfoFile?.Exists == true)
            {
                dom = PackageInfoFile;
            }

            try
            {
                //Get the project file
                await using (FileStream projData = dom.OpenRead())
                {
                    await projData.CopyToAsync(ms);
                }

                //reset stream
                ms.Seek(0, SeekOrigin.Begin);

                //Load the project dom
                ProjectData.Load(ms);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load project dom file {dom.FullName}", ex);
            }
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        { }

        /// <inheritdoc/>
        public override string ToString() => config.ProjectName;
    }
}