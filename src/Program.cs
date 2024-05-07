using Typin;
using Typin.Console;

using VNLib.Tools.Build.Executor.Constants;

using Microsoft.Extensions.DependencyInjection;


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
                 .ConfigureServices(services =>
                 {
                     //Init new pipeline and add to service collection
                     services
                     .AddSingleton(Config.Log)
                     .AddSingleton<BuildPipeline>()
                     .AddSingleton(new ConfigManager(Semver.SemVersionStyles.Any));
                     
                 })
                 .Build()
                 .RunAsync()
                 .AsTask()
                 .GetAwaiter()
                 .GetResult();
        }
    }
}