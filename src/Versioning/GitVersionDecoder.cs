using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using System.Text.Json;

namespace VNLib.Tools.Build.Executor.Versioning
{
    internal sealed class GitVersionDecoder(string overrideCmdName)
    {
        public async Task<GitVersion> GetVersionInfoAsync(string repoPath, CancellationToken token)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(repoPath);

            ProcessStartInfo psi = new(overrideCmdName)
            {
                WorkingDirectory = Path.GetFullPath(repoPath),
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            psi.ArgumentList.Add("/output");
            psi.ArgumentList.Add("json");

            using Process? proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Failed to start the gitversion process");

            string? output = await proc.StandardOutput.ReadToEndAsync(token);
            string? error = await proc.StandardError.ReadToEndAsync(token);

            await proc.WaitForExitAsync(token);

            int exitCode = proc.ExitCode;
            if (!string.IsNullOrWhiteSpace(error) || exitCode != 0)
            {
                throw new InvalidOperationException($"Failed to get gitversion info code={exitCode:x}, error={error}");
            }

            return JsonSerializer.Deserialize<GitVersion>(output)
                ?? throw new InvalidOperationException("Failed to decode gitversion output");
        }
    }
}