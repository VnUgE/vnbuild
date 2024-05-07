using System.Threading.Tasks;

namespace VNLib.Tools.Build.Executor.Model
{
    internal interface IBuildable : IArtifact
    {
        Task DoStepSyncSource();

        Task<bool> CheckForChangesAsync();

        Task DoStepBuild();

        Task DoStepPostBuild(bool success);

        Task DoStepPublish();

        Task DoRunTests(bool failOnError);
    }
}