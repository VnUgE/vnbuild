using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using LibGit2Sharp;

namespace VNLib.Tools.Build.Executor.Extensions
{
    internal static class BuildExtensions
    {

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
     

        public static bool IsFileIgnored(this Repository repo, string file)
        {
            FileStatus status = repo.RetrieveStatus(file);

            //If the leaf project is ignored, skip it
            return status.HasFlag(FileStatus.Ignored) || status.HasFlag(FileStatus.Nonexistent);
        }
    }
}