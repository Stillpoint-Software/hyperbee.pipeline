using System.ComponentModel.Design;
using FluentValidation;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Validation.FluentValidation.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FV = FluentValidation;

namespace Hyperbee.Pipeline.Validation.FluentValidation.Tests;

[TestClass]
public class ContextValidationTests
{
    private static IPipelineContext CreateContextWithValidator<TModel, TValidator>()
        where TModel : class
        where TValidator : FV.IValidator<TModel>, new()
    {
        var container = new ServiceContainer();
        var provider = new TestValidatorProvider();
        provider.Register<TModel>( new TValidator() );
        container.AddService( typeof( IValidatorProvider ), provider );
        var factory = PipelineContextFactory.CreateFactory( container, resetFactory: true );
        return factory.Create( Substitute.For<ILogger>() );
    }

    private static IPipelineContext CreateContextWithNoProvider()
    {
        var container = new ServiceContainer();
        var factory = PipelineContextFactory.CreateFactory( container, resetFactory: true );
        return factory.Create( Substitute.For<ILogger>() );
    }

    private static IPipelineContext CreateContextWithEmptyProvider()
    {
        var container = new ServiceContainer();
        var provider = new TestValidatorProvider();
        container.AddService( typeof( IValidatorProvider ), provider );
        var factory = PipelineContextFactory.CreateFactory( container, resetFactory: true );
        return factory.Create( Substitute.For<ILogger>() );
    }

    [TestMethod]
    public async Task Context_ValidateAsync_should_return_true_for_valid_argument()
    {
        var context = CreateContextWithValidator<TestOutput, TestOutputValidator>();

        var input = new TestOutput { Name = "Widget", ProcessedAge = 5 };
        var result = await context.ValidateAsync( input );

        Assert.IsTrue( result );
        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public async Task Context_ValidateAsync_should_return_false_for_invalid_argument()
    {
        var context = CreateContextWithValidator<TestOutput, TestOutputValidator>();

        var input = new TestOutput { Name = "", ProcessedAge = 0 };
        var result = await context.ValidateAsync( input );

        Assert.IsFalse( result );
        Assert.IsFalse( context.IsValid() );
    }

    [TestMethod]
    public async Task Context_ValidateAsync_should_cancel_context_on_failure()
    {
        var context = CreateContextWithValidator<TestOutput, AlwaysFailValidator>();

        var input = new TestOutput { Name = "Widget", ProcessedAge = 5 };
        await context.ValidateAsync( input );

        Assert.IsFalse( context.IsValid() );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task Context_ValidateAsync_should_return_true_for_null_argument()
    {
        var context = CreateContextWithValidator<TestOutput, TestOutputValidator>();

        var result = await context.ValidateAsync<TestOutput>( null! );

        Assert.IsTrue( result );
        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public async Task Context_ValidateAsync_should_fail_when_no_provider_registered()
    {
        var context = CreateContextWithNoProvider();

        var input = new TestOutput { Name = "Widget", ProcessedAge = 5 };
        var result = await context.ValidateAsync( input );

        Assert.IsFalse( result );
        Assert.IsFalse( context.IsValid() );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task Context_ValidateAsync_should_fail_when_no_validator_registered_for_type()
    {
        var context = CreateContextWithEmptyProvider();

        var input = new TestOutput { Name = "Widget", ProcessedAge = 5 };
        var result = await context.ValidateAsync( input );

        Assert.IsFalse( result );
        Assert.IsFalse( context.IsValid() );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task Context_ValidateAsync_with_ruleset_should_pass_valid_input()
    {
        var context = CreateContextWithValidator<RuleSetModel, RuleSetModelValidator>();

        var input = new RuleSetModel { Name = "Plan", Value = 500 };
        var result = await context.ValidateAsync( input, "Create" );

        Assert.IsTrue( result );
        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public async Task Context_ValidateAsync_with_ruleset_should_fail_invalid_input()
    {
        var context = CreateContextWithValidator<RuleSetModel, RuleSetModelValidator>();

        var input = new RuleSetModel { Name = "Plan", Value = 2000 };
        var result = await context.ValidateAsync( input, "Create" );

        Assert.IsFalse( result );
        Assert.IsFalse( context.IsValid() );
    }

    [TestMethod]
    public async Task Context_ValidateAsync_with_ruleset_excluding_defaults_should_ignore_default_rules()
    {
        var context = CreateContextWithValidator<RuleSetModel, RuleSetModelValidator>();

        // Name is empty but default rules excluded - only Create ruleset checked
        var input = new RuleSetModel { Name = "", Value = 500 };
        var result = await context.ValidateAsync( input, "Create", includeDefaultRules: false );

        Assert.IsTrue( result );
        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public async Task Context_ValidateAsync_with_dynamic_ruleset_selector_should_select_correct_ruleset()
    {
        var context = CreateContextWithValidator<RuleSetModel, RuleSetModelValidator>();

        // No Id => "Create" ruleset, value within limit
        var input = new RuleSetModel { Name = "Plan", Value = 500 };
        var result = await context.ValidateAsync( input, ( ctx, model ) => string.IsNullOrEmpty( model.Id ) ? "Create" : "Update" );

        Assert.IsTrue( result );
        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public async Task Context_ValidateAsync_with_dynamic_ruleset_selector_should_fail_with_wrong_ruleset()
    {
        var context = CreateContextWithValidator<RuleSetModel, RuleSetModelValidator>();

        // Has Id => "Update" ruleset, VersionTag is empty so should fail
        var input = new RuleSetModel { Name = "Plan", Id = "123", Value = 500, VersionTag = "" };
        var result = await context.ValidateAsync( input, ( ctx, model ) => string.IsNullOrEmpty( model.Id ) ? "Create" : "Update" );

        Assert.IsFalse( result );
        Assert.IsFalse( context.IsValid() );
    }

    private class TestValidatorProvider : IValidatorProvider
    {
        private readonly Dictionary<Type, object> _validators = new();

        public void Register<TModel>( FV.IValidator<TModel> validator ) where TModel : class
        {
            _validators[typeof( TModel )] = new FluentValidatorAdapter<TModel>( validator );
        }

        public IValidator<TPlugin>? For<TPlugin>() where TPlugin : class
        {
            return _validators.TryGetValue( typeof( TPlugin ), out var validator )
                ? (IValidator<TPlugin>) validator
                : null;
        }
    }
}
