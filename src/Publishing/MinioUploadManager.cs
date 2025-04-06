using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Model;

using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Publishing
{

    internal sealed class MinioUploadManager(BuildConfig config, string minioPath) : IUploadManager
    {
        private readonly ProcessRunner _runner = new(config);

        public async Task UploadDirectoryAsync(string path)
        {
            //Recursivley copy all files in the working directory
            string[] args =
            [
                "cp",
                "--recursive",
                ".",
               minioPath
            ];

            //Set working dir to the supplied dir path, and run the command
            int result = await _runner.RunProcessAsync("mc", "minio", new(path), args);

            if (result != 0)
            {
                throw new BuildFailedException($"Failed to upload directory {path} with status code {result:x}");
            }
        }
    }
}