using System.IO;
using System.Text.Json.Serialization;

using Serilog.Core;

namespace VNLib.Tools.Build.Executor.Constants
{
    public sealed class BuildConfig
    {
        [JsonIgnore]
        public Logger Log { get; set; }

        [JsonIgnore]
        public bool DryRun { get; set; }

        [JsonIgnore]
        public bool Force { get; set; }

        [JsonIgnore]
        public bool Confirm { get; set; }

        /// <summary>
        /// A flag indicates whether to enable go-task's verbose output.
        /// </summary>
        [JsonIgnore]
        public bool TaskVerbose { get; set; } = false;

        [JsonIgnore]
        public string WorkingDirectory { get; set; } = Directory.GetCurrentDirectory();

        [JsonPropertyName("default_sha_method")]
        public string HashFuncName { get; set; } = "sha256";

        [JsonPropertyName("task_exe_name")]
        public string TaskExeName { get; set; } = "task";

        [JsonPropertyName("build_directory")]
        public string BuildDirectory { get; set; } = "build";

        /// <summary>
        /// The default log template for the build system
        /// </summary>
        [JsonPropertyName("log_template")]
        public string LogTemplate { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// The search pattern to use when searching for git directories
        /// </summary>
        [JsonPropertyName("git_dir_pattern")]
        public string GitDirName { get; set; } = ".git";

        /// <summary>
        /// The name of the module level vnbuild.json file
        /// </summary>
        [JsonPropertyName("module_configle_file")]
        public string ModuleConfigFileName { get; set; } = ".vnbuild-module.json";

        /// <summary>
        /// The gitversion dotnet tool path
        /// </summary>
        [JsonPropertyName("gitversion_tool_path")]
        public string GitversionToolPath { get; set; } = "dotnet-gitversion";

        /// <summary>
        /// The command to pass to gpg for signing files
        /// </summary>
        [JsonPropertyName("gpg_command")]
        public string GpgCommand { get; set; } = "--detach-sign {file}";

        /// <summary>
        /// The name of the gpg executable
        /// </summary>
        [JsonPropertyName("gpg_exe_name")]
        public string GpgExeName { get; set; } = "gpg";
    }
}