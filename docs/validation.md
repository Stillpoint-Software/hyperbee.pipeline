---
layout: default
title: Validation
nav_order: 4
---

# Pipeline Validation

Hyperbee.Pipeline provides a flexible validation system with a pluggable architecture. The validation functionality is split into two packages:

- **Hyperbee.Pipeline.Validation** - Interfaces, base implementations, and pipeline extensions
- **Hyperbee.Pipeline.Validation.FluentValidation** - FluentValidation adapter (recommended)

## Quick Start

### Installation

For most users, install the FluentValidation adapter package:

```bash
dotnet add package Hyperbee.Pipeline.Validation.FluentValidation
```

This automatically includes the core validation package as a dependency.

### Basic Usage

```csharp
using Hyperbee.Pipeline;
using Hyperbee.Pipeline.Validation.FluentValidation;
using FluentValidation;

// 1. Define your model
public class Order
{
    public string ProductName { get; set; }
    public decimal Amount { get; set; }
}

// 2. Create a FluentValidation validator
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

// 3. Register validators in DI
services.AddFluentValidation(options =>
    options.ScanAssembly(typeof(OrderValidator).Assembly));

// 4. Use ValidateAsync in your pipeline
var command = PipelineFactory
    .Start<Order>()
    .ValidateAsync() // Automatically resolves and runs OrderValidator
    .PipeAsync(async (ctx, order) => await ProcessOrder(order))
    .Build();

var result = await command(context, new Order { ProductName = "Widget", Amount = 10 });
```

## Validation Methods

### Builder Extensions (Declarative)

| Method | Description |
| ------ | ----------- |
| `builder.ValidateAsync()` | Validates the current value at this point in the pipeline |
| `builder.IfValidAsync(...)` | Conditionally executes a sub-pipeline only if validation passes |
| `builder.ValidateAndCancelOnFailureAsync()` | Validates and cancels the pipeline on failure |

### Context Extensions (Imperative)

| Method | Description |
| ------ | ----------- |
| `ctx.ValidateAsync(argument)` | Validates a value imperatively from within a pipe step |
| `ctx.IsValid()` | Returns true if no validation failures have been recorded |
| `ctx.ValidationFailures()` | Returns all recorded validation failures |
| `ctx.FailAfter(message)` | Records a failure and cancels the pipeline |

## builder.ValidateAsync

`builder.ValidateAsync()` validates the current pipeline value at the point it is called. It has three overloads:

### Basic Validation

```csharp
var command = PipelineFactory
    .Start<Order>()
    .ValidateAsync() // Automatically cancels pipeline if validation fails
    .PipeAsync(async (ctx, order) => await ProcessOrder(order)) // Only runs if valid
    .Build();
```

### Validation with RuleSets

```csharp
public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.ProductName).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);

        RuleSet("Premium", () => {
            RuleFor(x => x.Amount).GreaterThan(100);
        });
    }
}

// Static RuleSet
var command = PipelineFactory
    .Start<Order>()
    .ValidateAsync("Premium") // Runs "Premium" RuleSet + default rules
    .Build();

// Dynamic RuleSet based on context
var command2 = PipelineFactory
    .Start<Order>()
    .ValidateAsync((ctx, order) =>
        order.Amount > 1000 ? "Premium" : null) // Conditionally apply RuleSet
    .Build();
```

## ctx.ValidateAsync

`ctx.ValidateAsync()` is the **imperative** counterpart to `builder.ValidateAsync()`. It lets you trigger validation from within a pipe step — useful when the value to validate is produced mid-step, when validation is conditional, or when you need to act on the result immediately.

Returns `true` if validation passes, `false` if it fails. On failure, the context is automatically cancelled.

### Basic Imperative Validation

```csharp
var command = PipelineFactory
    .Start<string>()
    .PipeAsync(async (ctx, id) =>
    {
        var order = await _repository.GetOrderAsync(id);

        if (!await ctx.ValidateAsync(order))
            return null; // Pipeline already cancelled

        return order;
    })
    .Build();
```

### Imperative Validation with RuleSets

```csharp
var command = PipelineFactory
    .Start<Order>()
    .PipeAsync(async (ctx, order) =>
    {
        // Static RuleSet
        if (!await ctx.ValidateAsync(order, "Premium"))
            return order;

        return await ApplyPremiumProcessing(order);
    })
    .Build();
```

### Imperative Validation with Dynamic RuleSet

```csharp
var command = PipelineFactory
    .Start<Order>()
    .PipeAsync(async (ctx, order) =>
    {
        await ctx.ValidateAsync(order,
            (ctx, o) => o.Amount > 1000 ? "Premium" : null);

        return order;
    })
    .Build();
```

### When to Use Each Style

| Style | Use when |
| ----- | -------- |
| `builder.ValidateAsync()` | Validating the pipeline value at a fixed point; fail-fast before downstream steps |
| `ctx.ValidateAsync()` | Validating a value produced mid-step; conditional validation; need the `bool` result inline |

## IfValidAsync

Conditionally execute a pipeline only if validation passes:

```csharp
var command = PipelineFactory
    .Start<Order>()
    .ValidateAsync()
    .IfValidAsync(builder => builder
        .PipeAsync(async (ctx, order) => await ProcessOrder(order))
    )
    .PipeAsync(async (ctx, order) => {
        // Handle both valid and invalid cases
        return ctx.IsValid()
            ? order
            : HandleValidationFailure(ctx.ValidationFailures());
    })
    .Build();
```

## Context Extensions

### Checking Validation State

```csharp
.PipeAsync(async (ctx, order) => {
    if (ctx.IsValid())
    {
        // Validation passed
    }

    var failures = ctx.ValidationFailures();
    foreach (var failure in failures)
    {
        Console.WriteLine($"{failure.PropertyName}: {failure.ErrorMessage}");
    }
})
```

### Manual Validation Failures

```csharp
using Hyperbee.Pipeline.Validation;

.PipeAsync(async (ctx, order) => {
    // Manual validation
    if (order.Amount > 10000)
    {
        ctx.FailAfter("Exceeds maximum allowed"); // propertyName auto-filled by CallerMemberName
    }

    return order;
})
```

### Custom Validation Result Types

The validation system includes specialized failure types for HTTP scenarios:

```csharp
using Hyperbee.Pipeline.Validation;

.PipeAsync(async (ctx, order) => {
    var existingOrder = await _repository.GetOrderAsync(order.Id);

    if (existingOrder == null)
    {
        ctx.FailAfter(new NotFoundValidationFailure("Order", $"Order {order.Id} not found"));
        return null;
    }

    if (!ctx.User.CanAccessOrder(existingOrder))
    {
        ctx.FailAfter(new UnauthorizedValidationFailure("Order", "Access denied"));
        return null;
    }

    return existingOrder;
})
```

Available failure types:
- `ValidationFailure` - Base validation failure
- `ApplicationValidationFailure` - Application-level failure
- `NotFoundValidationFailure` - Sets ErrorCode to "NotFound" (404)
- `UnauthorizedValidationFailure` - Sets ErrorCode to "Unauthorized" (401)
- `ForbiddenValidationFailure` - Sets ErrorCode to "Forbidden" (403)

## ASP.NET Core Integration

When using `Hyperbee.Pipeline.AspNetCore`, validation failures are automatically mapped to HTTP responses:

```csharp
using Hyperbee.Pipeline.AspNetCore;

app.MapPost("/orders", async (Order order, ICommandFunction<Order, OrderResult> command) =>
{
    var result = await command.ExecuteAsync(order, cancellationToken);

    // Automatically returns:
    // - 400 Bad Request for validation failures
    // - 404 Not Found for NotFoundValidationFailure
    // - 401 Unauthorized for UnauthorizedValidationFailure
    // - 403 Forbidden for ForbiddenValidationFailure
    return result.ToHttpResult();
});
```

## Advanced Scenarios

### Custom Validators

You can create custom validators by implementing the `IValidator<T>` interface:

```csharp
using Hyperbee.Pipeline.Validation;

public class CustomOrderValidator : IValidator<Order>
{
    public Task<IValidationResult> ValidateAsync(
        Order instance,
        CancellationToken cancellationToken = default)
    {
        var failures = new List<IValidationFailure>();

        if (string.IsNullOrEmpty(instance.ProductName))
        {
            failures.Add(new ValidationFailure("ProductName", "Product name is required"));
        }

        if (instance.Amount <= 0)
        {
            failures.Add(new ValidationFailure("Amount", "Amount must be greater than zero"));
        }

        return Task.FromResult<IValidationResult>(new ValidationResult(failures));
    }

    public Task<IValidationResult> ValidateAsync(
        Order instance,
        Action<IValidationContext> configure,
        CancellationToken cancellationToken = default)
    {
        // For custom validators, configuration can be ignored or implemented as needed
        return ValidateAsync(instance, cancellationToken);
    }
}
```

### Custom Validator Provider

To use custom validators, implement `IValidatorProvider`:

```csharp
using Hyperbee.Pipeline.Validation;

public class CustomValidatorProvider : IValidatorProvider
{
    private readonly Dictionary<Type, object> _validators = new();

    public CustomValidatorProvider()
    {
        _validators[typeof(Order)] = new CustomOrderValidator();
    }

    public IValidator<T>? For<T>() where T : class
    {
        return _validators.TryGetValue(typeof(T), out var validator)
            ? (IValidator<T>)validator
            : null;
    }
}

// Register in DI
services.AddSingleton<IValidatorProvider, CustomValidatorProvider>();
```

### Mixing Validators

You can mix FluentValidation and custom validators:

```csharp
// Some models use FluentValidation
public class OrderValidator : AbstractValidator<Order> { }

// Other models use custom validators
public class PaymentValidator : IValidator<Payment> { }

// In Startup.cs
services.AddFluentValidation(options =>
    options.ScanAssembly(Assembly.GetExecutingAssembly()));

// FluentValidatorProvider will only return validators for types with FluentValidation validators
// You can chain or create a composite provider for mixed scenarios
```

## Exception Handling

Validation integrates with pipeline exception handling:

```csharp
var command = PipelineFactory
    .Start<Order>()
    .ValidateAsync()
    .PipeAsync(async (ctx, order) => await ProcessOrder(order))
    .HandleExceptionsAsync(async (ctx, exception) => {
        // Log exception
        ctx.FailAfter(new ValidationFailure("System", exception.Message));
    })
    .Build();
```

## Testing

The `Hyperbee.Pipeline.TestHelpers` package provides utilities for testing validated pipelines:

```csharp
using Hyperbee.Pipeline.TestHelpers;
using FluentValidation;

[TestMethod]
public async Task Should_Validate_Order()
{
    // Create a test context with validators
    var factory = new PipelineContextFactoryFixture()
        .WithValidator<Order, OrderValidator>()
        .Create();

    var context = factory.Create(NullLogger.Instance);

    var command = PipelineFactory
        .Start<Order>()
        .ValidateAsync()
        .Build();

    var order = new Order { ProductName = "", Amount = -10 };
    await command(context, order);

    Assert.IsFalse(context.IsValid());
    Assert.AreEqual(2, context.ValidationFailures().Count());
}
```

## Package Architecture

### Hyperbee.Pipeline.Validation

Contains interfaces, base implementations, and pipeline extensions:

- `IValidationFailure`, `IValidationResult`, `IValidator<T>`, `IValidationContext`, `IValidatorProvider` - Core interfaces
- `ValidationFailure` - Base implementation of `IValidationFailure`
- `ValidationResult` - Base implementation of `IValidationResult`
- Domain-specific failures: `NotFoundValidationFailure`, `UnauthorizedValidationFailure`, etc.
- Pipeline extensions: `ValidateAsync`, `IfValidAsync`, etc.
- Context extensions: `IsValid`, `GetValidationResult`, `FailAfter`, etc.

**No FluentValidation dependency** - You can use this package alone for custom validation.

### Hyperbee.Pipeline.Validation.FluentValidation

Adapter that integrates FluentValidation:

- `FluentValidatorProvider` - Resolves FluentValidation validators from DI
- Adapter classes that bridge FluentValidation types to pipeline abstractions
- `AddFluentValidation()` extension method for DI registration

**This is the recommended package for most users.**

## Best Practices

1. **Validate Early** - Place `ValidateAsync()` early in your pipeline to fail fast
2. **Use Typed Failures** - Use `NotFoundValidationFailure`, etc. for HTTP scenarios
3. **Handle Failures** - Always check `ctx.IsValid()` or use `IfValidAsync`
4. **Cancel on Failure** - Use `ValidationAction.CancelAfter` when you don't need to continue
5. **Test Validation** - Use `PipelineContextFactoryFixture` to test validation logic
6. **Separate Concerns** - Keep validation rules in validators, not in pipeline steps

---

For more information, see:
- [ASP.NET Core Integration](aspnetcore.md)
- [Middleware Documentation](middleware.md)
- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
