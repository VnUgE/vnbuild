using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using LibGit2Sharp;

using Semver;

using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Extensions
{
    internal static class BuildExtensions
    {

        /// <summary>
        /// Determines if the file exists within the current directory
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="fileName">The name of the file to search for</param>
        /// <returns>True if the file exists, false otherwise</returns>
        public static bool FileExists(this DirectoryInfo dir, string fileName) => File.Exists(Path.Combine(dir.FullName, fileName));

        /// <summary>
        /// Determines if a child directory exists
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="dirName">The name of the directory to check for</param>
        /// <returns>True if the directory exists</returns>
        public static bool ChildExists(this DirectoryInfo dir, string dirName) => Directory.Exists(Path.Combine(dir.FullName, dirName));

        /// <summary>
        /// Deletes a child directory
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="dirName">The name of the directory to delete</param>
        /// <param name="recurse">Recursive delete, delete all child items</param>
        public static void DeleteChild(this DirectoryInfo dir, string dirName, bool recurse = true) => Directory.Delete(Path.Combine(dir.FullName, dirName), recurse);

        /// <summary>
        /// Creates a child directory within the current directory
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="name">The name of the child directory</param>
        public static DirectoryInfo CreateChild(this DirectoryInfo dir, string name) => Directory.CreateDirectory(Path.Combine(dir.FullName, name));

        /// <summary>
        /// Computes the SHA256 hash of the current file and writes the hash to 
        /// a filename.sha256 hexadecimal text file
        /// </summary>
        /// <param name="file"></param>
        /// <returns>A task the completes when the file hash has been produced in the output directory</returns>
        public static async Task ComputeFileHashAsync(this FileInfo file, string hashName)
        {
            string outputName = $"{file.FullName}.{hashName}";

            //convert the hash to hexadecimal
            string hex = await ComputeFileHashStringAsync(file);

            //Write the hex hash to the output file
            await File.WriteAllTextAsync(outputName, hex);
        }

        /// <summary>
        /// Computes the SHA256 hash of the current file and returns the file hash as a hexadecimal string
        /// </summary>
        /// <param name="file"></param>
        /// <returns>A task the completes when the file hash has been produced in the output directory</returns>
        public static async Task<string> ComputeFileHashStringAsync(this FileInfo file)
        {
            using SHA256 alg = SHA256.Create();

            //Open the output file to read the file data to compute hash
            await using FileStream input = file.OpenRead();

            //Compute hash
            byte[] hash = await alg.ComputeHashAsync(input);

            //convert the hash to hexadecimal
            return Convert.ToHexString(hash);
        }


        public static Task RunAllAsync<T>(this IEnumerable<T> workCol, Func<T, Task> cb)
        {
            Task[] tasks = workCol.Select(cb).ToArray();
            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Gets the module's version based on the latest tag and the number of commits since the last tag
        /// that supports pre-release/semver
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="style">The version style</param>
        /// <returns>The ci build/version number</returns>
        public static string GetModuleCiVersion(this IModuleData mod, string defaultCiVersion, SemVersionStyles style)
        {
            int ciNumber = 0;
            SemVersion baseVersion;

            //Get latest version tag from git
            Tag? vTag = mod.Repository.Tags.OrderByDescending(p => SemVersion.Parse(p.FriendlyName, style)).FirstOrDefault();

            //Find the number of commits since the last tag
            if (vTag != null)
            {
                //Get the number of commits since the last tag
                baseVersion = SemVersion.Parse(vTag.FriendlyName, style);

                //Search through commits till we can find the commit that matches the tag
                Commit[] commits = mod.Repository.Commits.ToArray();

                for (; ciNumber < commits.Length; ciNumber++)
                {
                    if (commits[ciNumber].Sha == vTag.Target.Sha)
                    {
                        break;
                    }
                }
            }
            else
            {
                //No tags, so just use the number of commits
                ciNumber = mod.Repository.Commits.Count() - 1;
                baseVersion = SemVersion.Parse(defaultCiVersion, style);
            }

            //If there are commits, then increment the version prerelease
            if (ciNumber > 0)
            {
                //Increment the version
                baseVersion = baseVersion.WithPrerelease($"ci{ciNumber:0000}");
            }

            return baseVersion.ToString();
        }

        public static bool IsFileIgnored(this Repository repo, string file)
        {
            FileStatus status = repo.RetrieveStatus(file);

            //If the leaf project is ignored, skip it
            return status.HasFlag(FileStatus.Ignored) || status.HasFlag(FileStatus.Nonexistent);
        }
    }
}