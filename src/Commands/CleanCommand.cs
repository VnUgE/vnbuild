using System;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Commands
{

    [Command("clean", Description = "Cleans the build pipeline")]
    public sealed class CleanCommand(BuildPipeline pipeline, ConfigManager cfg) : BaseCommand(pipeline, cfg)
    {
        public override async ValueTask ExecStepsAsync(IConsole console)
        {
            console.Output.WriteLine("Begining clean step");

            await pipeline.DoStepCleanAsync();

            //Cleanup the sum dir and scratch dir
            Config.Index.SumDir.Delete(true);
            Config.Index.ScratchDir.Delete(true);

            console.WithForegroundColor(ConsoleColor.Green, o => o.Output.WriteLine("Pipeline cleaned"));
        }

        public override IFeedManager[] Feeds => [];
    }
}