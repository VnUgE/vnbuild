using System.IO;
using System.Threading.Tasks;

namespace VNLib.Tools.Build.Executor.Model
{
    public enum ModuleFileType
    {
        None,
        Catalog,
        GitHistory,
        Checksum,
        LatestHash,
        VersionHistory,
        Archive
    }

    public interface IModuleFileManager
    {
        /// <summary>
        /// Writes the file to the module output directory
        /// </summary>
        /// <param name="type">The <see cref="ModuleFileType"/> to write</param>
        /// <param name="fileData">The file data to write</param>
        /// <returns>A task that resolves when the file has been written</returns>
        Task<FileInfo> WriteFileAsync(ModuleFileType type, byte[] fileData);

        /// <summary>
        /// Writes the checksum file to the sum's output directory for the given project
        /// </summary>
        /// <param name="project">The project to write the sum file data for</param>
        /// <param name="fileData"></param>
        /// <returns></returns>
        Task WriteChecksumAsync(IProject project, byte[] fileData);

        /// <summary>
        /// Attemts to read the checksum file data for the given project
        /// </summary>
        /// <param name="project">The project to get the sum data for</param>
        /// <returns>The file contents of the sum file, or null if the file does not exist</returns>
        Task<byte[]?> ReadCheckSumAsync(IProject project);

        /// <summary>
        /// The module's output directory
        /// </summary>
        string OutputDir { get; }

        /// <summary>
        /// Copies the given file to the project's output directory
        /// </summary>
        /// <param name="project"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        Task CopyArtifactToOutputAsync(IProject project, FileInfo file);

        /// <summary>
        /// Gets the output directory for the given project
        /// </summary>
        /// <param name="project">The project to get the artifact output of</param>
        /// <returns>A <see cref="DirectoryInfo"/> object describing the output dir</returns>
        DirectoryInfo GetArtifactOutputDir(IProject project);
    }
}