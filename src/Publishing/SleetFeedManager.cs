using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Publishing
{
    internal sealed class SleetFeedManager : IFeedManager
    {
        private readonly string SleetConfigFile;

        ///<inheritdoc/>
        public string FeedOutputDir { get; }

        private SleetFeedManager(string indexFilex, string outputDir)
        {
            //Search for the sleet file in the build dir
            SleetConfigFile = indexFilex;
            FeedOutputDir = outputDir;
        }

        ///<inheritdoc/>
        public void AddVariables(TaskfileVars vars)
        {
            vars.Set("SLEET_DIR", FeedOutputDir);
            vars.Set("SLEET_CONFIG_PATH", SleetConfigFile);
        }

        /// <summary>
        /// Attempts to create a new sleet feed manager from the given directory index
        /// if the index contains a sleet feed. Returns null if no sleet feed was found
        /// </summary>
        /// <returns>The feed manager if found, null otherwise</returns>
        /// <exception cref="FileNotFoundException"></exception>
        /// <exception cref="ArgumentException"></exception>
        [return: NotNullIfNotNull(nameof(feedPath))]
        public static IFeedManager? GetSleetFeed(string? feedPath)
        {
            if (string.IsNullOrWhiteSpace(feedPath))
            {
                return null;
            }

            //Read the sleet config file
            byte[] sleetIndexFile = File.ReadAllBytes(feedPath);
            using JsonDocument doc = JsonDocument.Parse(sleetIndexFile);
            string rootDir = doc.RootElement.GetProperty("root").GetString() ?? throw new ArgumentException("The sleet output directory was not specified");
            return new SleetFeedManager(feedPath, rootDir);
        }
    }
}