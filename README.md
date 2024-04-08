# Hyperbee.Pipeline

Classes for building composable async pipelines supporting:

  * Value projections
  * Early returns
  * Child pipelines
  * [Middleware](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/blob/main/docs/middleware.md)
  * [Conditional flow](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/blob/main/docs/execution.md)
  * [Dependency Injection](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/blob/main/docs/dependencyInjection.md)

## Usage

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

# Status

| Branch     | Action                                                                                                                                                                                                                      |
|------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `develop`  | [![Build status](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/actions/workflows/publish.yml/badge.svg?branch=develop)](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/actions/workflows/publish.yml)  |
| `main`     | [![Build status](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/actions/workflows/publish.yml/badge.svg)](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/actions/workflows/publish.yml)                 |


[![Hyperbee.Pipeline](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/blob/main/assets/hyperbee.jpg?raw=true)](https://github.com/Stillpoint-Software/Hyperbee.Pipeline)

# Help
 See [Todo](https://github.com/Stillpoint-Software/Hyperbee.Pipeline/blob/main/docs/todo.md)