using System.Collections.Generic;

using LibGit2Sharp;

namespace VNLib.Tools.Build.Executor.Model
{
    public interface IModuleData
    {
        ICollection<IProject> Projects { get; }

        string ModuleName { get; }

        Repository Repository { get; }

        TaskfileVars TaskVars { get; }

        IModuleFileManager FileManager { get; }
    }
}