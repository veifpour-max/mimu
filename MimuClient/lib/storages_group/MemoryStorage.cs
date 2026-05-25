using System.Collections.Generic;

namespace LocalMimu.Repositories;

public class MemoryStorage : IStorage
{
    private Dictionary<string, string> _internalData = new();

    public async Task Save(string fileName, string data)
    {
         _internalData[fileName] = data;
    }

    public async Task<string> Load(string fileName)
    {
        // Достаем данные по ключу. Если нет — вернем пустую строку
         return  _internalData.GetValueOrDefault(fileName) ?? "";
    }

    public async Task<bool> Exists(string fileName)
    {
        return _internalData.ContainsKey(fileName);
    }
}