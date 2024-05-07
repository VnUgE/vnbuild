using System;
using System.IO;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Extensions;
using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Modules
{

    public sealed class ModuleFileManager(BuildConfig config, IModuleData ModData) : IModuleFileManager
    {
        private readonly IDirectoryIndex Index = config.Index;

        ///<inheritdoc/>
        public string OutputDir => Path.Combine(Index.OutputDir.FullName, ModData.ModuleName);

        ///<inheritdoc/>
        public async Task CopyArtifactToOutputAsync(IProject project, FileInfo file)
        {
            string targetDir = GetProjectTargetDir(project);

            //Project artifacts are versioned by the latest git commit hash
            string outputFile = Path.Combine(targetDir, file.Name);

            //Create the target directory if it doesn't exist
            Directory.CreateDirectory(targetDir);

            //Copy the file to the output directory
            FileInfo output = file.CopyTo(outputFile, true);

            //Compute the file hash of the new output file
            await output.ComputeFileHashAsync(config.HashFuncName);
        }

        ///<inheritdoc/>
        public DirectoryInfo GetArtifactOutputDir(IProject project)
        {
            string path = GetProjectTargetDir(project);
            return new DirectoryInfo(path);
        }

        ///<inheritdoc/>
        public Task<byte[]?> ReadCheckSumAsync(IProject project)
        {
            string sumFile = Path.Combine(Index.SumDir.FullName, $"{ModData.ModuleName}-{project.GetSafeProjectName()}.json");            
            return File.Exists(sumFile) ? File.ReadAllBytesAsync(sumFile) : Task.FromResult<byte[]?>(null);
        }

        ///<inheritdoc/>
        public Task WriteChecksumAsync(IProject project, byte[] fileData)
        {
            //Create sum file inside the sum directory
            string sumFile = Path.Combine(Index.SumDir.FullName, $"{ModData.ModuleName}-{project.GetSafeProjectName()}.json");
            return File.WriteAllBytesAsync(sumFile, fileData);
        }

        ///<inheritdoc/>
        public async Task<FileInfo> WriteFileAsync(ModuleFileType type, byte[] fileData)
        {
            //Get the file path for the given type
            string filePath = type switch
            {
                //Catalog is written to the version pointed to by the latest git commit hash
                ModuleFileType.Catalog => $"{OutputDir}/{GetLatestTagOrSha()}/index.json",
                ModuleFileType.GitHistory => $"{OutputDir}/git.json",
                ModuleFileType.LatestHash => $"{OutputDir}/@latest",
                ModuleFileType.VersionHistory => $"{OutputDir}/versions.json",
                //Store project archive
                ModuleFileType.Archive => $"{OutputDir}/{GetLatestTagOrSha()}/archive.tgz",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };

            await File.WriteAllBytesAsync($"{filePath}", fileData);
            //Return new file handle
            return new FileInfo(filePath);
        }

        private string GetProjectTargetDir(IProject project)
        {
            //get last tag
            return Path.Combine(OutputDir, GetLatestTagOrSha(), project.GetSafeProjectName());
        }

        private string GetLatestTagOrSha()
        {
            return ModData.Repository.Head.Tip.Sha;
        }

    }
}