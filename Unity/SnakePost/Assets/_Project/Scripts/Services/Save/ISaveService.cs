#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace _Project.Scripts.Services.Save
{
    public interface ISaveService
    {
        ValueTask<T> LoadAsync<T>(SaveKey<T> key, CancellationToken cancellationToken = default) where T : notnull;

        ValueTask SaveAsync<T>(SaveKey<T> key, T value, CancellationToken cancellationToken = default)
            where T : notnull;

        ValueTask FlushAsync(CancellationToken cancellationToken = default);
    }
}