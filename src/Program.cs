using System.Reflection;
using Typin;
using Typin.Console;

namespace VNLib.Tools.Build.Executor
{

    sealed class Program
    {
        static int Main(string[] argsv)
        {
            //Fetch the current version of the assembly
            Assembly assembly = Assembly.GetExecutingAssembly();
            string version = assembly.GetName().Version?.ToString() ?? "1.0.0";

            return new CliApplicationBuilder()
                 .AddCommandsFromThisAssembly()
                 .UseConsole<SystemConsole>()
                 .UseTitle("VNBuild Copyright (c) Vaughn Nugent")
                 .UseVersionText(version)
                 .Build()
                 .RunAsync()
                 .AsTask()
                 .GetAwaiter()
                 .GetResult();
        }
    }
}