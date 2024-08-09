---
layout: default
title: Syntax
nav_order: 2
---

# Pipeline Syntax

## Statements
             
| Method     | Description 
| ---------- | ----------- 
| Call       | Execute a `void` statement that does not transform the pipeline output.
| CallAsync  | Asynchronously execute a `void` statement that does not transform the pipeline output.
| Pipe       | Execute a statement that transforms the pipeline output.
| PipeAsync  | Asynchronously execute a statement that transforms the pipeline output.

## Flow Control

| Method     | Description 
| ---------- | ----------- 
| Pipe       | Pipes a child pipeline with optional middlewares.
| PipeIf     | Conditionally pipes a child pipeline with optional middlewares.
| Call       | Calls a child pipeline with optional middlewares.
| CallIf     | Conditionally calls a child pipeline with optional middlewares.
| ForEach    | Enumerates a collection pipeline input.
| Reduce     | Transforms an enumerable pipeline input.
| WaitAll    | Waits for concurrent pipelines to complete.

## Cancellation

| Method     | Description 
| ---------- | ----------- 
| Cancel     | Cancels the pipeline after the current step.
| CancelWith | Cancels the pipeline with a value, after the current step.

## Reference

### Statements

- `Call` - Execute a statement that does not transform the pipeline output.
- `CallAsync` - Asynchronously execute a statement that does not transform the pipeline output.
- `Pipe` - Execute a statement that transforms the pipeline output.
- `PipeAsync` - Asynchronously execute a statement that transforms the pipeline output.

In this example notice that `arg + 9` is not returned from the use of `Call`.

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

### If Conditions

`PipeIf` and `PipeIfAsync` allow you to conditionally add a step to the pipeline. You can specify a condition function that determines whether 
the step should be added, a builder function that creates the step, and an optional flag indicating whether middleware should be inherited.### CallIf

`CallIf` and `CallIfAsync` allow you to conditionally call a child pipeline with optional middlewares. You can specify a condition function that determines whether 
the child pipeline should be called, a builder function that creates the child pipeline, and an optional flag indicating whether middleware should be inherited.### ForEach

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

### ForEach

`ForEach` and `ForEachAsync` allow you to enumerate a collection pipeline input and apply a pipeline to each element. 

```csharp
var count = 0;

var command = PipelineFactory
    .Start<string>()
    .Pipe( ( ctx, arg ) => arg.Split( ' ' ) )
    .ForEach().Type<string>( builder => builder
        .Pipe( ( ctx, arg ) => count += 10 )
    )
    .Pipe( ( ctx, arg ) => count += 5 )
    .Build();

await command( new PipelineContext(), "e f" );

Assert.AreEqual( count, 25 );
```

### Reduce

`Reduce` and `ReduceAync` allow you to transform an enumerable pipeline input to a single value. You can specify a reducer function
that defines how the elements should be combined, and a builder function that creates the pipeline for processing the elements.### Cancel

```csharp
var command = PipelineFactory
     .Start<string>()
     .Pipe( ( ctx, arg ) => arg.Split( ' ' ) )
     .Reduce().Type<string, int>( ( aggregate, value ) => aggregate + value, builder => builder
         .Pipe( ( ctx, arg ) => int.Parse( arg ) + 10 )
     )
     .Pipe( ( ctx, arg ) => arg + 5 )
     .Build();

var result = await command( new PipelineContext(), "1 2 3 4 5" );

Assert.AreEqual( result, 70 );
```

### WaitAll

`WaitAll` allows you to wait for concurrent pipelines to complete before continuing. You can specify a set of builders that create
the pipelines to wait for, and a reducer function that combines the results of the pipelines.

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

### Cancellation

`Cancel` method allows you to cancel the pipeline after the current step.

`CancelWith` method allows you to cancel the pipeline with a value after the current step.## Parallel Flow
