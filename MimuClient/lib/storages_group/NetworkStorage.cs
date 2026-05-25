namespace LocalMimu.Repositories;

public class NetworkStorage : IStorage
{
    private readonly FileStorage _fileStorage = new();
    public async Task Save(string fileName, string data)
    {
        await Task.Delay(1000);
        await _fileStorage.Save(fileName, data);
    }
    public async Task<string> Load(string fileName)
    {
        await Task.Delay(1000);
        return await _fileStorage.Load(fileName);
    }
    public async Task<bool> Exists(string fileName)
    {
        await Task.Delay(1000);
        return await _fileStorage.Exists(fileName);

    }




}