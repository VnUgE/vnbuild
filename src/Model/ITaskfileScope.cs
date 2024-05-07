using System.IO;

namespace VNLib.Tools.Build.Executor.Model
{
    public interface ITaskfileScope
    {
        /// <summary>
        /// The taskfile working directory
        /// </summary>
        DirectoryInfo WorkingDir { get; }

        /// <summary>
        /// The taskfile variable container
        /// </summary>
        TaskfileVars TaskVars { get; }

        /// <summary>
        /// The optional taskfile name
        /// </summary>
        string? TaskfileName { get; }
    }
}