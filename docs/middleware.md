---
layout: default
title: Middleware
nav_order: 4
---

# Middleware
Pipelines support custom middleware. Custom middleware can be created by implementing an extension method that uses a `Hook` or `Wrap` builder.

## Middleware Syntax

| Method     | Description 
| ---------- | ----------- 
| Hook       | Applies middleware to each step in the pipeline.
| Wrap       | Wraps the middleware around the preceeding steps.


## Hook

`Hooks` are middleware that surround individual pipeline actions. The `Hook` and `HookAsync` methods allow you to add a hook that is called 
for every statement in the pipeline. This hook takes the current context, the current argument, and a delegate to the next part of the 
pipeline. It can manipulate the argument before and after calling the next part of the pipeline.

Here's an example of how to use `HookAsync` with an inline lambda:

```csharp
var command = PipelineFactory
    .Start<string>()
    .HookAsync( async ( ctx, arg, next ) => await next( ctx, arg + "[" ) + "]" )
    .Pipe( ( ctx, arg ) => arg + "1" )
    .Pipe( ( ctx, arg ) => arg + "2" )
    .Build();

var result = await command( new PipelineContext() );

Assert.AreEqual( "[1][2]", result );
```

### Example
Example of a `hook` middleware that surrounds each step. Hooks must be constrained to only be available at the start 
of the pipeline. This is accomplished be extending `IPipelineStartBuilder<TInput, TOutput>`. 

#### Definition:
```csharp
public static class PipelineMiddleware
{
    public static IPipelineStartBuilder<TInput, TOutput> WithLogging<TInput, TOutput>( this IPipelineStartBuilder<TInput, TOutput> builder )
    {
        return builder.HookAsync( async ( context, argument, next ) =>
        {
            Console.WriteLine( $"[{context.Id:D2}] begin with (arg = {argument})" );

            var result = await next( context, argument );

            Console.WriteLine( $"[{context.Id:D2}] end with (result = {result})" );

            return result;
        } );
    }
}
```

#### Usage:
```csharp
var command = PipelineFactory
    .Start<string>()
    .WithLogging()
    .Pipe((ctx, arg) => $"hello {arg}" )
    .Pipe((ctx, arg) => $"{arg}, again!")
    .Build();

var result = await command( new PipelineContext(), "hook" );
```

_output:_
```
[02] begin (arg = hook)
[02] end (result = hello hook)
[03] begin (arg = hello hook)
[03] end (result = hello hook, again!)
```
The `WithLogging` hooked into the beginning and end of each pipeline step with the `next` method being the individual action(s).

## Wraps

`Wraps` are middleware that surround a group of pipeline actions. The `Wrap` and `WrapAsync` method allows you to wrap a part of the 
pipeline. This is useful when you want to apply a transformation to only a part of the pipeline.

Here’s an example of how to use `WrapAsync`:

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

### Example
Example of a `wrap` middleware that surrounds a block of steps. Create `Wrap` middleware by extending `IPipelineBuilder<TInput, TOutput>`.

```csharp
public static class PipelineMiddleware
{
    public static IPipelineBuilder<TInput, TOutput> WithTransaction<TInput, TOutput>( this IPipelineBuilder<TInput, TOutput> builder )
    {
        return builder.WrapAsync( async ( context, argument, next ) =>
        {
            Console.WriteLine( $"[{context.Id:D2}] begin transaction (name = '{context.Name}')" );

            var result = await next( context, argument );

            Console.WriteLine( $"[{context.Id:D2}] end transaction (name = '{context.Name}')" );

            return result;
        }, "T" );
    }
}
```

#### Usage:
```csharp
var command = PipelineFactory
    .Start<string>()
    .Pipe((ctx, arg) => $"hello {arg}")
    .Pipe((ctx, arg) => $"{arg}, again!")
    .WithTransaction()
    .Build();

var result = await command(new PipelineContext(), "wrap");

Assert.AreEqual("hello wrap, again!", result);
```

_output:_
```
[02] begin transaction (name = 'T')
[02] end transaction (name = 'T')
```

The `WithTransaction` wrapped all the pipeline steps and was only executed at the beginning and and of the command with the
`next` method being the entire group of actions.

## Composition

Because of the way pipeline are composed it is possible for middleware to be additive or appear to override previous middleware.  

### Example
In this example of a `hook` the pipeline logs the current user.

```csharp
public static class PipelineMiddleware
{
    public static IPipelineStartBuilder<TInput, TOutput> WithUser<TInput, TOutput>( this IPipelineStartBuilder<TInput, TOutput> builder, string user )
    {
        return builder.HookAsync( async ( context, argument, next ) =>
        {
            Console.WriteLine( $"[{context.Id:D2}] begin (user = '{user}') (arg = {argument})" );

            var result = await next( context, argument );

            Console.WriteLine( $"[{context.Id:D2}] end (result = {result})" );

            return result;
        } );
    }
}
```


#### Usage:

```csharp
var command = PipelineFactory
    .Start<int>()
    .WithUser("bob")
    .WithUser("jim")
    .Pipe((ctx, arg) => $"input: {++arg}")
    .Build();

var result = await command(new PipelineContext(), 1);
```

_output:_

```
[02] begin (user = 'jim') (arg = 1)
[02] begin (user = 'bob') (arg = 1)
[02] end (result = input: 1)
[02] end (result = input: 1)
```

:warning: In this case both hooks are executed from the inside out (which appears as reverse order) before the main pipe step.

#### Usage with child pipeline:

```csharp
var command = PipelineFactory
    .Start<int>()
    .WithUser("bob")
    .Pipe((ctx, arg) => ++arg)
    .PipeIf((ctx, arg) => arg == 2, 
        inheritMiddleware: false, 
        builder => builder
            .WithUser("jim")
            .Pipe((ctx, arg) => $"hello {++arg}")
    )
    .Pipe((ctx, arg) => $"{arg}, again!")
    .Build();

var result = await command(new PipelineContext(), 1);
```

_output:_

```
[02] begin (user = 'bob') (arg = 1)
[02] end (result = 2)
[03] begin (user = 'jim') (arg = 2)
[03] end (result = hello 3)
[04] begin (user = 'bob') (arg = hello 3)
[04] end (result = hello 3, again!)
```

:warning: In this case because the child pipeline does not inherit the parent middleware only the one "jim" hook wraps the `$"hello {++arg}"` step.