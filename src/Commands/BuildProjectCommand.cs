using System;
using System.Threading;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("build-project", Description = "Builds a named project inside a named module")]
    public sealed class BuildProjectCommand(BuildPipeline pipeline, ConfigManager bm) : BaseCommand(pipeline, bm)
    {

        [CommandParameter(1, Name = "Module", Description = "The name of the module that contains the project to build")]
        public string? ModuleName { get; set; }

        [CommandParameter(2, Name = "Project", Description = "The name of the project to build")]
        public string? ProjectName { get; set; }

        public override async ValueTask ExecStepsAsync(IConsole console)
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

        public override IFeedManager[] Feeds => [];
    }
}