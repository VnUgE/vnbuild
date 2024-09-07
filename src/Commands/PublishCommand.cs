
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Typin.Console;
using Typin.Attributes;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Publishing;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("publish", Description = "Runs publishig build steps on a completed build")]
    public sealed class PublishCommand(BuildPipeline pipeline, ConfigManager bm) : BaseCommand(pipeline, bm)
    {
        [CommandOption("minio", Description = "The path to upload the build artifacts")]
        public string? MinioPath { get; set; }

        [CommandOption("ftp", Description = "The FTP server address to upload the build artifacts. Enables FTP mode over s3")]
        public string? FtpServerAddress { get; set; }

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
            (Config.Index as Dirs)!.OutputDir = BuildDirs.GetOrCreateDir(Constants.Config.OUTPUT_DIR, CustomOutDir);

            IUploadManager uploads = GetUploadManager(console);
            IFeedManager? feed = Feeds.FirstOrDefault();

            //Optional gpg signer for signing published artifacts
            BuildPublisher pub = new(Config, new GpgSigner(Sign, GpgKey));

            console.WithForegroundColor(
                ConsoleColor.DarkGreen, 
                static o => o.Output.WriteLine("Publishing modules")
            );

            //Run publish steps
            await pipeline.OnPublishingAsync()
                .ConfigureAwait(false);

            console.WithForegroundColor(
                ConsoleColor.DarkGreen, 
                static o => o.Output.WriteLine("Preparing module output for upload")
            );

            //Prepare the output 
            await pipeline.PrepareOutputAsync(pub)
                .ConfigureAwait(false);

            //Run upload
            await pipeline.ManualUpload(pub, uploads)
                .ConfigureAwait(false);

            //Publish feeds
            if (feed is not null)
            {
                console.WithForegroundColor(
                    ConsoleColor.DarkGreen, 
                    static o => o.Output.WriteLine("Uploading feeds...")
                );

                //Exec feed upload
                await uploads.UploadDirectoryAsync(feed.FeedOutputDir);
            }

            console.WithForegroundColor(
                ConsoleColor.Green, 
                static o => o.Output.WriteLine("Upload build complete")
            );
        }

        public override IFeedManager[] Feeds => SleetPath is null ? [] : [SleetFeedManager.GetSleetFeed(SleetPath)];

        private MultiUploadManager GetUploadManager(IConsole console)
        {
            try
            {
                IUploadManager[] uploadMan = [];

                if (!string.IsNullOrWhiteSpace(MinioPath))
                {
                    console.Output.WriteLine("Creating Minio publisher");

                    uploadMan = [MinioUploadManager.Create(MinioPath), ..uploadMan];
                }
                
                if (!string.IsNullOrWhiteSpace(FtpServerAddress))
                {
                    console.Output.WriteLine("Using FTP publisher");

                    uploadMan = [FtpUploadManager.Create(FtpServerAddress), .. uploadMan];
                }

                if(uploadMan.Length == 0)
                {
                    console.WithForegroundColor(
                        ConsoleColor.DarkYellow,
                        static o => o.Output.WriteLine("No upload manager specified, output will be skipped")
                    );
                }

                return new MultiUploadManager(uploadMan);
            }
            catch(UriFormatException urie)
            {
                throw new BuildFailedException("Invalid server address", urie);
            }
        }

        private sealed class MultiUploadManager(params IUploadManager[] managers) : IUploadManager
        {
            private readonly IUploadManager[] _managers = managers;

            public async Task UploadDirectoryAsync(string path)
            {
                IEnumerable<Task> tasks = _managers.Select(m => m.UploadDirectoryAsync(path));
                
                await Task.WhenAll(tasks);
            }
        }
    }
}