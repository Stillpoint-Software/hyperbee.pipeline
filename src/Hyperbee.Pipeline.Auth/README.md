# Hyperbee.Pipeline.Claims

The `Hyperbee.Pipeline.Auth` library is a set of extentsions to `Hyperbee.Pipeline` that adds support for authorization within the pipeline.


## Examples

```csharp
// Will return the claim if available
var command = PipelineFactory
    .Start<string>()
    .PipeIfClaim( Claim )
    .Build();

```

```csharp
// WithAuth takes a function to validate the claim. 
var command = PipelineFactory
    .Start<string>()
    .WithAuth( ValidateClaim )
    .Build();

private async Task<bool> ValidateClaim( IPipelineContext context, string roleValue, ClaimsPrincipal claimsPrincipal )
    {
        return claimsPrincipal.HasClaim( x => x.Value == roleValue );
    }
```

## Dependency Injection

Sometimes Pipelines and Pipeline middleware need access to specific container services. This can be
accomplished by registering services with the `PipelineContextFactory`. This can be done through
DI configuration, or manually through the `PipelineContextFactoryProvider` if you are not using DI.

Pipelines manage dependencies with a specialized container. This allows the implementor to control
the services that are exposed through the pipeline. If you want to expose all application
services then you can call `AddPipeline` and pass `includeAllServices: true`. 

Register pipelines with DI and provide Pipeline dependencies using the application container.

```csharp
services.AddPipeline( includeAllServices: true );
```

Register Pipelines with DI and provide Pipeline dependencies using a specialized container.

```csharp
services.AddPipeline( (factoryServices, rootProvider) =>
{
    factoryServices.AddTransient<IThing>()
    factoryServices.ProxyService<IPrincipalProvider>( rootProvider ); // pull from root container
} );
```

```csharp
// Add httpContextAccessor if using web api
 services.AddHttpContextAccessor();

// Add with the pipelines
services.AddClaimPrincipalAccessor();
```

Or create your own claims principal use for the pipelines:

```csharp
services.AddPipeline( (factoryServices, rootProvider) =>
{
    factoryServices.AddClaimPrincipalAccessor( IClaimsPrincipal claimsPrincipal )
} );
```

## Advanced Features

The `PipelineFactory` library provides a variety of helper methods that allow you to customize the behavior of your pipelines. These methods provide powerful functionality for manipulating data as it passes through the pipeline.

### Reduce

The `Reduce` and `ReduceAsync` methods allow you to reduce a sequence of elements to a single value. You can specify a reducer function that defines how the elements should be combined, and a builder function that creates the pipeline for processing the elements.

### WaitAll

The `WaitAll` method allows you to wait for all pipelines to complete before continuing. You can specify a set of builders that create the pipelines to wait for, a reducer function that combines the results of the pipelines.

```csharp
var count = 0;

var command = PipelineFactory
    .Start<int>()
    .WaitAll( builders => builders.Create(
            builder => builder.Pipe( ( ctx, arg ) => Interlocked.Increment( ref count ) ),
            builder => builder.Pipe( ( ctx, arg ) => Interlocked.Increment( ref count ) )
        ),
        reducer: ( ctx, arg, results ) => { return arg + results.Sum( x => (int) x.Result ); }
    )
    .Build();

var result = await command( new PipelineContext() );

Assert.AreEqual( 2, count );
Assert.AreEqual( 3, result );
```

### PipeIf

The `PipeIf` method allows you to conditionally add a step to the pipeline. You can specify a condition function that determines whether the step should be added, a builder function that creates the step, and an optional flag indicating whether middleware should be inherited.

### ForEach and ForEachAsync

The `ForEach` and `ForEachAsync` methods allow you to apply a pipeline to each element in a sequence. You can specify a builder function that creates the pipeline for processing the elements.

```csharp
var count = 0;

var command = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => arg.Split( ' ' ) )
    .ForEach<string>( builder => builder
        .Pipe( ( ctx, arg ) => count += 10 )
    )
    .Pipe( ( ctx, arg ) => count += 5 )
    .Build();

await command( new PipelineContext(), "e f" );

Assert.AreEqual( count, 25 );
```

### Call and CallAsync

The `Call` and `CallAsync` methods allow you to add a procedure to the pipeline. You can think of these as `Action<T>` and `Pipe` like `Func<T>`.

In this example notice that `arg + 9` is not returned from the use of `Call`

```csharp
var callResult = string.Empty;

var command = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => arg + "1" )
    .Pipe( ( ctx, arg ) => arg + "2" )
    .Call( builder => builder
        .Call( ( ctx, arg ) => callResult = arg + "3" )
        .Pipe( ( ctx, arg ) => arg + "9" )
    )
    .Pipe( ( ctx, arg ) => arg + "4" )
    .Build();

var result = await command( new PipelineContext() );

Assert.AreEqual( "124", result );
Assert.AreEqual( "123", callResult );
```

### Chaining Child Pipelines

The `PipelineFactory` library allows you to chain pipelines together. Since pipelines are just functions, they can be used as input to other pipelines. This allows you to create complex data processing flows by reusing and chaining together multiple pipelines.

Here's an example of how to chain pipelines together:

```csharp
var command2 = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => $"{arg} again!" )
    .Build();

var command1 = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => $"hello {arg}" )
    .PipeAsync( command2 )
    .Build();

var result = await command1( new PipelineContext(), "pipeline" );

Assert.AreEqual( "hello pipeline again!", result );
```

## Additional Documentation 
Classes for building composable async pipelines supporting:

  * [Middleware](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/blob/main/docs/middleware.md)
  * [Conditional flow](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/blob/main/docs/execution.md)
  * [Dependency Injection](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/blob/main/docs/dependencyInjection.md)
  * Value projections
  * Early returns
  * Child pipelines

