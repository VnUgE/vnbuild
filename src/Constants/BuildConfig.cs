using System.Text.Json.Serialization;

using Semver;

using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Constants
{
    public sealed class BuildConfig
    {
        [JsonPropertyName("soure_file_extensions")]
        public string[] SourceFileEx { get; set; } = [
            "c",
            "cpp",
            "cxx",
            "h",
            "hpp",
            "cs",
            "proj",
            "sln",
            "ts",
            "js",
            "java",
            "json",
            "yaml",
            "yml",
        ];

        [JsonPropertyName("excluded_dirs")]
        public string[] ExcludedSourceDirs { get; set; } = [
            "bin",
            "obj",
            "packages",
            "node_modules",
            "dist",
            "build",
            "out",
            "target",
        ];

        [JsonPropertyName("default_sha_method")]
        public string HashFuncName { get; set; } = "sha256";

        [JsonPropertyName("head_file_name")]
        public string HeadFileName { get; set; } = "@latest";

        [JsonPropertyName("module_task_file_name")]
        public string ModuleTaskFileName { get; set; } = "Module.Taskfile.yaml";

        [JsonPropertyName("main_taskfile_name")]
        public string MainTaskFileName { get; set; } = "build.taskfile.yaml";

        [JsonPropertyName("output_file_type")]
        public string OutputFileType { get; set; } = "*.tgz";

        [JsonPropertyName("task_exe_name")]
        public string TaskExeName { get; set; } = "task";

        [JsonPropertyName("source_archive_name")]
        public string SourceArchiveName { get; set; } = "archive.tgz";

        [JsonPropertyName("source_archive_format")]
        public string SourceArchiveFormat { get; set; } = "tgz";

        [JsonPropertyName("project_bin_dir")]
        public string ProjectBinDir { get; set; } = "bin";

        [JsonPropertyName("default_ci_version")]
        public string DefaultCiVersion { get; set; } = "0.1.0";

        [JsonPropertyName("semver_style")]
        public SemVersionStyles SemverStyle { get; set; }

        [JsonIgnore]
        public IDirectoryIndex Index { get; set; } = default!;
    }
}