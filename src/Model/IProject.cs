using System.IO;
using System.Threading.Tasks;

namespace VNLib.Tools.Build.Executor.Model
{
    public interface IProject : ITaskfileScope
    {
        /// <summary>
        /// Gets the the project file
        /// </summary>
        FileInfo ProjectFile { get; }

        /// <summary>
        /// Gets the actual project name
        /// </summary>
        string ProjectName { get; }

        /// <summary>
        /// The msbuild project dom
        /// </summary>
        IProjectData ProjectData { get; }

        /// <summary>
        /// A value that indicates (after a source sync) that the project 
        /// is considered up to date.
        /// </summary>
        bool UpToDate { get; set; }

        /// <summary>
        /// Invoked when the executor requests all created artifacts load async assets
        /// and update state accordingly
        /// </summary>
        /// <param name="vars">The taskfile variable container</param>
        /// <returns>A task that completes when all assets are loaded</returns>
        Task LoadAsync(TaskfileVars vars);
    }
}