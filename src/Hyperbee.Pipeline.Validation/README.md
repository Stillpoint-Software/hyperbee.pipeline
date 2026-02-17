# Hyperbee.Pipeline.Validation

The `Hyperbee.Pipeline.Validation` library provides base validation implementations and pipeline extensions for `Hyperbee.Pipeline`. This package is framework-agnostic and works with any validation framework that provides an `IValidator<T>` adapter.

## Features

- **Base Implementations**: Concrete `ValidationFailure` and `ValidationResult` classes implementing the abstractions
- **Pipeline Extensions**: `ValidateAsync`, `IfValidAsync`, `ValidateAndCancelOnFailureAsync` for declarative validation
- **Context Extensions**: Methods to store and retrieve validation results from pipeline contexts
- **Domain Failure Types**: `ApplicationValidationFailure`, `NotFoundValidationFailure`, `UnauthorizedValidationFailure`, `ForbiddenValidationFailure`
- **Exception Handling**: Middleware to map exceptions to validation failures

## When to Use This Package

- **Custom Validator Implementation**: Building validators without depending on a specific framework
- **Framework-Agnostic Libraries**: Creating reusable components that support validation
- **Multiple Validation Frameworks**: Using different validators for different scenarios

## Basic Usage

### Using a Custom Validator

```csharp
public class EmailValidator : IValidator<EmailAddress>
{
    public async Task<IValidationResult> ValidateAsync(
        EmailAddress instance,
        CancellationToken cancellationToken = default)
    {
        var failures = new List<IValidationFailure>();

        if (string.IsNullOrEmpty(instance.Address))
        {
            failures.Add(new ValidationFailure(
                nameof(instance.Address),
                "Email address is required"));
        }

        return new ValidationResult(failures);
    }

    public Task<IValidationResult> ValidateAsync(
        EmailAddress instance,
        Action<IValidationContext> configure,
        CancellationToken cancellationToken = default)
    {
        return ValidateAsync(instance, cancellationToken);
    }
}

// Register validator
services.AddSingleton<IValidator<EmailAddress>, EmailValidator>();
services.AddSingleton<IValidatorProvider, CustomValidatorProvider>();

// Use in pipeline
var pipeline = PipelineBuilder
    .Start<EmailAddress>()
    .ValidateAsync()
    .Pipe((context, email) => SendEmail(email))
    .Build();
```

### Validation in Pipeline

```csharp
var pipeline = PipelineBuilder
    .Create<CreateUserInput, User>()
    .ValidateAsync()
    .Pipe((context, input) => CreateUser(input));
```

### Conditional Execution on Validation

```csharp
var pipeline = PipelineBuilder
    .Create<Order, Order>()
    .ValidateAsync()
    .IfValidAsync(builder => builder
        .PipeAsync(async (ctx, order) => await SaveOrderAsync(order))
    )
    .Build();
```

### Checking Validation Results

```csharp
var result = await pipeline(context, input);

if (!context.IsValid())
{
    foreach (var error in context.ValidationFailures())
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
    }
}
```

### Exception Handling Middleware

```csharp
var pipeline = PipelineFactory
    .Start<string>()
    .WithExceptionHandling(config => config
        .AddException<InvalidOperationException>(errorcode: 1001)
        .AddException<TimeoutException>(errorcode: 1002)
    )
    .Pipe((ctx, arg) => $"hello {arg}")
    .Build();
```

### Domain-Specific Validation Failures

```csharp
// Not found
context.SetValidationResult(
    new NotFoundValidationFailure("UserId", "User not found"),
    ValidationAction.CancelAfter);

// Unauthorized
context.SetValidationResult(
    new UnauthorizedValidationFailure("", "Authentication required"),
    ValidationAction.CancelAfter);

// Forbidden
context.SetValidationResult(
    new ForbiddenValidationFailure("", "Insufficient permissions"),
    ValidationAction.CancelAfter);
```

## Dependency Injection

```csharp
// Register your validator provider
services.AddSingleton<IValidatorProvider, YourValidatorProvider>();

// Register pipelines
services.AddPipeline(includeAllServices: true);
```

## Related Packages

- **`Hyperbee.Pipeline.Validation.FluentValidation`** - FluentValidation adapter (most common use case)

## For Most Users

If you're using FluentValidation, reference `Hyperbee.Pipeline.Validation.FluentValidation` instead. This package is primarily for:
- Custom validator implementations
- Framework-agnostic libraries
- Advanced scenarios requiring multiple validation frameworks
