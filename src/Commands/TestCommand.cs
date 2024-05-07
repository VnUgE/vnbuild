using System;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("test", Description = "Executes tests steps within the pipline for all loaded modules")]
    public sealed class TestCommand(BuildPipeline pipeline, ConfigManager cfg) : BaseCommand(pipeline, cfg)
    {
        private readonly BuildPipeline _pipeline = pipeline;

        [CommandOption("--no-fail", Description = "Exit testing on the first test failure")]
        public bool FailOnTestFail { get; set; } = true;

        public override async ValueTask ExecStepsAsync(IConsole console)
        {
            console.Output.WriteLine("Begining test step");

            await _pipeline.ExecuteTestsAsync(FailOnTestFail);

            console.WithForegroundColor(ConsoleColor.Green, o => o.Output.WriteLine("Pipeline tests compled"));
        }

        public override IFeedManager[] Feeds => [];
    }
}