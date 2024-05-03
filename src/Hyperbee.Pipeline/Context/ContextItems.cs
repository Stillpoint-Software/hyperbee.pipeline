using System.Collections.Concurrent;

namespace Hyperbee.Pipeline.Context;

public class ContextItems
{
    private ConcurrentDictionary<string, object> Items { get; } = new( StringComparer.OrdinalIgnoreCase );

    internal ContextItems()
    {
    }

    public bool TryGetValue<T>( string key, out T value )
    {
        if ( Items.TryGetValue( key, out var item ) )
        {
            value = (T) item;
            return true;
        }

        value = default;
        return false;
    }

    public void SetValue<T>( string key, T value )
    {
        Items[key] = value;
    }

    public bool Contains( string key )
    {
        return Items.ContainsKey( key );
    }
}
