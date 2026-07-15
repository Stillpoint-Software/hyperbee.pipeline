# Hyperbee Pipeline

`Hyperbee.Pipeline` allows you to construct asynchronous fluent pipelines in .NET. A pipeline, in this context, refers to a 
sequence of data processing elements arranged in series, where the output of one element serves as the input for the subsequent 
element.

Hyperbee pipelines are composable, reusable, and easy to test. They are designed to be used in a variety of scenarios, such 
as data processing, message handling, and workflow automation.

Some key features are:

* Middleware
* Hooks and Wraps
* Middleware providers for cross-cutting concerns
* Conditional flows
* Loops
* Parallel processing
* Dependency injection
* Early returns and cancellation
* Child pipelines
* Declarative and imperative validation, with a FluentValidation adapter
* Command pattern with assembly scanning and DI registration
* ASP.NET Core integration with RFC 7807 result mapping (`ToResult()`)

## Why Use Pipelines

Pipelines provide a structured approach to managing complex processes, promoting [SOLID](https://en.wikipedia.org/wiki/SOLID)
principles, including Inversion of Control (IoC) and Separation of Concerns (SoC). They enable composability, making it easier
to build, test, and maintain your code. By extending the benefits of middleware and request-response pipelines throughout your 
application, you achieve greater modularity, scalability, and flexibility. This is particularly important in domains that demand
compliance, auditing, strong identity and role management, or high-security standards—where clear boundaries and responsibilities
are essential. Hyperbee.Pipeline ensures that the advantages of pipelines and middleware are not abandoned at the controller
implementation, addressing a common gap in many frameworks. By using a functional approach, Hyperbee.Pipeline ensures that your
pipelines are not only robust and maintainable but also highly adaptable to changing requirements.


## Getting Started

To get started with Hyperbee.Pipeline, refer to the [documentation site](https://stillpoint-software.github.io/hyperbee.pipeline) for 
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

## Middleware Providers

Use `IPipelineMiddlewareProvider` to define cross-cutting middleware (logging, error mapping, etc.) in
one place and inject it via DI. The `PipelineFactory.Create` convenience method applies hooks and wraps
automatically:

```csharp
var command = PipelineFactory.Create<string, string>( middlewareProvider, builder =>
    builder
        .Pipe( ( ctx, arg ) => $"hello {arg}" )
        .Pipe( ( ctx, arg ) => $"{arg}!" )
);
```

For explicit control, use `UseHooks` and `UseWraps` builder extensions. See the
[Middleware documentation](https://stillpoint-software.github.io/hyperbee.pipeline/middleware.html)
for details.

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

## Validation and Error Handling

Pipelines validate their inputs declaratively, and commands surface failure state as data —
a command returns a result plus diagnostics instead of throwing:

```csharp
var pipeline = PipelineFactory
    .Start<CreateUserRequest>()
    .ValidateAsync()                        // FluentValidation or custom validators
    .PipeAsync( ( ctx, request ) => CreateUser( request ) )
    .Build();
```

Project the diagnostics at your boundary: `ToResult()` maps validation failures to RFC 7807
responses (422/404/403/401) at the HTTP edge, while `context.ThrowIfError()` and
`context.ThrowIfInvalid()` surface errors and validation failures to in-process callers. See the
[Validation documentation](https://stillpoint-software.github.io/hyperbee.pipeline/validation.html)
for details.

## Packages

| Package | Description |
|---------|-------------|
| `Hyperbee.Pipeline` | Core pipeline builders, commands, middleware, and context |
| `Hyperbee.Pipeline.Validation` | Framework-agnostic validation: `ValidateAsync`, failure types, `ThrowIfInvalid` |
| `Hyperbee.Pipeline.Validation.FluentValidation` | FluentValidation adapter with assembly scanning |
| `Hyperbee.Pipeline.AspNetCore` | `ToResult()` command-result mapping for minimal APIs, with customizable `ResultMapper` |
| `Hyperbee.Pipeline.Auth` | Claims-based pipeline steps (`WithAuth`, `PipeIfClaim`) |
| `Hyperbee.Pipeline.Caching` | Memory and distributed cache pipeline steps |
| `Hyperbee.Pipeline.TestHelpers` | Test fixtures and `CommandResult` helpers |

## Credits

Hyperbee.Pipeline is built upon the great work of several open-source projects. Special thanks to:

- [Just The Docs](https://github.com/just-the-docs/just-the-docs) for the documentation theme.

## Contributing

We welcome contributions! Please see our [Contributing Guide](https://github.com/Stillpoint-Software/.github/blob/main/.github/CONTRIBUTING.md) 
for more details.


# Status

| Branch     | Action                                                                                                                                                                                                                      |
|------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `main`     | [![Build status](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/actions/workflows/pack_publish.yml/badge.svg)](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/actions/workflows/pack_publish.yml)                 |
