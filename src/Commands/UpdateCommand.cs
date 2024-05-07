using System;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("update", Description = "Runs the build steps for updating application soure code")]
    public sealed class UpdateCommand(BuildPipeline pipeline, ConfigManager cfg) : BaseCommand(pipeline, cfg)
    {
        public override async ValueTask ExecStepsAsync(IConsole console)
        {
            //Run the update step
            await pipeline.DoStepUpdateSource();

            console.WithForegroundColor(ConsoleColor.Green, o => o.Output.WriteLine("Source update complete"));
        }

        public override IFeedManager[] Feeds => [];
    }
}