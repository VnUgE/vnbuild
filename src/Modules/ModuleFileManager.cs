using System;
using System.IO;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Extensions;
using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Directories;


namespace VNLib.Tools.Build.Executor.Modules
{
    public sealed class ModuleFileManager(BuildConfig Config, IModuleData Mod) : IModuleFileManager
    {
        private readonly IDirectoryIndex _index = new GlobalDirIndex(Config);

        /// <summary>
        /// The output directory for the module
        /// </summary>
        private string OutputDir => _index.GetDirectory(VnbuildDir.Output, Mod.Config);

        /// <summary>
        /// The SHA of the current git HEAD
        /// </summary>
        private readonly string HeadSha = Mod.Repository.Head.Tip.Sha;

        ///<inheritdoc/>
        public async Task CopyArtifactToOutputAsync(IProject project, FileInfo file)
        {
            string targetDir = GetProjectTargetDir(project);

            //Project artifacts are versioned by the latest git commit hash
            string outputFile = Path.Combine(targetDir, file.Name);

            //Create the target directory if it doesn't exist
            Directory.CreateDirectory(targetDir);

            //Copy the file to the output directory
            FileInfo output = file.CopyTo(outputFile, overwrite: true);

            //Compute the file hash of the new output file
            await output.ComputeFileHashAsync(Config.HashFuncName);
        }

        ///<inheritdoc/>
        public DirectoryInfo GetArtifactOutputDir(IProject project)
        {
            string path = GetProjectTargetDir(project);
            return new DirectoryInfo(path);
        }

        private string GetChecksumFile(IProject project)
        {
            //Create sum file inside the module's sum directory
            string sumDir = _index.GetDirectory(VnbuildDir.Sum, Mod.Config);
            return Path.Combine(sumDir, $"{project.GetSafeProjectName()}.json");
        }

        ///<inheritdoc/>
        public Task<byte[]?> ReadCheckSumAsync(IProject project)
        {
            string sumFile = GetChecksumFile(project);
            return File.Exists(sumFile) ? File.ReadAllBytesAsync(sumFile) : Task.FromResult<byte[]?>(null);
        }

        ///<inheritdoc/>
        public Task WriteChecksumAsync(IProject project, byte[] fileData)
        {
            string checksumPath = GetChecksumFile(project);
            Directory.CreateDirectory(Path.GetDirectoryName(checksumPath)!);
            return File.WriteAllBytesAsync(checksumPath, fileData);
        }

        ///<inheritdoc/>
        public async Task<FileInfo> WriteFileAsync(ModuleFileType type, byte[] fileData)
        {
            //Get the file path for the given type
            string filePath = type switch
            {
                //Catalog is written to the version pointed to by the latest git commit hash
                ModuleFileType.Catalog          => $"{OutputDir}/{HeadSha}/index.json",
                ModuleFileType.GitHistory       => $"{OutputDir}/git.json",
                ModuleFileType.LatestHash       => $"{OutputDir}/@latest",
                ModuleFileType.VersionHistory   => $"{OutputDir}/versions.json",
                //Store project archive
                ModuleFileType.Archive => $"{OutputDir}/{HeadSha}/archive.tgz",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };

            await File.WriteAllBytesAsync(filePath, fileData);

            //Return new file handle
            return new FileInfo(filePath);
        }

        private string GetProjectTargetDir(IProject project)
        {
            //get last tag
            return Path.Combine(OutputDir, HeadSha, project.GetSafeProjectName());
        }
    }
}