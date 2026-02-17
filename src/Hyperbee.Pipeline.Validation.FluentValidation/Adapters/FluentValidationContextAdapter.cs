using Hyperbee.Pipeline.Validation;
using FV = FluentValidation;

namespace Hyperbee.Pipeline.Validation.FluentValidation;

/// <summary>
/// Adapts <see cref="IValidationContext"/> to FluentValidation's validation options.
/// </summary>
internal class FluentValidationContextAdapter<T> : IValidationContext where T : class
{
    private readonly List<string> _ruleSets = new();

    public FluentValidationContextAdapter( T instance )
    {
        Instance = instance;
    }

    public T Instance { get; }

    public void IncludeRuleSets( params string[] ruleSets )
    {
        _ruleSets.AddRange( ruleSets );
    }

    internal FV.ValidationContext<T> CreateContext()
    {
        if ( _ruleSets.Count > 0 )
        {
            var selector = new FV.Internal.RulesetValidatorSelector( _ruleSets.ToArray() );
            return new FV.ValidationContext<T>( Instance, null, selector );
        }

        return new FV.ValidationContext<T>( Instance );
    }
}
