using System;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("update", Description = "Runs the build steps for updating application soure code")]
    public sealed class UpdateCommand : BaseCommand
    {
        public override async ValueTask ExecStepsAsync(IConsole console, BuildPipeline pipeline)
        {
            //Run the update step
            await pipeline.DoStepUpdateSource();

            console.WithForegroundColor(ConsoleColor.Green, o => o.Output.WriteLine("Source update complete"));
        }
    }
}