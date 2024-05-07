using System.IO;
using System.Linq;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Modules
{
    internal sealed class GitCodeModule : ModuleBase
    {
        private string _moduleName;

        public override IProjectExplorer ModuleExplorer { get; }

        public override string ModuleName => _moduleName;

        public GitCodeModule(BuildConfig config, DirectoryInfo root) 
            : base(config, root)
        {
            ModuleExplorer = new MsBuildModuleExplorer(config, this, root);
            _moduleName = root.Name;    //Default to dir name
        }

        ///<inheritdoc/>
        public override Task LoadAsync(TaskfileVars vars)
        {
            //Try to load a solution file in the top-level module directory
            FileInfo? sln = WorkingDir.EnumerateFiles("*.sln", SearchOption.TopDirectoryOnly).FirstOrDefault();

            if(sln is not null)
            {
                //Remove the .build extension from the solution file name for proper module name
                _moduleName = Path.GetFileNameWithoutExtension(sln.Name).Replace(".build", string.Empty);

                vars.Set("SOLUTION_FILE_NAME", sln.Name);
            }

            return base.LoadAsync(vars);
        }
    }
}