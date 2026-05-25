namespace LocalMimu.Repositories;

public interface IStorage
{
    Task Save(string fileName, string data);
    Task<string> Load(string fileName);
    Task<bool> Exists(string fileName);
}