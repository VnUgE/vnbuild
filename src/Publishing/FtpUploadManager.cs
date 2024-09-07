using System;
using System.IO;

using FluentFTP;

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor.Publishing
{
    internal sealed class FtpUploadManager(AsyncFtpClient client, string remotePath) : IUploadManager
    {
        public async Task UploadDirectoryAsync(string path)
        {
            path = Directory.GetParent(path)!.FullName;

            await client.AutoConnect();

            var res = await client.UploadDirectory(
                localFolder: path,
                remoteFolder: remotePath,
                FtpFolderSyncMode.Update,
                FtpRemoteExists.Overwrite,
                FtpVerify.Throw | FtpVerify.Retry
            );

            foreach(FtpResult fileResult in res)
            {
                switch (fileResult.ToStatus())
                {
                    case FtpStatus.Success:
                        Config.Log.Information(
                            "Uploaded {size} bytes, {0} -> {1}",
                            fileResult.Size, 
                            fileResult.LocalPath, 
                            fileResult.RemotePath
                        );
                        break;

                    case FtpStatus.Skipped:
                        Config.Log.Information("Skipped {0} -> {1}", fileResult.LocalPath, fileResult.RemotePath);
                        break;

                    case FtpStatus.Failed:
                        Config.Log.Warning(
                            "Failed to upload {0}, reason: {exp}", 
                            fileResult.LocalPath, 
                            fileResult.Exception?.Message
                        );
                        break;  
                }
            }
        }

        [return: NotNullIfNotNull(nameof(serverAddress))]
        public static IUploadManager? Create(string? serverAddress)
        {
            if(string.IsNullOrWhiteSpace(serverAddress))
            {
                return null;
            }

            //Convert to uri, this may throw but this is currently the best way to validate the address
            Uri serverUri = new(serverAddress);

            //Initlaize the client
            AsyncFtpClient client = new()
            {
                Host = serverUri.Host,
                Port = serverUri.Port,
                

                Config = new()
                {
                    LogToConsole = Config.Log.IsEnabled(Serilog.Events.LogEventLevel.Verbose),

                    //Disable senstive logging in case running in automated CI pipelines where logs may be published
                    LogUserName = false,
                    LogPassword = false,
                    //EncryptionMode = FtpEncryptionMode.Auto,
                    RetryAttempts = 3,
                },

                Credentials = new()
                {
                    //Pull credentials from the environment instead of command line
                    UserName = Environment.GetEnvironmentVariable("FTP_USERNAME"),
                    Password = Environment.GetEnvironmentVariable("FTP_PASSWORD")
                },
            };

            return new FtpUploadManager(client, serverUri.LocalPath);
        }
    }
}