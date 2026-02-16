namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public interface IValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    bool IsValid { get; }

    /// <summary>
    /// Gets the collection of validation failures. Empty if validation succeeded.
    /// </summary>
    IList<IValidationFailure> Errors { get; }
}
