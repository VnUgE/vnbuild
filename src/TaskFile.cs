using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor
{
    public enum TaskfileComamnd
    {
        Clean,
        Build,
        Upload,
        Update,
        PostbuildSuccess,
        PostbuildFailure,

        /// <summary>
        /// Runs in every runable location during a publish step for the project/module 
        /// to run publish tasks or pre-publish tasks
        /// </summary>
        Publish,

        /// <summary>
        /// A short running task that runs tests for a project or module
        /// </summary>
        Test,

        /// <summary>
        /// A long running task that starts before tests and ends after tests
        /// </summary>
        TestUp
    }

    /// <summary>
    /// Represents a controller for the TaskFile build system
    /// </summary>
    public sealed class TaskFile(BuildConfig build, ModuleConfig mod)
    {
        private readonly ProcessRunner _runner = new(build);

        /// <summary>
        /// Executes the desired Taskfile command with the given user args for 
        /// the configured manager.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="scope">Additional information used to execute task and the desired command</param>
        /// <returns>A task that completes with the status code of the operation</returns>
        public async Task ExecCommandAsync(
            ITaskfileScope scope, 
            TaskfileComamnd command, 
            bool throwIfFailed, 
            CancellationToken token = default
        )
        {
            //Specify taskfile if it is set
            List<string> args = [];
            if (!string.IsNullOrWhiteSpace(scope.TaskfileName))
            {
                //If taskfile is set, we need to make sure it is in the working dir to execute it, otherwise just exit
                if (!File.Exists(Path.Combine(scope.WorkingDir.FullName, scope.TaskfileName)))
                {
                    return;
                }

                args.Add("-t");
                args.Add(scope.TaskfileName);
            }

            //Add dryrun flag if set
            if (build.DryRun)
            {
                args.Add("--dry");
            }

            if (build.Force)
            {
                args.Add("--force");
            }

            if (build.TaskVerbose)
            {
                args.Add("--verbose");
            }

            string logName;

            if (scope is IProject proj)
            {
                logName = proj.Config.ProjectName;
            }
            else if (scope is IModuleData mod)
            {
                logName = mod.Config.ModuleName;
            }
            else
            {
                logName = scope.WorkingDir.Name;
            }

            //Always add command last
            args.Add(GetCommand(command));

            //Exec task in the module dir
            int result = await _runner.RunProcessAsync(
                process: build.TaskExeName,
                logName,
                scope.WorkingDir,
                args: [.. args],
                env: scope.TaskVars.GetVariables()
            );

            if (throwIfFailed)
            {
                ThrowIfStepFailed(scope, result, command);
            }
        }

        private static string GetCommand(TaskfileComamnd cmd)
        {
            return cmd switch
            {
                TaskfileComamnd.Clean               => "clean",
                TaskfileComamnd.Build               => "build",
                TaskfileComamnd.Upload              => "upload",
                TaskfileComamnd.Update              => "update",
                TaskfileComamnd.PostbuildSuccess    => "postbuild_success",
                TaskfileComamnd.PostbuildFailure    => "postbuild_failed",
                TaskfileComamnd.Publish             => "publish",
                TaskfileComamnd.Test                => "test",
                TaskfileComamnd.TestUp              => "test-up",
                _ => throw new NotImplementedException()
            };
        }

        private void ThrowIfStepFailed(ITaskfileScope scope, int result, TaskfileComamnd cmd)
        {
            switch (result)
            {
                case 200:   //Named task not found
                    return;
                case 201:
                    ThrowIfStepFailed(
                        status: false,
                        message: $"Task failed to execute task command {cmd} for {scope.WorkingDir.Name}",
                        mod.ModuleName
                    );
                    return;
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
