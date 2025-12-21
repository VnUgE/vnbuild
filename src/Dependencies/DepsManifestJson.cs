using System.IO;
using System.Text.Json.Serialization;

namespace VNLib.Tools.Build.Executor.Dependencies
{
    public sealed class DepsManifestJson
    {
        /// <summary>
        /// The array of dependency objects for download/install
        /// </summary>
        [JsonPropertyName("deps")]
        public DependencyJson[] Dependencies { get; set; } = [];

        /// <summary>
        /// Optional temporary directory used to write the package files to 
        /// when downloading.
        /// </summary>
        [JsonPropertyName("temp_dir")]
        public string? TempDir { get; init; } = Path.GetTempPath();
    }
}