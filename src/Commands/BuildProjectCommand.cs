using System;
using System.Threading;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

namespace VNLib.Tools.Build.Executor.Commands
{   
    [Command("build project", Description = "Builds a named project inside a named module")]
    public class BuildProjectCommand : BaseCommand
    {

        [CommandParameter(1, Name = "Module", Description = "The name of the module that contains the project to build")]
        public string? ModuleName { get; set; }

        [CommandParameter(2, Name = "Project", Description = "The name of the project to build")]
        public string? ProjectName { get; set; }

        public override async ValueTask ExecStepsAsync(IConsole console, BuildPipeline pipeline)
        {
            CancellationToken cancellation = console.GetCancellationToken();

            console.Output.WriteLine("Starting single project build");

            if (Confirm)
            {
                console.Output.WriteLine("Press any key to continue...");
                await console.Input.ReadLineAsync(cancellation);
                cancellation.ThrowIfCancellationRequested();
            }

            await pipeline.BuildSingleProject(ModuleName!, ProjectName!);

            console.WithForegroundColor(ConsoleColor.Green, static o => o.Output.WriteLine("Completed successfully"));
        }
    }

    [Obsolete("'build-project' command is replaced with 'build project' command")]
    [Command("build-project", Description = "obsolete - use 'build project' command instead")]
    public sealed class BuildProjectOld : BuildProjectCommand
    { }
}
