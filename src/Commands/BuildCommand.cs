using System;
using System.Threading;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

namespace VNLib.Tools.Build.Executor.Commands
{

    [Command("build", Description = "Executes a build operation in pipeline")]
    public class BuildCommand : BaseCommand
    {

        [CommandOption("no-delay", Description = "Skips any built-in delay/wait")]
        public bool SkipDelay { get; set; } = false;

        public override async ValueTask ExecStepsAsync(IConsole console, BuildPipeline pipeline)
        {
            CancellationToken cancellation = console.GetCancellationToken();

            console.Output.WriteLine("Starting build pipeline. Checking for source code changes");

            if (Force)
            {
                console.WithForegroundColor(ConsoleColor.Yellow, static o => o.Output.WriteLine("Forcing build step"));
            }

            //Check for source code changes
            bool changed = await pipeline.CheckForChangesAsync();

            //continue build
            if (!Force && !changed)
            {
                console.WithForegroundColor(ConsoleColor.Green, static o => o.Output.WriteLine("No source code changes detected. Skipping build step"));
                return;
            }

            if (Confirm)
            { 
                console.Output.WriteLine("Press any key to continue...");
                await console.Input.ReadLineAsync(cancellation);
                cancellation.ThrowIfCancellationRequested();
            }
            else if(!SkipDelay)
            {
                //wait for 10 seconds
                for (int i = 10; i > 0; i--)
                {
                    string seconds = i > 1 ? "seconds" : "second";
                    console.Output.WriteLine($"Starting build step in {i} {seconds}");
                    await Task.Delay(1000, cancellation);                
                }
            }

            await pipeline.DoStepBuild(Force);

            console.WithForegroundColor(ConsoleColor.Green, static o => o.Output.WriteLine("Build completed successfully"));
        }
    }
}