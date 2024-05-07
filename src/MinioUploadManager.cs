using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Model;

using static VNLib.Tools.Build.Executor.Constants.Utils;

namespace VNLib.Tools.Build.Executor
{
    internal class MinioUploadManager : IUploadManager
    {
        private readonly string _minioPath;

        private MinioUploadManager(string minioPath)
        {
            _minioPath = minioPath; 
        }

        public Task CleanAllAsync(string path)
        {
            throw new System.NotImplementedException();
        }

        public Task DeleteFileAsync(string filePath)
        {
            throw new System.NotImplementedException();
        }

        public async Task UploadDirectoryAsync(string path)
        {
            //Recursivley copy all files in the working directory
            string[] args = 
            {
                "cp",
                "--recursive",
                ".",
               _minioPath
            };

            //Set working dir to the supplied dir path, and run the command
            int result = await RunProcessAsync("mc", path, args);

            if(result != 0)
            {
                throw new BuildStepFailedException($"Failed to upload directory {path} with status code {result:x}");
            }
        }

        public Task UploadFileAsync(string filePath)
        {
            throw new System.NotImplementedException();
        }

        [return:NotNullIfNotNull(nameof(uploadPath))]
        public static IUploadManager? Create(string? uploadPath)
        {
            return string.IsNullOrWhiteSpace(uploadPath) ? null : new MinioUploadManager(uploadPath);
        }
    }
}