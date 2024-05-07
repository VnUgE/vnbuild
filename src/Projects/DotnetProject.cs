
using System;
using System.IO;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Projects
{

    internal sealed class DotnetProject : ModuleProject
    {
        public override IProjectData ProjectData { get; }


        public DotnetProject(FileInfo projectFile, string projectName):base(projectFile, projectName)
        {
            //Create the porject dom
            ProjectData = new DotnetProjectDom();
        }     

        ///<inheritdoc/>
        public override async Task LoadAsync(TaskfileVars vars)
        {
            //Load project dom
            await base.LoadAsync(vars);
          
            //Set .NET specific vars
            TaskVars.Set("TARGET_FRAMEWORK", ProjectData["TargetFramework"] ?? string.Empty);
            TaskVars.Set("PROJ_ASM_NAME", ProjectData["AssemblyName"] ?? string.Empty);
        }

        public override string ToString() => ProjectName;

        public override void Dispose()
        { }
    }
}