using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

using LibGit2Sharp;

using Semver;

using VNLib.Tools.Build.Executor.Constants;
using VNLib.Tools.Build.Executor.Model;
using VNLib.Tools.Build.Executor.Extensions;
using VNLib.Tools.Build.Executor.Projects;
using static VNLib.Tools.Build.Executor.Constants.Config;

namespace VNLib.Tools.Build.Executor.Publishing
{

    public sealed class BuildPublisher(BuildConfig config, GpgSigner signer)
    {
        public bool SignEnabled => signer.IsEnabled;

        /// <summary>
        /// Prepares the module output and its collection of file details for publishing
        /// then runs the upload step
        /// </summary>
        /// <param name="module"></param>
        /// <returns>A task that completes when the module's output has been created</returns>
        public async Task PrepareModuleOutput(IModuleData module)
        {
            //Copy project artifacts to output directory
            await CopyProjectOutputToModuleOutputAsync(module);

            //Copy source archive
            string? archiveFile = await CopySourceArchiveToOutput(module, module.FileManager);

            Log.Information("Building module {mod} catalog and git history", module.ModuleName);

            //Build module catalog
            await BuildModuleCatalogAsync(module, config.SemverStyle, archiveFile);

            Log.Information("Building module {mod} git history", module.ModuleName);

            //Build git history
            await BuildModuleGitHistoryAsync(module);

            Log.Information("Building module {mod} version history", module.ModuleName);

            //build version history
            await BuildModuleVersionHistory(module);

            Log.Information("Moving module {mod} artifacts to the output", module.ModuleName);
        }

        /// <summary>
        /// Uploads the modules output to the remote
        /// </summary>
        /// <param name="module">The module containing the information to upload</param>
        /// <returns></returns>
        public Task UploadModuleOutput(IUploadManager Uploader, IModuleData module)
        {
            Log.Information("Uploading module {mod}", module.ModuleName);

            //Upload the entire output directory
            return Uploader.UploadDirectoryAsync(module.FileManager.OutputDir);
        }

        /*
        * Builds the project catalog file and publishes it to the module file manager
        */
        private async Task BuildModuleCatalogAsync(IModuleData mod, SemVersionStyles style, string? archiveFile)
        {
            /*
             * Builds the index.json file for the module. It 
             * contains an array of projects and their metadata
             */

            string moduleVersion = mod.GetModuleCiVersion(config.DefaultCiVersion, style);

            using MemoryStream ms = new();

            using (Utf8JsonWriter writer = new(ms))
            {
                //Open initial object
                writer.WriteStartObject();

                InitModuleFile(writer, mod);

                //Add the archive path if it was created in the module
                if (archiveFile != null)
                {
                    writer.WriteStartObject("archive");

                    //Archive path is in the build directory
                    writer.WriteString("path", config.SourceArchiveName);

                    //Get the checksum of the archive
                    string checksum = await new FileInfo(archiveFile).ComputeFileHashStringAsync();
                    writer.WriteString(config.HashFuncName, checksum);
                    writer.WriteString("sha_file", $"{config.SourceArchiveName}.{config.HashFuncName}");

                    //If signing is enabled, add the signature file (it is constant)
                    if (SignEnabled)
                    {
                        writer.WriteString("signature", $"{config.SourceArchiveName}.sig");
                    }

                    writer.WriteEndObject();
                }

                //Build project array
                writer.WriteStartArray("projects");

                foreach (IProject project in mod.Projects)
                {
                    //start object for each project
                    writer.WriteStartObject();

                    //Write the project info
                    await WriteProjectInfoAsync(mod.FileManager, project, mod.Repository.Head.Tip.Sha, moduleVersion, writer);

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                //Close object
                writer.WriteEndObject();

                writer.Flush();
            }

            ms.Seek(0, SeekOrigin.Begin);

            await mod.FileManager.WriteFileAsync(ModuleFileType.Catalog, ms.ToArray());
        }

        private static async Task BuildModuleVersionHistory(IModuleData mod)
        {
            /*
             * Builds the index.json file for the module. It 
             * contains an array of projects and their metadata
             */

            using MemoryStream ms = new();

            using (Utf8JsonWriter writer = new(ms))
            {
                //Open initial object
                writer.WriteStartObject();

                InitModuleFile(writer, mod);

                //Set the head pointer to the latest commit we build
                writer.WriteString("head", mod.Repository.Head.Tip.Sha);

                //Build project array
                writer.WriteStartArray("versions");

                //Write all git hashes from head back to the first commit
                foreach (Commit commit in mod.Repository.Commits)
                {
                    writer.WriteStringValue(commit.Sha);
                }

                writer.WriteEndArray();

                //Releases will be an array of objects containing the tag and the hash
                writer.WriteStartArray("releases");

                //Write all git tags
                foreach (Tag tag in mod.Repository.Tags.OrderByDescending(static p => p.FriendlyName))
                {
                    writer.WriteStartObject();

                    writer.WriteString("tag", tag.FriendlyName);
                    writer.WriteString("hash", tag.Target.Sha);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();

                //Close object
                writer.WriteEndObject();

                writer.Flush();
            }

            ms.Seek(0, SeekOrigin.Begin);

            await mod.FileManager.WriteFileAsync(ModuleFileType.VersionHistory, ms.ToArray());
        }


        /*
         * Builds the project's git history file and publishes it to the module file manager
         * 
         * Also updates the latest hash file
         */
        private static async Task BuildModuleGitHistoryAsync(IModuleData mod)
        {
            using MemoryStream ms = new();

            using (Utf8JsonWriter writer = new(ms))
            {
                //Open initial object
                writer.WriteStartObject();

                InitModuleFile(writer, mod);

                //Write the head commit
                writer.WriteStartObject("head");
                writer.WriteString("branch", mod.Repository.Head.FriendlyName);
                WriteSingleCommit(writer, mod.Repository.Head.Tip);
                writer.WriteEndObject();

                //Write commit history
                WriteCommitHistory(writer, mod.Repository);

                //Close object
                writer.WriteEndObject();

                writer.Flush();
            }

            ms.Seek(0, SeekOrigin.Begin);

            await mod.FileManager.WriteFileAsync(ModuleFileType.GitHistory, ms.ToArray());

            await mod.FileManager.WriteFileAsync(
                ModuleFileType.LatestHash, 
                Encoding.UTF8.GetBytes(mod.Repository.Head.Tip.Sha)
            );
        }

        /*
         * Captures all of the project artiacts and copies them to the module output directory
         */
        private async Task CopyProjectOutputToModuleOutputAsync(IModuleData mod)
        {
            //Copy build artifacts to module output directory
            await mod.Projects.RunAllAsync(project =>
            {
                //Get all output files from the project build, and copy them to the module output directory
                return project.GetProjectBuildFiles(config)
                .RunAllAsync(artifact => mod.FileManager.CopyArtifactToOutputAsync(project, artifact));
            });

            /*
             * If signing is enabled, we can sign all project files synchronously
             */
            if (SignEnabled)
            {
                Log.Information("GPG Siginig is enabled, signing all artifacts for module {mod}", mod.ModuleName);

                /*
                 * Get all of the artifacts from the module's projects that match the target output 
                 * file type, and sign them
                 */
                IEnumerable<FileInfo> artifacts = mod.Projects.SelectMany(
                            p => mod.FileManager.GetArtifactOutputDir(p)
                            .EnumerateFiles(config.OutputFileType, SearchOption.TopDirectoryOnly)
                        );

                //Sign synchronously
                foreach (FileInfo artifact in artifacts)
                {
                    await signer.SignFileAsync(artifact);
                }
            }
        }


        private static void InitModuleFile(Utf8JsonWriter writer, IModuleData mod)
        {
            //Set object name
            writer.WriteString("module_name", mod.ModuleName);
            //Modified date
            writer.WriteString("modifed_date", DateTime.UtcNow);
        }

        private static void WriteCommitHistory(Utf8JsonWriter writer, Repository repo)
        {
            writer.WriteStartArray("commits");

            //Write commit history for current repo
            foreach (Commit commit in repo.Head.Commits)
            {
                writer.WriteStartObject();

                WriteSingleCommit(writer, commit);

                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            //Write tag history for current repo
            writer.WriteStartArray("tags");

            foreach (Tag tag in repo.Tags)
            {
                //clamp message length and ellipsis if too long
                string? message = tag.Annotation?.Message;
                if (message != null && message.Length > 120)
                {
                    message = $"{message[..120]}...";
                }

                writer.WriteStartObject();
                writer.WriteString("name", tag.FriendlyName);
                writer.WriteString("sha", tag.Target.Sha);
                writer.WriteString("message", message);
                writer.WriteString("author", tag.Annotation?.Tagger.Name);
                writer.WriteString("date", tag.Annotation?.Tagger.When ?? default);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private static void WriteSingleCommit(Utf8JsonWriter writer, Commit commit)
        {
            writer.WriteString("sha", commit.Sha);
            writer.WriteString("message", commit.Message);
            writer.WriteString("author", commit.Author.Name);
            writer.WriteString("commiter", commit.Committer.Name);
            writer.WriteString("date", commit.Committer.When);
            writer.WriteString("message_short", commit.MessageShort);
        }

        /// <summary>
        /// Builds and writes the projects information to the <see cref="Utf8JsonWriter"/>
        /// </summary>
        /// <param name="project"></param>
        /// <param name="writer">
        /// The <see cref="Utf8JsonWriter"/> to write the project
        /// information to
        /// </param>
        /// <returns>A task that completes when the write operation has completed</returns>
        private async Task WriteProjectInfoAsync(IModuleFileManager man, IProject project, string latestSha, string version, Utf8JsonWriter writer)
        {
            //Reload the project dom after execute because semversion may be updated after build step
            if (project is ModuleProject mp)
            {
                await mp.LoadProjectDom();
            }

            writer.WriteString("name", project.ProjectName);
            writer.WriteString("repo_url", project.ProjectData.RepoUrl);
            writer.WriteString("description", project.ProjectData.Description);
            writer.WriteString("version", version);
            writer.WriteString("copyright", project.ProjectData.Copyright);
            writer.WriteString("author", project.ProjectData.Authors);
            writer.WriteString("product", project.ProjectData.Product);
            writer.WriteString("company", project.ProjectData.CompanyName);
            writer.WriteString("commit", latestSha);
            //Write target framework if it exsits
            writer.WriteString("target_framework", project.ProjectData["TargetFramework"]);

            //Start file array
            writer.WriteStartArray("files");

            //Get only tar files, do not include the sha files
            foreach (FileInfo output in GetProjOutputFiles(man, project))
            {
                //beging file object
                writer.WriteStartObject();

                writer.WriteString("name", output.Name);
                writer.WriteString("path", $"{project.GetSafeProjectName()}/{output.Name}");
                writer.WriteString("date", output.LastWriteTimeUtc);
                writer.WriteNumber("size", output.Length);

                //Compute the file hash
                string hashHex = await output.ComputeFileHashStringAsync();
                writer.WriteString(config.HashFuncName, hashHex);

                //Path to sha-file
                writer.WriteString("sha_file", $"{project.GetSafeProjectName()}/{output.Name}.{config.HashFuncName}");

                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private IEnumerable<FileInfo> GetProjOutputFiles(IModuleFileManager man, IProject project)
        {
            return man.GetArtifactOutputDir(project)
                .EnumerateFiles("*.*", SearchOption.TopDirectoryOnly)
                .Where(p => p.Extension != $".{config.HashFuncName}");
        }

        /// <summary>
        /// Copies the source git archive tgz file to the output directory
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="man"></param>
        /// <returns></returns>
        private async Task<string?> CopySourceArchiveToOutput(IModuleData mod, IModuleFileManager man)
        {
            //Try to get a source archive in the module directory
            string? archiveFile = Directory.EnumerateFiles(mod.Repository.Info.WorkingDirectory, config.SourceArchiveName, SearchOption.TopDirectoryOnly).FirstOrDefault();

            //If archive is null ignore and continue
            if (string.IsNullOrWhiteSpace(archiveFile))
            {
                Log.Information("No archive file found for module {mod}", mod.ModuleName);
                return null;
            }

            Log.Information("Found source archive for module {mod}, copying to output...", mod.ModuleName);

            //Otherwise copy to output
            byte[] archive = await File.ReadAllBytesAsync(archiveFile);
            FileInfo output = await man.WriteFileAsync(ModuleFileType.Archive, archive);

            //Compute the hash of the file
            await output.ComputeFileHashAsync(config.HashFuncName);

            if (SignEnabled)
            {
                //Sign the file if signing is enabled
                await signer.SignFileAsync(output);
            }

            return archiveFile;
        }
    }
}