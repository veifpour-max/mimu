using System.Threading.Tasks;

namespace LocalMimu.Models;

public interface IStorageService
{
    Task<string?> PickFileAsync();
}