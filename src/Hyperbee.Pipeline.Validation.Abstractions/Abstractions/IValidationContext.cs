namespace Hyperbee.Pipeline.Validation;

/// <summary>
/// Provides configuration options for validation operations.
/// </summary>
public interface IValidationContext
{
    /// <summary>
    /// Specifies which RuleSets to include during validation.
    /// </summary>
    /// <param name="ruleSets">The names of the RuleSets to include. Multiple RuleSets can be specified.</param>
    /// <remarks>
    /// RuleSets allow grouping validation rules within a validator and executing them selectively.
    /// Use this method to specify which RuleSets should be executed during validation.
    /// </remarks>
    void IncludeRuleSets(params string[] ruleSets);
}
