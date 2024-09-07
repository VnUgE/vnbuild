
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Extensions
{

    internal static class ProjectExtensions
    {

        /// <summary>
        /// Gets the project dependencies for the given project
        /// </summary>
        /// <param name="project"></param>
        /// <returns>The list of project dependencies</returns>
        public static string[] GetDependencies(this IProject project)
        {
            //Get the project file names (not paths) that are dependencies
            return project.ProjectData.GetProjectRefs().Select(static r => Path.GetFileName(r)).ToArray();
        }

        private static bool IsSourceFile(BuildConfig conf, string fileName)
        {
            for (int i = 0; i < conf.SourceFileEx.Length; i++)
            {
                if (fileName.EndsWith(conf.SourceFileEx[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsExcludedDir(BuildConfig conf, string path)
        {
            for (int i = 0; i < conf.ExcludedSourceDirs.Length; i++)
            {
                if (path.Contains(conf.ExcludedSourceDirs[i]))
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<FileInfo> GetProjectBuildFiles(this IProject project, BuildConfig config)
        {
            //See if an output dir is specified
            string? outDir = project.ProjectData["output_dir"] ?? project.ProjectData["output"];

            //If an output dir is specified, only get files from that dir
            if(!string.IsNullOrWhiteSpace(outDir))
            {
                //realtive file path
                outDir = Path.Combine(project.WorkingDir.FullName, outDir);

                if (Directory.Exists(outDir))
                {
                    return new DirectoryInfo(outDir)
                        .EnumerateFiles(config.OutputFileType, SearchOption.TopDirectoryOnly);
                }
            }

            return project.WorkingDir.EnumerateFiles(config.OutputFileType, SearchOption.AllDirectories);
        }

        /// <summary>
        /// Gets the sha256 hash of all the source files within the project
        /// </summary>
        /// <returns>A task that resolves the hexadecimal string of the sha256 hash of all the project source files</returns>
        public static async Task<string> GetSourceFileHashAsync(this IProject project, BuildConfig config)
        {
            //Get all 
            FileInfo[] sourceFiles = project.WorkingDir!.EnumerateFiles("*.*", SearchOption.AllDirectories)
                    //Get all source files, c/c#/c++ source files, along with .xproj files (project files)
                    .Where(n => IsSourceFile(config, n.Name))
                    //Exclude the obj intermediate output dir
                    .Where(f => !IsExcludedDir(config, f.DirectoryName ?? ""))
                    .ToArray();

            //Get a scratch file to append file source code to
            await using FileStream scratch = new(
                $"{config.Index.ScratchDir.FullName}/{Path.GetRandomFileName()}", 
                FileMode.OpenOrCreate, 
                FileAccess.ReadWrite, 
                FileShare.None, 
                8192, 
                FileOptions.DeleteOnClose
            );

            //Itterate over all source files
            foreach (FileInfo sourceFile in sourceFiles)
            {
                //Open the source file stream
                await using FileStream source = sourceFile.OpenRead();

                //Append the data to the file stream
                await source.CopyToAsync(scratch);
            }

            //Flush the stream to disk and roll back to start
            await scratch.FlushAsync();
            scratch.Seek(0, SeekOrigin.Begin);

            byte[] hash;

            //Create a sha 256 hash of the file
            using (SHA256 alg = SHA256.Create())
            {
                //Hash the file
                hash = await alg.ComputeHashAsync(scratch);
            }

            //Get hex of the hash
            return Convert.ToHexString(hash);
        }

        public static string GetSafeProjectName(this IProject project)
        {
            return project.ProjectName
                .Replace('/', '-')
                .Replace('\\','-');
        }
    }
}