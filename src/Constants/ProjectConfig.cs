using System.Text.Json.Serialization;

namespace VNLib.Tools.Build.Executor.Constants
{
    public sealed class ProjectConfig
    {
        [JsonPropertyName("project_name")]
        public required string ProjectName { get; set; }

        [JsonPropertyName("project_file")]
        public required string ProjectFilePath { get; set; }

        [JsonPropertyName("project_directory")]
        public required string ProjectDirectory { get; set; }

        public string GetSafeProjectName()
        {
            return ProjectName
                .Replace('/', '-')
                .Replace('\\', '-');
        }
    }
}