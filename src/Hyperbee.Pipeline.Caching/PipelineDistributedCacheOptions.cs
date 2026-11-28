using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Hyperbee.Pipeline.Caching;

public class PipelineDistributedCacheOptions : DistributedCacheEntryOptions, IOptions<PipelineDistributedCacheOptions>
{
    public string Key { get; set; }
    public PipelineDistributedCacheOptions Value => this;
}
