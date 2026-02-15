namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Defines the action to take after setting a validation result in the pipeline context.
/// </summary>
public enum ValidationAction
{
    /// <summary>
    /// Cancels pipeline execution after setting the validation result.
    /// </summary>
    CancelAfter,

    /// <summary>
    /// Continues pipeline execution after setting the validation result.
    /// </summary>
    ContinueAfter,
}
