using System.Threading.Tasks;

namespace VNLib.Tools.Build.Executor.Model
{
    public interface IUploadManager
    {
        Task UploadDirectoryAsync(string path);
    }
}