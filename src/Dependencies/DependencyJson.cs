using System.Text.Json.Serialization;

namespace VNLib.Tools.Build.Executor.Dependencies
{
    public class DependencyJson
    {
        /// <summary>
        /// The source url of the package to download and unpack. Source is required.
        /// </summary>
        [JsonPropertyName("source")]
        public required string Source { get; init; }

        /// <summary>
        /// The output directory to write the unpacked source to (chroot) or
        /// to place the artifact if unpacking is disabled
        /// </summary>
        [JsonPropertyName("dest")]
        public required string Destination { get; init; }

        /// <summary>
        /// An optional checksum for the source file to compare against
        /// </summary>
        [JsonPropertyName("checksum")]
        public string? Sum { get; init; }

        /// <summary>
        /// A value that indicates if the http connection used to download the package
        /// respects default SSL/TLS security validations, or disable them. The default
        /// is false. (Use security)
        /// </summary>
        [JsonPropertyName("insecure")]
        public bool Insecure { get; init; }

        /// <summary>
        /// A value that indicates if the dependency should be automatically unpacked 
        /// after downloaded. The default is true.
        /// </summary>
        [JsonPropertyName("unpack")]
        public bool Unpack { get; init; } = true;

        /// <summary>
        /// An optional command string to execute on the terminal after a successful
        /// install.
        /// </summary>
        [JsonPropertyName("post_install_cmd")]
        public string? PostInstallCommand { get; init; }

        /// <summary>
        /// An optional command string to execute on the terminal before the dependency 
        /// is installed.
        /// </summary>
        [JsonPropertyName("pre_install_cmd")]
        public string? PreInstallCommand { get; init; }
    }
}