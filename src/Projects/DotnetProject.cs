
using System;
using System.IO;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Directories;
using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Projects
{

    internal sealed class DotnetProject(ModuleConfig mod, ProjectConfig config, IDirectoryIndex index) 
        : ModuleProject(mod, config, index)
    {
        public override IProjectData ProjectData { get; } = new DotnetProjectDom();

        ///<inheritdoc/>
        public override async Task LoadAsync(TaskfileVars vars)
        {
            //Load project dom
            await base.LoadAsync(vars);
          
            //Set .NET specific vars
            TaskVars.Set("TARGET_FRAMEWORK", ProjectData["TargetFramework"] ?? string.Empty);
            TaskVars.Set("PROJ_ASM_NAME", ProjectData["AssemblyName"] ?? string.Empty);
        }
    }
}