using System.IO;
using System.Threading.Tasks;

namespace LocalMimu.Repositories;

public class FileStorage : IStorage
{
   public async Task Save(string fileName, string info)
    {
        await File.WriteAllTextAsync(fileName, info);
    }
    public async Task<string> Load(string fileName)
    {
       return await File.ReadAllTextAsync(fileName);
    }
    public Task<bool> Exists(string fileName)
    {
       return Task.FromResult(File.Exists(fileName)); 
    }
}