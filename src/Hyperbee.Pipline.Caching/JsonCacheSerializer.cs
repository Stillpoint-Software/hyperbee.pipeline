using System.Text.Json;

namespace Hyperbee.Pipeline.Caching;

public class JsonCacheSerializer : ICacheSerializer
{
    public async Task<byte[]> SerializeAsync<T>( T item, CancellationToken cancellationToken = default )
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync( stream, item, cancellationToken: cancellationToken );
        return stream.ToArray();
    }

    public async Task<T> DeserializeAsync<T>( byte[] bytes, CancellationToken cancellationToken = default )
    {
        using var stream = new MemoryStream( bytes );
        return await JsonSerializer.DeserializeAsync<T>( stream, cancellationToken: cancellationToken );
    }
}
