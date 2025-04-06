using System;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("test", Description = "Executes tests steps within the pipline for all loaded modules")]
    public sealed class TestCommand : BaseCommand
    {

        [CommandOption("--no-fail", Description = "Exit testing on the first test failure")]
        public bool FailOnTestFail { get; set; } = true;

        public override async ValueTask ExecStepsAsync(IConsole console, BuildPipeline pipeline)
        {
            console.Output.WriteLine("Begining test step");

            await pipeline.ExecuteTestsAsync(FailOnTestFail);

            console.WithForegroundColor(ConsoleColor.Green, o => o.Output.WriteLine("Pipeline tests compled"));
        }
    }
}