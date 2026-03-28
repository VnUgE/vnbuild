using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Typin.Exceptions;

namespace VNLib.Tools.Build.Executor.Constants
{
    internal sealed class ProcessRunner(BuildConfig config)
    {
        /// <summary>
        /// Runs a process by its name/exe file path, and writes its stdout/stderr to 
        /// the default build log
        /// </summary>
        /// <param name="process">The name of the process to run</param>
        /// <param name="args">CLI arguments to pass to the process</param>
        /// <returns>The process exit code</returns>
        public async Task<int> RunProcessAsync(
            string process,
            string logName,
            DirectoryInfo? workingDir,
            string[] args,
            IReadOnlyDictionary<string, string>? env = null
        )
        {
            //Init new console cancellation token
            using ConsoleCancelToken ctToken = new();

            ProcessStartInfo psi = new(process)
            {
                //Redirect streams
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                //Create a child process, not shell
                UseShellExecute = false,
                WorkingDirectory = workingDir?.FullName ?? string.Empty,
            };

            if (env != null)
            {
                //Add all env variables to process
                foreach (KeyValuePair<string, string> kv in env)
                {
                    psi.Environment.Add(kv.Key, kv.Value);
                }
            }

            //Add arguments
            foreach (string arg in args)
            {
                psi.ArgumentList.Add(arg);
            }

            using Process proc = new();
            proc.StartInfo = psi;

            //Start the process
            proc.Start();

            config.Log.Debug("Starting process {proc} in {dir}, with args {args}", proc.ProcessName, psi.WorkingDirectory, args);
            Console.WriteLine();

            //Log std out
            Task stdout = ProcessStdOutAsync(proc, logName, Console.Out, ctToken.Token);
            Task stdErr = ProcessStdErrAsync(proc, logName, Console.Error, ctToken.Token);

            //Wait for the process to exit
            Task wfe = proc.WaitForExitAsync(ctToken.Token);

            //Wait for stderr/out/proc to exit
            await Task.WhenAll(stdout, stdErr, wfe);

            Console.WriteLine();
            config.Log.Debug("[CHILD]:{id}:{p} exited w/ code {code}", proc.ProcessName, proc.Id, proc.ExitCode);

            //Return status code
            return proc.ExitCode;
        }

        /// <summary>
        /// Starts a process from the given <paramref name="psi"/>, streams its stdout and stderr to the
        /// console, waits for exit, then throws a <see cref="CommandException"/> if the process returns
        /// a non-zero exit code.
        /// </summary>
        /// <param name="psi">Fully configured process start info.</param>
        /// <param name="processName">Display name used in log prefixes and error messages.</param>
        /// <param name="cancellationToken">Token to cancel the wait.</param>
        internal static async Task RunAndThrowAsync(
            ProcessStartInfo psi,
            string processName,
            CancellationToken cancellationToken
        )
        {
            using Process proc = Process.Start(psi)
                ?? throw new InvalidOperationException($"Failed to start {processName} process");

            await Task.WhenAll(
                ProcessStdOutAsync(proc, processName, Console.Out, cancellationToken),
                ProcessStdErrAsync(proc, processName, Console.Error, cancellationToken),
                proc.WaitForExitAsync(cancellationToken)
            ).ConfigureAwait(false);

            if (proc.ExitCode != 0)
            {
                throw new CommandException($"{processName} failed with exit code {proc.ExitCode}", exitCode: -2);
            }
        }

        /// <summary>
        /// Runs a quick availability/version check for a process by starting it and discarding output.
        /// Returns <c>true</c> if the process exits with code 0, <c>false</c> otherwise.
        /// </summary>
        /// <param name="psi">Start info for the version/availability check invocation.</param>
        internal static async Task<bool> CheckAvailableAsync(ProcessStartInfo psi)
        {
            using Process? proc = Process.Start(psi);

            if (proc is null)
            {
                return false;
            }

            await Task.WhenAll(
                proc.StandardOutput.ReadToEndAsync(),
                proc.StandardError.ReadToEndAsync(),
                proc.WaitForExitAsync()
            ).ConfigureAwait(false);

            return proc.ExitCode == 0;
        }


        /// <summary>
        /// Continuously reads stdout from the given process and writes it to the given output
        /// until the process exits
        /// </summary>
        /// <param name="psi">The process to log</param>
        /// <param name="logName">The name of the formatted log output</param>
        /// <param name="output">The detination stream to write the output to</param>
        /// <param name="cancellation">A token to cancel the read operation</param>
        /// <returns>A task that completes when all text is read and/or the process has exited</returns>
        internal static async Task ProcessStdOutAsync(Process psi, string logName, TextWriter output, CancellationToken cancellation)
        {
            do
            {
                //Read lines from the process
                string? line = await psi.StandardOutput.ReadLineAsync(cancellation);

                if (line == null)
                {
                    break;
                }

                //Print to log file
                output.WriteLine($"[{logName}]: {line}");
            } while (!psi.HasExited);
        }

        /// <summary>
        /// Continuously reads stderr from the given process and writes it to the given output
        /// until the process exits
        /// </summary>
        /// <param name="psi">The process to log</param>
        /// <param name="logName">The name of the formatted log output</param>
        /// <param name="output">The detination stream to write the output to</param>
        /// <param name="cancellation">A token to cancel the read operation</param>
        /// <returns>A task that completes when all text is read and/or the process has exited</returns>
        internal static async Task ProcessStdErrAsync(Process psi, string logName, TextWriter output, CancellationToken cancellation)
        {
            do
            {
                //Read lines from the process
                string? line = await psi.StandardError.ReadLineAsync(cancellation);

                if (line == null)
                {
                    break;
                }

                //Print to log file
                output.WriteLine($"[{logName}]: {line}");
            } while (!psi.HasExited);
        }
    }
}