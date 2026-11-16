using System.Threading;
using System.Threading.Tasks;

namespace _Project.Scripts.Services.Save
{
    public interface ISaveService
    {
        Task<T> GetAsync<T>(string key, CancellationToken ct = default);

        Task SetAsync<T>(string key, T value, CancellationToken ct = default);
    }
}