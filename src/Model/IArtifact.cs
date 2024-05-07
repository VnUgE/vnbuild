using System;
using System.Threading.Tasks;

namespace VNLib.Tools.Build.Executor.Model
{
    internal interface IArtifact : IDisposable
    {
        /// <summary>
        /// Invoked when the executor requests all created artifacts load async assets
        /// and update state accordingly
        /// </summary>
        /// <param name="vars">The taskfile variable container</param>
        /// <returns>A task that completes when all assets are loaded</returns>
        Task LoadAsync(TaskfileVars vars);

        /// <summary>
        /// Invoked when the executor requests all artifacts cleanup assets that 
        /// may have been generated during a build process
        /// </summary>
        /// <returns>A task that completes when all assest are cleaned</returns>
        Task CleanAsync();
    }
}