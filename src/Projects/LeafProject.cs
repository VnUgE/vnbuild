using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Directories;

namespace VNLib.Tools.Build.Executor.Projects
{
    internal sealed class LeafProject(ModuleConfig mod, ProjectConfig proj, IDirectoryIndex dirs) 
        : ModuleProject(mod, proj, dirs)
    {
        ///<inheritdoc/>
        public override IProjectData ProjectData { get; } = new NativeProjectDom();

        public override async Task LoadAsync(TaskfileVars vars)
        {
            await base.LoadAsync(vars);

            //Overwrite project name with the name from the project dom
            proj.ProjectName = ProjectData["name"] ?? proj.ProjectName;

            TaskVars.Set("PROJECT_NAME", proj.ProjectName);
        }
    }
}