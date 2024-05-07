using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using static VNLib.Tools.Build.Executor.Constants.Config;

namespace VNLib.Tools.Build.Executor.Constants
{

    internal static class Utils
    {

        /// <summary>
        /// Runs a process by its name/exe file path, and writes its stdout/stderr to 
        /// the default build log
        /// </summary>
        /// <param name="process">The name of the process to run</param>
        /// <param name="args">CLI arguments to pass to the process</param>
        /// <returns>The process exit code</returns>
        public static async Task<int> RunProcessAsync(string process, string? workingDir, string[] args, IReadOnlyDictionary<string, string>? env = null)
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
                WorkingDirectory = workingDir ?? string.Empty,
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

            Log.Debug("Starting process {proc}, with args {args}", proc.ProcessName, args);
            Console.WriteLine();

            //Log std out
            Task stdout = LogStdOutAsync(proc, ctToken.Token);
            Task stdErr = LogStdErrAsync(proc, ctToken.Token);

            //Wait for the process to exit
            Task wfe = proc.WaitForExitAsync(ctToken.Token);

            //Wait for stderr/out/proc to exit
            await Task.WhenAll(stdout, stdErr, wfe);

            Console.WriteLine();
            Log.Debug("[CHILD]:{id}:{p} exited w/ code {code}", proc.ProcessName, proc.Id, proc.ExitCode);

            //Return status code
            return proc.ExitCode;
        }

        private static async Task LogStdOutAsync(Process psi, CancellationToken cancellation)
        {
            try
            {
                string procName = psi.ProcessName;
                int id = psi.Id;

                do
                {
                    //Read lines from the process
                    string? line = await psi.StandardOutput.ReadLineAsync(cancellation);

                    if (line == null)
                    {
                        break;
                    }

                    //Print to log file
                    Console.WriteLine(line);
                } while (!psi.HasExited);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception was raised while reading the process standard output");
            }
        }

        private static async Task LogStdErrAsync(Process psi, CancellationToken cancellation)
        {
            try
            {
                string procName = psi.ProcessName;
                int id = psi.Id;

                do
                {
                    //Read lines from the process
                    string? line = await psi.StandardError.ReadLineAsync(cancellation);

                    if (line == null)
                    {
                        break;
                    }

                    //Print to log file
                    Console.WriteLine(line);
                } while (!psi.HasExited);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception was raised while reading the process standard output");
            }
        }


        /// <summary>
        /// Throws a <see cref="BuildStepFailedException"/> if the value
        /// of <paramref name="status"/> is false
        /// </summary>
        /// <param name="status">If false throws exception</param>
        /// <param name="message">The message to display</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfStepFailed(bool status, string message, string artifactName)
        {
            if (!status)
            {
                throw new BuildStepFailedException(message, artifactName);
            }
        }
    
    }
}