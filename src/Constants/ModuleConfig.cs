using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VNLib.Tools.Build.Executor.Constants
{
    public sealed class ModuleConfig
    {
        /// <summary>
        /// The name of the module. Set by the discovery 
        /// process.
        /// </summary>
        [JsonIgnore]
        public string ModuleName { get; set; }

        /// <summary>
        /// The directory where the module is located. Set 
        /// by the discovery process.
        /// </summary>
        [JsonIgnore]
        public string ModuleDirectory { get; set; }

        [JsonPropertyName("soure_file_extensions")]
        public string[] SourceFileEx { get; init; } = [
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
        public string[] ExcludedSourceDirs { get; init; } = [
            "bin",
            "obj",
            "packages",
            "node_modules",
            "dist",
            "build",
            "out",
            "target",
        ];

        /// <summary>
        /// The name of the module level task file 
        /// </summary>
        [JsonPropertyName("module_task_file_name")]
        public string ModuleTaskFileName { get; init; } = "Module.Taskfile.yaml";

        /// <summary>
        /// The output file type to use when searching for output files
        /// </summary>
        [JsonPropertyName("output_file_type")]
        public string OutputFileType { get; init; } = "*.tgz";

        /// <summary>
        /// The default archive format to use when creating source archives
        /// </summary>
        [JsonPropertyName("source_archive_format")]
        public string SourceArchiveFormat { get; init; } = "tgz";

        /// <summary>
        /// The default name of the binary directory contained within
        /// the project directory
        /// </summary>
        [JsonPropertyName("binary_dir_name")]
        public string ProjectBinDir { get; init; } = "bin";

        [JsonPropertyName("task_environment_variables")]
        public IDictionary<string, string> TaskVars { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("source_archive_name")]
        public string SourceArchiveName { get; init; } = "archive.tgz";

        [JsonPropertyName("project_search_patterns")]
        public string[] ProjectSearchPatterns { get; init; } = ["package.json"];

        [JsonPropertyName("solution_file_pattern")]
        public string SolutionFilePattern { get; init; } = "*.sln";
    }
}