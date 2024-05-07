using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using Semver;

using VNLib.Tools.Build.Executor.Model;

namespace VNLib.Tools.Build.Executor.Constants
{
    public class ConfigManager(SemVersionStyles semver)
    {
        public async Task<BuildConfig> GetOrCreateConfig(IDirectoryIndex index, bool overwrite)
        {
            //Get the config file path
            string configFilePath = Path.Combine(index.BuildDir.FullName, Config.BUILD_CONFIG);

            BuildConfig? data = new()
            {
                SemverStyle = semver
            };

            //If the file doesnt exist or we want to overwrite it
            if (!File.Exists(configFilePath) || overwrite)
            {
                //Create a new config file
                await using FileStream fs = File.Create(configFilePath);
                await JsonSerializer.SerializeAsync(fs, data);
            }
            else
            {
                await using FileStream fs = File.OpenRead(configFilePath);
                data = await JsonSerializer.DeserializeAsync<BuildConfig>(fs);
            }

            data!.Index = index;

            return data;
        }
    }
}