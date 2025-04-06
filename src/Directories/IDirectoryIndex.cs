using LibGit2Sharp;

using System;
using System.IO;

using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Directories
{

    /// <summary>
    /// Interface for directory index operations.
    /// </summary>
    public interface IDirectoryIndex
    {
        /// <summary>
        /// Gets the internal directory for the given type.
        /// </summary>
        /// <param name="dir">The internal directory identifier.</param>
        /// <returns>The directory for the desired internal dir type.</returns>
        string GetDirectory(VnbuildDir dir);

        /// <summary>
        /// Gets the directory for the given type and module.
        /// </summary>
        /// <param name="dir">The internal directory identifier.</param>
        /// <param name="mod">The module configuration.</param>
        /// <returns>The directory for the desired internal dir type and module.</returns>
        /// <exception cref="NotImplementedException">Thrown when the directory type is not implemented.</exception>
        public virtual string GetDirectory(VnbuildDir dir, ModuleConfig mod)
        {
            return dir switch
            {
                VnbuildDir.Working      => Path.GetFullPath(mod.ModuleDirectory),
                VnbuildDir.Build        => CombinedPath(this, dir, mod.ModuleName),
                VnbuildDir.Scratch      => CombinedPath(this, dir, mod.ModuleName),
                VnbuildDir.Output       => CombinedPath(this, dir, mod.ModuleName),
                VnbuildDir.DotGit       => CombinedPath(this, VnbuildDir.Working, ".git"),  //.git should be local to the module
                VnbuildDir.Sum          => CombinedPath(this, dir, mod.ModuleName),
                _ => throw new NotImplementedException(),
            };

            static string CombinedPath(IDirectoryIndex index, VnbuildDir dir, string path)
            {
                return Path.Combine(index.GetDirectory(dir), path);
            }
        }

        ///<inheritdoc/>
        public virtual string GetDirectory(VnbuildDir dir, ModuleConfig mod, ProjectConfig proj)
        {
            return dir switch
            {
                VnbuildDir.Working      => Path.GetFullPath(proj.ProjectDirectory),
                VnbuildDir.Build        => CombinedPath(dir, proj.ProjectName),
                VnbuildDir.Scratch      => CombinedPath(dir, proj.ProjectName),
                VnbuildDir.Output       => CombinedPath(dir, proj.ProjectName),
                VnbuildDir.DotGit       => CombinedPath(dir, string.Empty),         //Only 1 git dir for the the entire module
                VnbuildDir.Sum          => CombinedPath(dir, proj.ProjectName),

                _ => throw new NotImplementedException(),
            };

            string CombinedPath(VnbuildDir dir, string path)
            {
                //Use module scope when getting the directory
                return Path.Combine(GetDirectory(dir, mod), path);
            }
        }
    }
}
