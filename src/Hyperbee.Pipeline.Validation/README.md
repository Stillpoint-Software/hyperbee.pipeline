# Hyperbee.Pipeline.Validation

The `Hyperbee.Pipeline.Validation` library is a set of extensions to `Hyperbee.Pipeline` that adds support for
[FluentValidation](https://github.com/FluentValidation/FluentValidation) within the pipeline.

## Examples

### Validate Pipeline Input

```csharp
var command = PipelineFactory
    .Start<Order>()
    .ValidateAsync()
    .Pipe( ( ctx, order ) => ProcessOrder( order ) )
    .Build();
```

### Conditional Execution on Validation

```csharp
var command = PipelineFactory
    .Start<Order>()
    .ValidateAsync()
    .IfValidAsync( builder => builder
        .PipeAsync( async ( ctx, order ) => await SaveOrderAsync( order ) )
    )
    .Build();
```

### RuleSet Validation

```csharp
// Static RuleSet
var command = PipelineFactory
    .Start<Order>()
    .ValidateAsync( ruleSet: "Create" )
    .Build();

// Dynamic RuleSet selection
var command = PipelineFactory
    .Start<Order>()
    .ValidateAsync( ( ctx, order ) => order.IsUpdate ? "Update" : "Create" )
    .Build();
```

### Checking Validation Results

```csharp
var result = await command( context, input );

if ( !context.IsValid() )
{
    foreach ( var error in context.ValidationFailures() )
    {
        Console.WriteLine( $"{error.PropertyName}: {error.ErrorMessage}" );
    }
}
```

### Exception Handling Middleware

```csharp
var command = PipelineFactory
    .Start<string>()
    .WithExceptionHandling( config => config
        .AddException<InvalidOperationException>( errorcode: 1001 )
        .AddException<TimeoutException>( errorcode: 1002 )
    )
    .Pipe( ( ctx, arg ) => $"hello {arg}" )
    .Build();
```

### Validation Failure Types

The library includes specialized validation failures for common scenarios:

- `ApplicationValidationFailure` - General application errors
- `NotFoundValidationFailure` - Resource not found
- `UnauthorizedValidationFailure` - Authentication required
- `ForbiddenValidationFailure` - Insufficient permissions

## Dependency Injection

```csharp
// Register validators
services.AddSingleton<IValidatorProvider, ValidatorProvider>();

// Register pipelines
services.AddPipeline( includeAllServices: true );
```
