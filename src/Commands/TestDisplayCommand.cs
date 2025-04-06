using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("display", Description = "Test command for debugging")]
    public sealed class TestDisplayCommand : BaseCommand
    {
        public override async ValueTask ExecStepsAsync(IConsole console, BuildPipeline pipeline)
        {
            console.Output.WriteLine("Press any key to exit...");
            await console.Input.ReadLineAsync(console.GetCancellationToken());
        }
    }
}