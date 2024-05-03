using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Hyperbee.Pipeline.Caching;

public class PipelineMemoryCacheOptions : MemoryCacheEntryOptions, IOptions<PipelineMemoryCacheOptions>
{
    public object Key { get; set; }
    public PipelineMemoryCacheOptions Value => this;
}
