using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("list-projects", Description = "Lists all projects in a named module")]
    public sealed class ListProjectsCommand : BaseCommand
    {
        [CommandParameter(1, Name = "Module", Description = "The name of the module that contains the project to build")]
        public string? ModuleName { get; set; }

        public override ValueTask ExecStepsAsync(IConsole console, BuildPipeline pipeline)
        {
            CancellationToken cancellation = console.GetCancellationToken();

            IProject[] projects = pipeline.GetModules()
                .Where(m => string.Equals(m.Config.ModuleName, ModuleName, StringComparison.OrdinalIgnoreCase))
                .SelectMany(static m => m.Projects)
                .ToArray();

            string projectList = string.Join("\n", projects.Select(static p => $"- {p.Config.ProjectName}"));

            console.Output.WriteLine($"Displaying projects for module {ModuleName}\n{projectList}");

            return default;
        }
    }
}