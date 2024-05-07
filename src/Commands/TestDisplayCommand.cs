using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("display", Description = "Test command for debugging")]
    public sealed class TestDisplayCommand(BuildPipeline pipeline, ConfigManager bm) : BaseCommand(pipeline, bm)
    {
        public override IFeedManager[] Feeds => [];

        public override async ValueTask ExecStepsAsync(IConsole console)
        {
            console.Output.WriteLine("Press any key to exit...");
            await console.Input.ReadLineAsync(console.GetCancellationToken());
        }
    }
}