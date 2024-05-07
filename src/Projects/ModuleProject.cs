using System;
using System.IO;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Extensions;

namespace VNLib.Tools.Build.Executor.Projects
{
    internal abstract class ModuleProject : IProject
    {
        ///<inheritdoc/>
        public FileInfo ProjectFile { get; }

        ///<inheritdoc/>
        public string ProjectName { get; protected set; }

        ///<inheritdoc/>
        public abstract IProjectData ProjectData { get; }

        ///<inheritdoc/>
        public bool UpToDate { get; set; }

        ///<inheritdoc/>
        public DirectoryInfo WorkingDir { get; protected set; }
        
        ///<inheritdoc/>
        public TaskfileVars TaskVars { get; protected set; }

        ///<inheritdoc/>
        public string? TaskfileName { get; protected set; }

        /// <summary>
        /// Gets the package info file for the project
        /// </summary>
        protected virtual FileInfo? PackageInfoFile { get; }

        public ModuleProject(FileInfo projectFile, string? projectName = null)
        {
            ProjectFile = projectFile;

            //Default project name to the file name
            ProjectName = projectName ?? Path.GetFileNameWithoutExtension(ProjectFile.Name);

            //Default up-to-date false
            UpToDate = false;

            //Default working dir to the project file's directory
            WorkingDir = ProjectFile.Directory!;

            TaskVars = null!;
        }
       
        ///<inheritdoc/>
        public virtual async Task LoadAsync(TaskfileVars vars)
        {
            TaskVars = vars;

            await LoadProjectDom();

            //Set some local environment variables

            //Set local environment variables
            TaskVars.Set("PROJECT_NAME", ProjectName);
            TaskVars.Set("PROJECT_DIR", WorkingDir.FullName);
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

        public abstract void Dispose();

        public override string ToString() => ProjectName;
    }
}