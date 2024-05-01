# Hyperbee.Pipeline

The `Hyperbee.Pipeline` library is a sophisticated tool for constructing asynchronous fluent pipelines in .NET. A pipeline, in this context, refers to a sequence of data processing elements arranged in series, where the output of one element serves as the input for the subsequent element.

A distinguishing feature of the `Hyperbee.Pipeline` library, setting it apart from other pipeline implementations, is its inherent support for **middleware** and **dependency injection**. Middleware introduces a higher degree of flexibility and control over the data flow, enabling developers to manipulate data as it traverses through the pipeline. This can be leveraged to implement a variety of functionalities such as eventing, caching, logging, and more, thereby enhancing the customizability of the code.

Furthermore, the support for dependency injection facilitates efficient management of dependencies within the pipeline. This leads to code that is more maintainable and testable, thereby improving the overall quality of the software.

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
