using System.IO;


namespace VNLib.Tools.Build.Executor.Model
{
    public interface IProjectData
    {
        string? Description { get; }
        string? Authors { get; }
        string? Copyright { get; }
        string? VersionString { get; }
        string? CompanyName { get; }
        string? Product { get; }
        string? RepoUrl { get; }
        string? this[string index] { get; }
        void Load(Stream stream);
        string[] GetProjectRefs();
    }
}