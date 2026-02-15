# Hyperbee.Pipeline.TestHelpers

The `Hyperbee.Pipeline.TestHelpers` library provides test fixtures and helpers for unit testing
`Hyperbee.Pipeline` commands with FluentValidation and NSubstitute.

## Examples

### Create a Test Pipeline Context

```csharp
// Default context with no validators
var factory = PipelineContextFactoryFixture.Default();

// Context with validators
var factory = new PipelineContextFactoryFixture()
    .WithValidator<OrderInput, OrderInputValidator>()
    .Create();
```

### Test a Command

```csharp
[Fact]
public async Task Should_return_result_when_input_is_valid()
{
    var factory = new PipelineContextFactoryFixture()
        .WithValidator<OrderInput, OrderInputValidator>()
        .Create();

    var logger = Substitute.For<ILogger<CreateOrderCommand>>();
    var command = new CreateOrderCommand( factory, logger );

    var (ctx, result) = await command.ExecuteAsync( new OrderInput { Name = "Widget" } );

    Assert.Null( ctx.Exception );
    Assert.NotNull( result );
}
```

### Register Custom Services

```csharp
var factory = new PipelineContextFactoryFixture()
    .WithValidator<OrderInput, OrderInputValidator>()
    .WithServices( services =>
    {
        services.AddSingleton( mockRepository );
        services.AddSingleton<IDateTimeProvider>( mockDateTimeProvider );
    } )
    .Create();
```

### Mock Command Functions

```csharp
var command = Substitute.For<ICommandFunction<Guid, Order>>();

// Mock a successful result
command.MockSuccessfulResult( new Order { Id = Guid.NewGuid(), Name = "Widget" } );

// Mock a validation failure
command.MockValidationFailureResult<Guid, Order>(
    new ValidationFailure( "Name", "Name is required." ) );

// Mock an exception
command.MockExceptionCommandResult<Guid, Order>(
    new InvalidOperationException( "Something went wrong." ) );
```

### Create Command Results Directly

```csharp
var success = CommandResultHelpers.CreateSuccess( new Order { Name = "Widget" } );

var failure = CommandResultHelpers.CreateValidationFailure<Order>(
    new ValidationFailure( "Name", "Name is required." ) );

var exception = CommandResultHelpers.CreateWithException<Order>(
    new InvalidOperationException( "Something went wrong." ) );
```
