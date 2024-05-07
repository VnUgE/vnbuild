using System.IO;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Projects
{
    internal sealed class LeafProject(BuildConfig config, FileInfo projectFile) : ModuleProject(projectFile)
    {
        ///<inheritdoc/>
        public override IProjectData ProjectData { get; } = new NativeProjectDom();

        ///<inheritdoc/>
        protected override FileInfo? PackageInfoFile => new(Path.Combine(WorkingDir.FullName, "package.json"));

        public override async Task LoadAsync(TaskfileVars vars)
        {
            await base.LoadAsync(vars);

            //Set the project name to the product name if set, otherwise use the working dir name
            ProjectName = ProjectData.Product ?? WorkingDir.Name;

            //Get the binary dir from the project file, or use the default
            string? binaryDir = ProjectData["output_dir"] ?? ProjectData["output"] ?? config.ProjectBinDir;

            //Overide the project name from the pacakge file if set
            TaskVars.Set("PROJECT_NAME", ProjectName);
            TaskVars.Set("BINARY_DIR", binaryDir);
        }

        public override string ToString() => ProjectName;

        public override void Dispose()
        { }
    }
}