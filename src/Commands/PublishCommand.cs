
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Typin.Console;
using Typin.Attributes;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Publishing;

namespace VNLib.Tools.Build.Executor.Commands
{
    [Command("publish", Description = "Runs publishig build steps on a completed build")]
    public sealed class PublishCommand : BaseCommand
    {
        [CommandOption("minio", Description = "The target path to upload artifacts to")]
        public string? MinioPath { get; set; }

        [CommandOption("ftp", Description = "The FTP server address to upload the build artifacts. Enables FTP mode over s3")]
        public string? FtpServerAddress { get; set; }

        [CommandOption("sign", Description = "Enables gpg signing of build artifacts")]
        public bool Sign { get; set; } = false;

        [CommandOption("gpg-key", Description = "Optional key to use when signing, otherwise uses the GPG default signing key")]
        public string? GpgKey { get; set; }

        [CommandOption("output", 'o', Description = "Specifies the output directory for the published modules")]
        public string? CustomOutDir { get; set; }

        public override async ValueTask ExecStepsAsync(IConsole console, BuildPipeline pipeline)
        {
            IUploadManager uploads = GetUploadManager(console);

            //Optional gpg signer for signing published artifacts
            BuildPublisher pub = new(Config, signer: new GpgSigner(Config, Sign, GpgKey));

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

            console.WithForegroundColor(
                ConsoleColor.Green, 
                static o => o.Output.WriteLine("Upload build complete")
            );
        }

        private IUploadManager GetUploadManager(IConsole console)
        {
            try
            {
                IUploadManager[] uploadMan = [];

                if (!string.IsNullOrWhiteSpace(CustomOutDir))
                {
                    console.WithForegroundColor(
                        ConsoleColor.Yellow,
                        o => o.Output.WriteLine($"Writing module output to {CustomOutDir}. Upload is disabled")
                    );
                    return new LocalFileUploadManager(CustomOutDir);
                }

                if (!string.IsNullOrWhiteSpace(MinioPath))
                {
                    console.Output.WriteLine("Creating Minio publisher");

                    uploadMan = [new MinioUploadManager(Config, MinioPath), ..uploadMan];
                }
                
                if (!string.IsNullOrWhiteSpace(FtpServerAddress))
                {
                    console.Output.WriteLine("Using FTP publisher");

                    uploadMan = [FtpUploadManager.Create(Config, FtpServerAddress), .. uploadMan];
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

        private sealed class LocalFileUploadManager(string outputDir) : IUploadManager
        {
            public Task UploadDirectoryAsync(string path)
            {
                //Create all output directories
                Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories)
                    .Select(d => new DirectoryInfo(d))
                    .Select(dir => dir.FullName.Replace(path, outputDir))
                    .ToList()
                    .ForEach(d => Directory.CreateDirectory(d));

                //Copy all files
                Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
                    .Select(f => new FileInfo(f))
                    .ToList()
                    .ForEach(file =>
                    {
                        string dest = file.FullName.Replace(path, outputDir);
                        file.CopyTo(dest, true);
                    });

                return Task.CompletedTask;
            }
        }
    }
}