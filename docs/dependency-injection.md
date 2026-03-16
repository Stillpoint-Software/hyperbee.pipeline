---
layout: default
title: Dependency Injection
nav_order: 3
---

# Dependency Injection

## Dependency Injection

To use pipelines with Dependency Injection call `AddPipeline()`, or one of its overloads, when configuring
the application.

### Example 1
Register pipelines with DI using defaults.

```csharp
services.AddPipeline();
```

## Pipeline dependencies

Sometimes Pipelines and Pipeline middleware need access to specific container services. This can be
accomplished by registering services with the `PipelineContextFactory`. This can be done through
DI configuration, or manually through the `PipelineContextFactoryProvider` if you are not using DI.

Pipelines manage dependencies with a specialized container. This allows the implementor to control
the services that are exposed through the pipeline. If you want to expose all application
services then you can call `AddPipeline` and pass `includeAllServices: true`. 

### Example 2
Register pipelines with DI and provide Pipeline dependencies using the application container.

```csharp
services.AddPipeline( includeAllServices: true );
```

### Example 3
Register Pipelines with DI and provide Pipeline dependencies using a specialized container.

```csharp
services.AddPipeline( (factoryServices, rootProvider) =>
{
    factoryServices.AddTransient<IThing>()
    factoryServices.ProxyService<IPrincipalProvider>( rootProvider ); // pull from root container
} );
```

### Example 4
Register Pipeline services manually and provide Pipeline dependencies using a specialized container.

```csharp
var serviceProvider = new ServiceCollection()
       .AddTransient<IPrincipalProvider>()
       .BuildServiceProvider();
   
PipelineContextFactory.CreateFactory( serviceProvider );

// get the `PipelineContextFactory` without DI
var factory = PipelineContextFactory.Instance;
```

Once you have registered pipeline services, custom middleware can get service instances at runtime
by accessing the `context.ServiceProvider` property.

```csharp
context.ServiceProvider.GetRequiredService<IPrincipleProvider>()
```

## Middleware Provider

Register an `IPipelineMiddlewareProvider` to inject cross-cutting hooks and wraps into pipelines
via DI. Commands that accept the provider can use `PipelineFactory.Create` to apply the middleware
automatically. See [Middleware](middleware.md) for details.

```csharp
services.AddSingleton<IPipelineMiddlewareProvider, MyMiddlewareProvider>();
```

## Result Mapper

Register an `IResultMapper` to apply shared exception-to-HTTP-status mapping across endpoints.
See the [AspNetCore README](../src/Hyperbee.Pipeline.AspNetCore/README.md) for details.

```csharp
services.AddSingleton<IResultMapper, ConflictResultMapper>();
```
