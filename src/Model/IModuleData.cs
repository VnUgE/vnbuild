using System.Collections.Generic;

using LibGit2Sharp;

using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Model
{
    public interface IModuleData
    {
        ICollection<IProject> Projects { get; }

        ModuleConfig Config { get; }

        Repository Repository { get; }

        TaskfileVars TaskVars { get; }

        IModuleFileManager FileManager { get; }

        string GetVersionString();
    }
}