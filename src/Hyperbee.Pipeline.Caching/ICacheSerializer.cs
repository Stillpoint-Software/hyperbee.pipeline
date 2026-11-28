namespace Hyperbee.Pipeline.Caching;

public interface ICacheSerializer
{
    public Task<byte[]> SerializeAsync<T>( T item, CancellationToken cancellationToken = default );
    public Task<T> DeserializeAsync<T>( byte[] bytes, CancellationToken cancellationToken = default );
}
