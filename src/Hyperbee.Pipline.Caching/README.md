# Hyperbee.Pipeline.Caching

The `Hyperbee.Pipeline.Caching` library is a set of extentsions to `Hyperbee.Pipeline` that adds support for caching within a pipeline using `Microsoft.Extensions.Caching` libraries.

## Examples
For simple pipelines the previous step's return value can be used as the key:

```csharp
// Takes a string and returns a number
var command = PipelineFactory
    .Start<string>()
    .PipeCacheAsync( CharacterCountAsync )
    .Build();

var result = await command( factory.Create( logger ), "test" );

Assert.AreEqual( 4, result );
```

Or for more complex the options callback can be used to customize how the results will be cached.

```csharp
// Takes a string and returns a number
var command = PipelineFactory
    .Start<string>()
    .PipeCacheAsync( CharacterCountAsync,
        ( input, options ) =>
        {
            options.Key = $"custom/{input}";
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours( 1 );
            return options;
        } )
    .Build();

var result = await command( factory.Create( logger ), "test" );

Assert.AreEqual( 4, result );
```

When a set of steps should cache the overload that takes a builder can be used.  In this case the inital input will be used at the key and the final step of the nested pipeline will be cached

```csharp
// Takes a string and returns a number
var command = PipelineFactory
    .Start<string>()
    .PipeCacheAsync( b => b
        .PipeAsync( CharacterCountAsync )
        .Pipe( (ctx, arg) => arg + 100 ))
    .Build();

var result = await command( factory.Create( logger ), "test" );

Assert.AreEqual( 104, result );
```

## Dependacy Injection

Because this uses the existing DI built into pipelines, caching can be configured with an existing cache:

```csharp
// Add Memory Cache
services.AddMemoryCache();

// Share with the pipelines
services.AddPipeline( includeAllServices: true );
```

Or defined seperately as part of the container use for the pipelines:

```csharp
services.AddPipeline( (factoryServices, rootProvider) =>
{
    factoryServices.AddMemoryCache();
    factoryServices.AddPipelineDefaultCacheSettings( absoluteExpirationRelativeToNow: TimeSpan.FromHours( 1 ) )
} );
```