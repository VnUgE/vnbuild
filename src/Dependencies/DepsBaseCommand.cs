using System.Threading.Tasks;

using Typin;
using Typin.Attributes;
using Typin.Console;
using Typin.Exceptions;

namespace VNLib.Tools.Build.Executor.Dependencies
{
    [Command("deps", Description = "vnbuild package management")]
    public class DepsBaseCommand : ICommand
    {
        public virtual ValueTask ExecuteAsync(IConsole console)
        {
            return ValueTask.FromException(
                new CommandException("Please specify a command", showHelp: true)
            );
        }
    }
}