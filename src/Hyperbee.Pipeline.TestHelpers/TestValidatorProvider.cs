using Hyperbee.Pipeline.Validation;

namespace Hyperbee.Pipeline.TestHelpers;

/// <summary>
/// Test implementation of IValidatorProvider that stores validators in a dictionary.
/// Thread-safe to support parallel test execution within same test class.
/// </summary>
public class TestValidatorProvider : IValidatorProvider
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Type, object> _validators = new();

    public void Register<TModel>( IValidator<TModel> validator )
        where TModel : class
    {
        _validators[typeof( TModel )] = validator;
    }

    public IValidator<TPlugin>? For<TPlugin>()
        where TPlugin : class
    {
        if ( _validators.TryGetValue( typeof( TPlugin ), out var validator ) )
        {
            return (IValidator<TPlugin>) validator;
        }

        return default;
    }
}
