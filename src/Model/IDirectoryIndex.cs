using System.IO;

namespace VNLib.Tools.Build.Executor.Model
{
    public interface IDirectoryIndex
    {
        /// <summary>
        /// The current build base directory
        /// </summary>
        DirectoryInfo BaseDir { get; }

        /// <summary>
        /// The top level internal build directory
        /// </summary>
        DirectoryInfo BuildDir { get; }

        /// <summary>
        /// The directory where log files are stored
        /// </summary>
        DirectoryInfo LogDir { get; }

        /// <summary>
        /// Gets the build scratch directory
        /// </summary>
        DirectoryInfo ScratchDir { get; }

        /// <summary>
        /// Gets the build checksum directory, used to store source file sums
        /// </summary>
        DirectoryInfo SumDir { get; }

        /// <summary>
        /// The build output directory
        /// </summary>
        DirectoryInfo OutputDir { get; }
    }
}