using System;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

namespace VNLib.Tools.Build.Executor.Commands
{

    [Command("clean", Description = "Cleans the build pipeline")]
    public sealed class CleanCommand : BaseCommand
    {
        public override async ValueTask ExecStepsAsync(IConsole console, BuildPipeline pipeline)
        {
            console.Output.WriteLine("Begining clean step");

            await pipeline.DoStepCleanAsync();

            console.WithForegroundColor(ConsoleColor.Green, o => o.Output.WriteLine("Pipeline cleaned"));
        }
    }
}