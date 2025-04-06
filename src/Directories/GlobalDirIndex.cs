using System;
using System.IO;

using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Directories
{

    public sealed class GlobalDirIndex(BuildConfig config) : IDirectoryIndex
    {
        /// <inheritdoc/>
        public string GetDirectory(VnbuildDir dir)
        {
            string buildDir = config.BuildDirectory;

            //If the build directory is relative, make it relative to the working directory
            if (!Path.IsPathRooted(config.BuildDirectory))
            {
                buildDir = Path.Combine(config.WorkingDirectory, config.BuildDirectory);    
            }

            buildDir = Path.GetFullPath(buildDir);

            return dir switch
            {
                VnbuildDir.Working      => Path.GetFullPath(config.WorkingDirectory),
                VnbuildDir.Build        => buildDir,
                VnbuildDir.Scratch      => Path.Combine(buildDir, "scratch"),
                VnbuildDir.Output       => Path.Combine(buildDir, "output"),
                VnbuildDir.Sum          => Path.Combine(buildDir, "sum"),
                _ => throw new ArgumentException($"Invalid directory path argument: {dir}", nameof(dir)),
            };
        }
    }
}