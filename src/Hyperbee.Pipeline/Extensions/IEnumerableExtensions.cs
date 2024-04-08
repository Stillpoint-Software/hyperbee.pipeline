using System.Collections.Concurrent;

namespace Hyperbee.Pipeline.Extensions;

internal static class EnumerableExtensions
{
    public static Task ForEachAsync<T>( this IEnumerable<T> source, Func<T, Task> function, int maxDegreeOfParallelism = 0 )
    {
        if ( maxDegreeOfParallelism <= 0 )
            maxDegreeOfParallelism = Environment.ProcessorCount;

        return Task.WhenAll( Partitioner
            .Create( source )
            .GetPartitions( maxDegreeOfParallelism )
            .Select( partition => Task.Run( async () =>
            {
                using var enumerator = partition;
                while ( partition.MoveNext() )
                {
                    await function( partition.Current ).ConfigureAwait( false );
                }
            } ) ) );
    }
}