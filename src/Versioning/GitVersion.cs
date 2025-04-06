using System.Text.Json.Serialization;

namespace VNLib.Tools.Build.Executor.Versioning
{
    internal sealed class GitVersion
    {
        /// <summary>
        /// The major version. Should be incremented on breaking changes.
        /// </summary>
        [JsonPropertyName("Major")]
        public required int Major { get; init; }

        /// <summary>
        /// The minor version. Should be incremented on new features.
        /// </summary>
        [JsonPropertyName("Minor")]
        public required int Minor { get; init; }

        /// <summary>
        /// The patch version. Should be incremented on bug fixes.
        /// </summary>
        [JsonPropertyName("Patch")]
        public required int Patch { get; init; }

        /// <summary>
        /// The pre-release tag is the pre-release label suffixed by the PreReleaseNumber.
        /// </summary>
        [JsonPropertyName("PreReleaseTag")]
        public required string PreReleaseTag { get; init; }

        /// <summary>
        /// The pre-release tag prefixed with a dash.
        /// </summary>
        [JsonPropertyName("PreReleaseTagWithDash")]
        public required string PreReleaseTagWithDash { get; init; }

        /// <summary>
        /// The pre-release label.
        /// </summary>
        [JsonPropertyName("PreReleaseLabel")]
        public required string PreReleaseLabel { get; init; }

        /// <summary>
        /// The pre-release label prefixed with a dash.
        /// </summary>
        [JsonPropertyName("PreReleaseLabelWithDash")]
        public required string PreReleaseLabelWithDash { get; init; }

        /// <summary>
        /// The pre-release number.
        /// </summary>
        [JsonPropertyName("PreReleaseNumber")]
        public required int? PreReleaseNumber { get; init; }

        /// <summary>
        /// A summation of branch specific pre-release-weight and the PreReleaseNumber. Can be used to obtain a monotonically increasing version number across the branches.
        /// </summary>
        [JsonPropertyName("WeightedPreReleaseNumber")]
        public required int? WeightedPreReleaseNumber { get; init; }

        /// <summary>
        /// The BuildMetaData padded with zeros.
        /// </summary>
        [JsonPropertyName("BuildMetaDataPadded")]
        public required string BuildMetaDataPadded { get; init; }

        /// <summary>
        /// The BuildMetaData suffixed with BranchName and Sha.
        /// </summary>
        [JsonPropertyName("FullBuildMetaData")]
        public required string FullBuildMetaData { get; init; }

        /// <summary>
        /// Major, Minor and Patch joined together, separated by dots.
        /// </summary>
        [JsonPropertyName("MajorMinorPatch")]
        public required string MajorMinorPatch { get; init; }

        /// <summary>
        /// The SemVer version.
        /// </summary>
        [JsonPropertyName("SemVer")]
        public required string SemVer { get; init; }

        /// <summary>
        /// The legacy SemVer version.
        /// </summary>
        [JsonPropertyName("LegacySemVer")]
        public required string LegacySemVer { get; init; }

        /// <summary>
        /// The legacy SemVer version padded with zeros.
        /// </summary>
        [JsonPropertyName("LegacySemVerPadded")]
        public required string LegacySemVerPadded { get; init; }

        /// <summary>
        /// Suitable for .NET AssemblyVersion. Defaults to Major.Minor.0.0 to allow the assembly to be hotfixed without breaking existing applications that may be referencing it.
        /// </summary>
        [JsonPropertyName("AssemblySemVer")]
        public required string AssemblySemVer { get; init; }

        /// <summary>
        /// Suitable for .NET AssemblyFileVersion. Defaults to FullSemVer padded with zeros.
        /// </summary>
        [JsonPropertyName("AssemblySemFileVer")]
        public required string AssemblySemFileVer { get; init; }

        /// <summary>
        /// The full, SemVer 2.0 compliant version number.
        /// </summary>
        [JsonPropertyName("FullSemVer")]
        public required string FullSemVer { get; init; }

        /// <summary>
        /// Suitable for .NET AssemblyInformationalVersion. Defaults to FullSemVer suffixed by FullBuildMetaData.
        /// </summary>
        [JsonPropertyName("InformationalVersion")]
        public required string InformationalVersion { get; init; }

        /// <summary>
        /// The name of the checked out Git branch.
        /// </summary>
        [JsonPropertyName("BranchName")]
        public required string BranchName { get; init; }

        /// <summary>
        /// Equal to BranchName, but with / replaced with -.
        /// </summary>
        [JsonPropertyName("EscapedBranchName")]
        public required string EscapedBranchName { get; init; }

        /// <summary>
        /// The SHA of the Git commit.
        /// </summary>
        [JsonPropertyName("Sha")]
        public required string Sha { get; init; }

        /// <summary>
        /// The Sha limited to 7 characters.
        /// </summary>
        [JsonPropertyName("ShortSha")]
        public required string ShortSha { get; init; }

        /// <summary>
        /// The NuGet version 2.
        /// </summary>
        [JsonPropertyName("NuGetVersionV2")]
        public required string NuGetVersionV2 { get; init; }

        /// <summary>
        /// The NuGet version.
        /// </summary>
        [JsonPropertyName("NuGetVersion")]
        public required string NuGetVersion { get; init; }

        /// <summary>
        /// The NuGet pre-release tag version 2.
        /// </summary>
        [JsonPropertyName("NuGetPreReleaseTagV2")]
        public required string NuGetPreReleaseTagV2 { get; init; }

        /// <summary>
        /// The NuGet pre-release tag.
        /// </summary>
        [JsonPropertyName("NuGetPreReleaseTag")]
        public required string NuGetPreReleaseTag { get; init; }

        /// <summary>
        /// The SHA of the commit used as version source.
        /// </summary>
        [JsonPropertyName("VersionSourceSha")]
        public required string VersionSourceSha { get; init; }

        /// <summary>
        /// The number of commits since the version source.
        /// </summary>
        [JsonPropertyName("CommitsSinceVersionSource")]
        public required int CommitsSinceVersionSource { get; init; }

        /// <summary>
        /// The number of commits since the version source padded with zeros.
        /// </summary>
        [JsonPropertyName("CommitsSinceVersionSourcePadded")]
        public required string CommitsSinceVersionSourcePadded { get; init; }

        /// <summary>
        /// The number of uncommitted changes present in the repository.
        /// </summary>
        [JsonPropertyName("UncommittedChanges")]
        public required int UncommittedChanges { get; init; }

        /// <summary>
        /// The ISO-8601 formatted date of the commit identified by Sha.
        /// </summary>
        [JsonPropertyName("CommitDate")]
        public required string CommitDate { get; init; }
    }
}