using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

using VNLib.Tools.Build.Executor.Constants;

namespace VNLib.Tools.Build.Executor
{
    public sealed class GpgSigner(bool enabled, string? defaultKey)
    {
        public bool IsEnabled { get; } = enabled;

        public async Task SignFileAsync(FileInfo file)
        {
            if (!IsEnabled)
            {
                return;
            }

            List<string> args = [ "--detach-sign" ];

            if (!string.IsNullOrWhiteSpace(defaultKey))
            {
                //Set the preferred key
                args.Add("--default-key");
                args.Add(defaultKey);
            }

            //Add input file
            args.Add(file.FullName);

            //Delete an original file 
            string sigFile = $"{file.FullName}.sig";
            if (File.Exists(sigFile))
            {
                File.Delete(sigFile);
            }

            int result = await Utils.RunProcessAsync("gpg", null, args.ToArray());

            switch (result)
            {
                case 2:
                case 0:
                    break;
                default:
                    throw new Exception($"Failed to sign file {file.FullName}");
            }
        }
    }
}