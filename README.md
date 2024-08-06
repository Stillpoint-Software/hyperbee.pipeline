# Hyperbee Pipeline

`Hyperbee.Pipeline` allows you to construct asynchronous fluent pipelines in .NET. A pipeline, in this context, refers to a 
sequence of data processing elements arranged in series, where the output of one element serves as the input for the subsequent 
element.

Hyperbee pipelines are composable, reusable, and easy to test. They are designed to be used in a variety of scenarios, such 
as data processing, message handling, and workflow automation.

Some key features are:

* Middleware
* Hooks
* Wraps
* Conditional flows
* Loops
* Parallel processing
* Dependency injection
* Early returns and cancellation
* Child pipelines

## Why Use Pipelines

Pipelines provide a structured approach to managing complex processes, promoting [SOLID](https://en.wikipedia.org/wiki/SOLID)
principles, including Inversion of Control (IoC) and Separation of Concerns (SoC). They enable composability, making it easier
to build, test, and maintain your code. By extending the benefits of middleware and request-response pipelines throughout your 
application, you achieve greater modularity, scalability, and flexibility. This is especially critical in domains such as 
healthcare, compliance auditing, identity and roles, and high-security environments where clear boundaries and responsibilities
are essential. Hyperbee.Pipeline ensures that the advantages of pipelines and middleware are not abandoned at the controller
implementation, addressing a common gap in many frameworks. By using a functional approach, Hyperbee.Pipeline ensures that your
pipelines are not only robust and maintainable but also highly adaptable to changing requirements.


## Getting Started

To get started with Hyperbee.Json, refer to the [documentation](https://stillpoint-software.github.io/hyperbee.pipeline) for 
detailed instructions and examples. 

Install via NuGet:

```bash
dotnet add package Hyperbee.Pipeline
```

## Building and Executing Pipelines

Pipelines are built using `PipelineFactory`. Once built, a pipeline is just an async function that takes a `PipelineContext` and 
an optional input value as parameters, and returns a result. 

```csharp
    var command = PipelineFactory
        .Start<string>()
        .Pipe( ( ctx, arg ) => $"hello {arg}" )
        .Build();

    var result = await command( new PipelineContext(), "pipeline" );

    Assert.AreEqual( "hello pipeline", result );
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

## Pipeline of Pipelines

The `PipelineFactory` library allows you to use pipelines together. Since pipelines are just functions, they can be used 
as input to other pipelines. This allows you to create complex data processing flows by reusing and chaining together
multiple pipelines.

Here's an example of how to use pipelines together:

```csharp
var pipeline2 = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => $"{arg} again!" )
    .Build();

var pipeline1 = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => $"hello {arg}" )
    .PipeAsync( pipeline2 )
    .Build();

var result = await pipeline1( new PipelineContext(), "you" );

Assert.AreEqual( "hello you again!", result );
```

## Conditional Flow and Advanced Features

The `PipelineFactory` library provides a variety of builders that allow you to customize the behavior of your pipelines. 
These methods provide powerful functionality for manipulating data as it passes through the pipeline.

- Functions
- Procedures
- Conditional Flow
- Iterators
- Reduce
- Parallel execution

## Credits

Hyperbee.Pipeline is built upon the great work of several open-source projects. Special thanks to:

- [Just The Docs](https://github.com/just-the-docs/just-the-docs) for the documentation theme.

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/Stillpoint-Software/.github/blob/main/.github/CONTRIBUTING.md) 
for more details.
