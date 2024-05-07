
using System;
using System.Linq;
using System.Threading.Tasks;

using Typin.Console;
using Typin.Attributes;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("publish", Description = "Runs publishig build steps on a completed build")]
    public sealed class PublishCommand(BuildPipeline pipeline, ConfigManager bm) : BaseCommand(pipeline, bm)
    {
        [CommandOption("upload-path", 'p', Description = "The path to upload the build artifacts")]
        public string? UploadPath { get; set; }

        [CommandOption("sign", 's', Description = "Enables gpg signing of build artifacts")]
        public bool Sign { get; set; } = false;

        [CommandOption("gpg-key", 'k', Description = "Optional key to use when signing, otherwise uses the GPG default signing key")]
        public string? GpgKey { get; set; }

        [CommandOption("sleet-path", 'F', Description = "Specifies the Sleet feed index path")]
        public string? SleetPath { get; set; }

        [CommandOption("dry-run", 'd', Description = "Executes all publish steps without pushing the changes to the remote server")]
        public bool DryRun { get; set; }

        [CommandOption("output", 'o', Description = "Specifies the output directory for the published modules")]
        public string? CustomOutDir { get; set; }

        public override async ValueTask ExecStepsAsync(IConsole console)
        {
            //Specify custom output dir
            (base.Config.Index as Dirs)!.OutputDir = BuildDirs.GetOrCreateDir(Constants.Config.OUTPUT_DIR, CustomOutDir);

            IUploadManager? uploads = MinioUploadManager.Create(UploadPath);
            IFeedManager? feed = Feeds.FirstOrDefault();

            //Optional gpg signer for signing published artifacts
            BuildPublisher pub = new(Config, new GpgSigner(Sign, GpgKey));

            console.WithForegroundColor(ConsoleColor.DarkGreen, static o => o.Output.WriteLine("Publishing modules"));

            //Run publish steps
            await pipeline.OnPublishingAsync().ConfigureAwait(false);

            console.WithForegroundColor(ConsoleColor.DarkGreen, static o => o.Output.WriteLine("Preparing module output for upload"));

            //Prepare the output 
            await pipeline.PrepareOutputAsync(pub).ConfigureAwait(false);

            if(uploads is null)
            {
                console.WithForegroundColor(ConsoleColor.DarkYellow, static o => o.Output.WriteLine("No upload path specified. Skipping upload"));
                console.WithForegroundColor(ConsoleColor.Green, static o => o.Output.WriteLine("Upload build complete"));
                return;
            }

            //Run upload
            await pipeline.ManualUpload(pub, uploads).ConfigureAwait(false);

            //Publish feeds
            if (feed is not null)
            {
                console.WithForegroundColor(ConsoleColor.DarkGreen, static o => o.Output.WriteLine("Uploading feeds..."));

                //Exec feed upload
                await uploads.UploadDirectoryAsync(feed.FeedOutputDir);
            }

            console.WithForegroundColor(ConsoleColor.Green, static o => o.Output.WriteLine("Upload build complete"));
        }

        public override IFeedManager[] Feeds => SleetPath is null ? [] : [SleetFeedManager.GetSleetFeed(SleetPath)];
    }
}