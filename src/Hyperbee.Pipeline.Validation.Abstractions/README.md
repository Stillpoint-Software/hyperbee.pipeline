# Hyperbee.Pipeline.Validation.Abstractions

The `Hyperbee.Pipeline.Validation.Abstractions` library provides the core validation interfaces for `Hyperbee.Pipeline`. This package contains only interfaces and no implementation dependencies, making it ideal for framework authors and library creators who want to support Hyperbee.Pipeline validation without taking a dependency on a specific validation framework.

## When to Use This Package

- **Framework Authors**: Building a custom validation adapter for a validation framework
- **Library Developers**: Creating reusable components that support validation without coupling to a specific validator
- **Plugin Systems**: Defining validation contracts that plugin implementations must satisfy

## Interfaces

### Core Validation

- **`IValidator<T>`** - Defines a validator for a specific type
- **`IValidationResult`** - Represents the result of a validation operation
- **`IValidationFailure`** - Represents a validation failure for a specific property
- **`IValidationContext`** - Provides configuration options (e.g., RuleSets) for validation
- **`IValidatorProvider`** - Provides access to validators from a registration mechanism (e.g., DI container)

## Example: Custom Validator Implementation

```csharp
public class EmailValidator : IValidator<EmailAddress>
{
    public Task<IValidationResult> ValidateAsync(
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
        else if (!instance.Address.Contains('@'))
        {
            failures.Add(new ValidationFailure(
                nameof(instance.Address),
                "Email address must contain @"));
        }

        return Task.FromResult<IValidationResult>(new ValidationResult(failures));
    }

    public Task<IValidationResult> ValidateAsync(
        EmailAddress instance,
        Action<IValidationContext> configure,
        CancellationToken cancellationToken = default)
    {
        // For this simple validator, we ignore RuleSets
        return ValidateAsync(instance, cancellationToken);
    }
}
```

## Related Packages

- **`Hyperbee.Pipeline.Validation`** - Base implementations and pipeline extensions (framework-agnostic)
- **`Hyperbee.Pipeline.FluentValidation`** - FluentValidation adapter for the most common validation scenarios

## Dependencies

- `Hyperbee.Pipeline` - Core pipeline library (no other dependencies)
