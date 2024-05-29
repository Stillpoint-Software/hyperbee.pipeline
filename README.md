# Hyperbee.Pipeline

The `Hyperbee.Pipeline` library is a sophisticated tool for constructing asynchronous fluent pipelines in .NET. A pipeline, in this context, refers to a sequence of data processing elements arranged in series, where the output of one element serves as the input for the subsequent element.

A distinguishing feature of the `Hyperbee.Pipeline` library, setting it apart from other pipeline implementations, is its inherent support for **middleware** and **dependency injection**. Middleware introduces a higher degree of flexibility and control over the data flow, enabling developers to manipulate data as it traverses through the pipeline. This can be leveraged to implement a variety of functionalities such as eventing, caching, logging, and more, thereby enhancing the customizability of the code.

Furthermore, the support for dependency injection facilitates efficient management of dependencies within the pipeline. This leads to code that is more maintainable and testable, thereby improving the overall quality of the software.


## Features
* Middleware
    * Pipelines come with the ability to enhance processing with custom middleware.
* Hook
    * the `Hook` and `HookAsync` method allows you to add a hook that is called for every statement in the pipeline.
* Wrap
    * The `Wrap` and `WrapAsync` method allows you to wrap a part of the pipeline.
* Dependency Injection
    * Sometimes Pipelines and Pipeline middleware need access to specific container services.
* **Advanced features**
    * The `PipelineFactory` library provides a variety of helper methods that allow you to customize the behavior of your pipelines.
    * Reduce
        * The `Reduce` and `ReduceAsync` methods allow you to reduce a sequence of elements to a single value. 
    * WaitAll
        * The `WaitAll` method allows you to wait for all pipelines to complete before continuing. 
    * PipeIf
        * The `PipeIf` method allows you to conditionally add a step to the pipeline.
    * ForEach and ForEachAsync
        *The `ForEach` and `ForEachAsync` methods allow you to apply a pipeline to each element in a sequence. 
    * Call and CallAsync
        * The `Call` and `CallAsync` methods allow you to add a procedure to the pipeline. 
    * Chaining Child Pipelines
        * The `PipelineFactory` library allows you to chain pipelines together. 


## Example

```csharp
// Takes a string and returns a number
var question = PipelineFactory
    .Start<string>()
    .PipeIf((ctx, arg) => arg == "Adams", builder => builder
        .Pipe((ctx, arg) => 42)
        .Cancel()
    )
    .Pipe((ctx, arg) => 0)
    .Build();

var answer1 = await question(new PipelineContext(), "Adams");
Assert.AreEqual(42, answer1);

var answer2 = await question(new PipelineContext(), "Smith");
Assert.AreEqual(0, answer2);
```


## Example Hook

The `Hook` and `HookAsync` methods allow you to add a hook that is called for every statement in the pipeline. This hook takes the current context, the current argument, and a delegate to the next part of the pipeline. It can manipulate the argument before and after calling the next part of the pipeline.

Here's an example of how to use `HookAsync`:

```csharp
var command = PipelineFactory
    .Start<string>()
    .HookAsync( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" )
    .Pipe( ( ctx, arg ) => arg + "1" )
    .Pipe( ( ctx, arg ) => arg + "2" )
    .Build();

var result = await command( new PipelineContext() );

Assert.AreEqual( "{1}{2}", result );
```

## Example Wrap

```csharp
var command = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => arg + "1" )
    .Pipe( ( ctx, arg ) => arg + "2" )
    .WrapAsync( async ( ctx, arg, next ) => await next( ctx, arg + "{" ) + "}" )
    .Pipe( ( ctx, arg ) => arg + "3" )
    .Build();

var result = await command( new PipelineContext() );

Assert.AreEqual( "{12}3", result );

```

## Example ForEach

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

## Example Call

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

## Example Chaining Child Pipelines

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


# Build Requirements

* To build and run this project, **.NET 8 SDK** is required.
* Ensure your development tools are compatible with .NET 8.

## Building the Project

* With .NET 8 SDK installed, you can build the project using the standard `dotnet build` command.

## Running Tests

* Run tests using the `dotnet test` command as usual.

# Status

| Branch     | Action                                                                                                                                                                                                                      |
|------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `develop`  | [![Build status](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/actions/workflows/publish.yml/badge.svg?branch=develop)](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/actions/workflows/publish.yml)  |
| `main`     | [![Build status](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/actions/workflows/publish.yml/badge.svg)](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/actions/workflows/publish.yml)                 |


# Help
 See our list of items [Todo](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/blob/main/docs/todo.md)

 [![Hyperbee.Extensions.DependencyInjection](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/blob/main/assets/hyperbee.svg?raw=true)](https://github.com/Stillpoint-Software/Hyperbee.Pipeline)
