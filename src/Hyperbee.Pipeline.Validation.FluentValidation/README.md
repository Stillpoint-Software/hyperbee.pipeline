# Hyperbee.Pipeline.FluentValidation

The `Hyperbee.Pipeline.FluentValidation` library integrates [FluentValidation](https://github.com/FluentValidation/FluentValidation) with `Hyperbee.Pipeline`. This is the recommended package for most users who want to add validation to their pipelines.

## Features

- **Seamless FluentValidation Integration**: Use your existing FluentValidation validators
- **Automatic Registration**: Scan assemblies and register validators automatically
- **RuleSet Support**: Execute specific RuleSets based on pipeline context
- **Full Pipeline Integration**: `ValidateAsync`, `IfValidAsync`, and other pipeline extensions
- **Zero Configuration**: Works out of the box with standard FluentValidation patterns

## Installation

```bash
dotnet add package Hyperbee.Pipeline.FluentValidation
```

This package transitively includes:
- `Hyperbee.Pipeline.Validation` (base implementations and extensions)
- `Hyperbee.Pipeline.Validation.Abstractions` (interfaces)
- `FluentValidation` (validation framework)

## Quick Start

### 1. Create FluentValidation Validators

```csharp
using FluentValidation;

public class CreateOrderValidator : AbstractValidator<CreateOrderInput>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Items).NotEmpty();
    }
}
```

### 2. Register FluentValidation

```csharp
using Hyperbee.Pipeline.Validation;

// Recommended: Use helper method
services.AddFluentValidation(options =>
    options.ScanAssembly(typeof(CreateOrderValidator).Assembly));

// Alternative: Manual registration
services.AddSingleton<IValidatorProvider, FluentValidatorProvider>();
services.AddValidatorsFromAssemblyContaining<CreateOrderValidator>();
```

### 3. Use in Pipelines

```csharp
var pipeline = PipelineBuilder
    .Create<CreateOrderInput, Order>()
    .ValidateAsync()  // Validates using CreateOrderValidator
    .Pipe((context, input) => CreateOrder(input))
    .Build();
```

## Examples

### Basic Validation

```csharp
var command = PipelineFactory
    .Start<Order>()
    .ValidateAsync()
    .Pipe((ctx, order) => ProcessOrder(order))
    .Build();
```

### Conditional Execution on Validation

```csharp
var command = PipelineFactory
    .Start<Order>()
    .ValidateAsync()
    .IfValidAsync(builder => builder
        .PipeAsync(async (ctx, order) => await SaveOrderAsync(order))
    )
    .Build();
```

### RuleSet Validation

```csharp
public class ProductCatalogValidator : AbstractValidator<ProductCatalog>
{
    public ProductCatalogValidator()
    {
        // Always executed rules
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);

        // Create scenario - ID not required
        RuleSet("Create", () =>
        {
            RuleFor(x => x.Quantity).LessThanOrEqualTo(1000);
        });

        // Update scenario - ID required
        RuleSet("Update", () =>
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.VersionTag).NotEmpty();
        });
    }
}

// Static RuleSet
var createPipeline = PipelineBuilder
    .Start<ProductCatalog>()
    .ValidateAsync("Create")  // Uses Create RuleSet + default rules
    .Pipe((ctx, catalog) => CreateItem(catalog));

// Dynamic RuleSet
var pipeline = PipelineBuilder
    .Start<ProductCatalog>()
    .ValidateAsync((ctx, catalog) => catalog.IsNew ? "Create" : "Update")
    .Pipe((ctx, catalog) => SaveItem(catalog));
```

### Checking Validation Results

```csharp
var result = await command(context, input);

if (!context.IsValid())
{
    foreach (var error in context.ValidationFailures())
    {
        Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
    }
}
```

### Exception Handling

```csharp
var command = PipelineFactory
    .Start<string>()
    .WithExceptionHandling(config => config
        .AddException<InvalidOperationException>(errorcode: 1001)
        .AddException<TimeoutException>(errorcode: 1002)
    )
    .Pipe((ctx, arg) => $"hello {arg}")
    .Build();
```

## Migration from Hyperbee.Pipeline.Validation 1.x

If you're upgrading from version 1.x:

### Before (v1.x)

```csharp
<PackageReference Include="Hyperbee.Pipeline.Validation" />

services.AddSingleton<IValidatorProvider, ValidatorProvider>();
services.AddValidatorsFromAssemblyContaining<OrderValidator>();
```

### After (v2.x)

```csharp
<PackageReference Include="Hyperbee.Pipeline.FluentValidation" />

// Recommended
services.AddFluentValidation(options =>
    options.ScanAssembly(typeof(OrderValidator).Assembly));

// Or keep manual registration
services.AddSingleton<IValidatorProvider, FluentValidatorProvider>();
services.AddValidatorsFromAssemblyContaining<OrderValidator>();
```

**Pipeline code remains unchanged** - all `ValidateAsync()` calls work exactly the same.

## Advanced Scenarios

### Using Multiple Validation Frameworks

You can use FluentValidation alongside custom validators:

```csharp
// Some models use FluentValidation
public class OrderValidator : AbstractValidator<Order> { }

// Some models use custom validators
public class PaymentValidator : IValidator<Payment>
{
    public Task<IValidationResult> ValidateAsync(Payment instance, CancellationToken ct = default)
    {
        // Custom validation logic
    }
}

// FluentValidatorProvider handles both via the adapter pattern
services.AddFluentValidation(options => options.ScanAssembly(Assembly.GetExecutingAssembly()));
```

## Related Packages

- **`Hyperbee.Pipeline.Validation.Abstractions`** - Pure interfaces (for framework authors)
- **`Hyperbee.Pipeline.Validation`** - Base implementations (for custom validators)

## Dependencies

- `Hyperbee.Pipeline.Validation` - Core validation (transitively included)
- `FluentValidation` - Validation framework
