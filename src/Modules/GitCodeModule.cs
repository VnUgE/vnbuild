using System.IO;
using System.Linq;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Directories;

namespace VNLib.Tools.Build.Executor.Modules
{
    internal sealed class GitCodeModule(BuildConfig build, ModuleConfig mod, IDirectoryIndex dirs) 
        : ModuleBase(build, mod, dirs)
    {
       
    }
}