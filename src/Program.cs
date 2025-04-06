using Typin;
using Typin.Console;

namespace VNLib.Tools.Build.Executor
{

    sealed class Program
    {
        static int Main(string[] argsv)
        {
            return new CliApplicationBuilder()
                 .AddCommandsFromThisAssembly()
                 .UseConsole<SystemConsole>()
                 .UseTitle("VNBuild Copyright (c) Vaughn Nugent")
                 .UseStartupMessage("VNBuild Copyright (c) Vaughn Nugent")
                 .UseVersionText("0.1.0")
                 .Build()
                 .RunAsync()
                 .AsTask()
                 .GetAwaiter()
                 .GetResult();
        }
    }
}