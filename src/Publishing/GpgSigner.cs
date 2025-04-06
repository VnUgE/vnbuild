using System;
using System.IO;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Publishing
{
    public sealed class GpgSigner(BuildConfig config, bool enabled, string? defaultKey)
    {
        private readonly ProcessRunner runner = new(config);

        public bool IsEnabled { get; } = enabled;

        public async Task SignFileAsync(FileInfo file)
        {
            if (!IsEnabled)
            {
                return;
            }

            //Delete an original file to avoid conflicts
            string sigFile = $"{file.FullName}.sig";
            if (File.Exists(sigFile))
            {
                File.Delete(sigFile);
            }

            //Substitute command variables
            string commandString = config.GpgCommand
                .Replace("{file}", file.FullName)
                .Replace("{key}", defaultKey);

            string[] commands = commandString.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            int result = await runner.RunProcessAsync(config.GpgExeName, "gpg", null, commands);

            switch (result)
            {
                case 2:
                case 0:
                    break;
                default:
                    throw new BuildFailedException($"Failed to sign file {file.FullName}");
            }
        }
    }
}