using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;
using static VNLib.Tools.Build.Executor.Constants.Utils;

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
        Publish,
        Test,
    }

    /// <summary>
    /// Represents a controller for the TaskFile build system
    /// </summary>
    public sealed class TaskFile(string taskFilePath, Func<string> moduleName)
    {
        /// <summary>
        /// Executes the desired Taskfile command with the given user args for 
        /// the configured manager.
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <param name="userArgs">Additional user arguments to pass to Task </param>
        /// <returns>A task that completes with the status code of the operation</returns>
        public async Task ExecCommandAsync(ITaskfileScope scope, TaskfileComamnd command, bool throwIfFailed)
        {
            //Get working copy of vars
            IReadOnlyDictionary<string, string> vars = scope.TaskVars.GetVariables();

            //Specify taskfile if it is set
            List<string> args = [];
            if(!string.IsNullOrWhiteSpace(scope.TaskfileName))
            {
                //If taskfile is set, we need to make sure it is in the working dir to execute it, otherwise just exit
                if(!File.Exists(Path.Combine(scope.WorkingDir.FullName, scope.TaskfileName)))
                {
                    return;
                }

                args.Add("-t");
                args.Add(scope.TaskfileName);
            }

            //Always add command last
            args.Add(GetCommand(command));

            //Exec task in the module dir
            int result = await RunProcessAsync(taskFilePath, scope.WorkingDir.FullName, [.. args], vars);
            
            if(throwIfFailed)
            {
                ThrowIfStepFailed(result, command);
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
                _ => throw new NotImplementedException()
            };
        }

        private void ThrowIfStepFailed(int result, TaskfileComamnd cmd)
        {
            switch (result)
            {
                case 200:   //Named task not found
                    return;
                case 201:
                    Utils.ThrowIfStepFailed(false, $"Task failed to execute task command {cmd}", moduleName.Invoke());
                    return;
            }
        }
    }
}