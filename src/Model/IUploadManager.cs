using System.Threading.Tasks;

namespace VNLib.Tools.Build.Executor.Model
{
    public interface IUploadManager
    {
        Task CleanAllAsync(string path);

        Task DeleteFileAsync(string filePath);

        Task UploadDirectoryAsync(string path);

        Task UploadFileAsync(string filePath);
    }
}