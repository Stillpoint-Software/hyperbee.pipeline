using System.ComponentModel.Design;
using FluentValidation;
using FluentValidation.Results;
using Hyperbee.Pipeline.Context;
using Hyperbee.Pipeline.Validation.FluentValidation.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using NSubstitute;
using FV = FluentValidation;

namespace Hyperbee.Pipeline.Validation.FluentValidation.Tests;

[TestClass]
public class PipelineValidationTests
{
    private static IPipelineContext CreateContextWithValidator<TModel, TValidator>()
        where TModel : class
        where TValidator : FV.IValidator<TModel>, new()
    {
        var container = new ServiceContainer();
        var provider = new TestValidatorProvider();
        provider.Register<TModel>( new TValidator() );
        container.AddService( typeof( Hyperbee.Pipeline.Validation.IValidatorProvider ), provider );
        var factory = PipelineContextFactory.CreateFactory( container, resetFactory: true );
        return factory.Create( Substitute.For<ILogger>() );
    }

    [TestMethod]
    public async Task Pipeline_should_pass_validation_with_valid_input()
    {
        var context = CreateContextWithValidator<TestOutput, TestOutputValidator>();

        var command = PipelineFactory
            .Start<TestOutput>()
            .ValidateAsync()
            .Pipe( ( ctx, arg ) => arg )
            .Build();

        var input = new TestOutput { Name = "Widget", ProcessedAge = 5 };
        var result = await command( context, input );

        Assert.IsTrue( context.IsValid() );
        Assert.AreEqual( "Widget", result.Name );
    }

    [TestMethod]
    public async Task Pipeline_should_cancel_on_invalid_input()
    {
        var context = CreateContextWithValidator<TestOutput, TestOutputValidator>();

        var executed = false;
        var command = PipelineFactory
            .Start<TestOutput>()
            .ValidateAsync()
            .Pipe( ( ctx, arg ) =>
            {
                executed = true;
                return arg;
            } )
            .Build();

        var input = new TestOutput { Name = "", ProcessedAge = 0 };
        await command( context, input );

        Assert.IsFalse( context.IsValid() );
        Assert.IsTrue( context.IsCanceled );
        Assert.IsFalse( executed );
    }

    [TestMethod]
    public async Task Pipeline_should_handle_null_argument()
    {
        var context = CreateContextWithValidator<TestOutput, TestOutputValidator>();

        var command = PipelineFactory
            .Start<TestOutput>()
            .ValidateAsync()
            .Build();

        var result = await command( context, null! );

        Assert.IsNull( result );
        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public async Task Pipeline_should_validate_with_rule_set()
    {
        var context = CreateContextWithValidator<RuleSetModel, RuleSetModelValidator>();

        var command = PipelineFactory
            .Start<RuleSetModel>()
            .ValidateAsync( ruleSet: "Create" )
            .Build();

        var input = new RuleSetModel { Name = "Plan", Value = 500 };
        await command( context, input );

        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public async Task Pipeline_should_fail_rule_set_validation()
    {
        var context = CreateContextWithValidator<RuleSetModel, RuleSetModelValidator>();

        var command = PipelineFactory
            .Start<RuleSetModel>()
            .ValidateAsync( ruleSet: "Create" )
            .Build();

        var input = new RuleSetModel { Name = "Plan", Value = 2000 };
        await command( context, input );

        Assert.IsFalse( context.IsValid() );
    }

    [TestMethod]
    public async Task Pipeline_should_validate_with_rule_set_excluding_defaults()
    {
        var context = CreateContextWithValidator<RuleSetModel, RuleSetModelValidator>();

        var command = PipelineFactory
            .Start<RuleSetModel>()
            .ValidateAsync( ruleSet: "Create", includeDefaultRules: false )
            .Build();

        // Name is empty but should pass because default rules are excluded
        var input = new RuleSetModel { Name = "", Value = 500 };
        await command( context, input );

        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public async Task Pipeline_should_validate_with_dynamic_rule_set_selector()
    {
        var context = CreateContextWithValidator<RuleSetModel, RuleSetModelValidator>();

        var command = PipelineFactory
            .Start<RuleSetModel>()
            .ValidateAsync( ( ctx, model ) => string.IsNullOrEmpty( model.Id ) ? "Create" : "Update" )
            .Build();

        // No Id => "Create" ruleset, value within limit
        var input = new RuleSetModel { Name = "Plan", Value = 500 };
        await command( context, input );

        Assert.IsTrue( context.IsValid() );
    }

    [TestMethod]
    public async Task Pipeline_should_execute_builder_when_valid()
    {
        var context = CreateContextWithValidator<TestOutput, TestOutputValidator>();

        var builderExecuted = false;
        var command = PipelineFactory
            .Start<TestOutput>()
            .IfValidAsync( builder => builder
                .Pipe( ( ctx, arg ) =>
                {
                    builderExecuted = true;
                    return arg;
                } )
            )
            .Build();

        var input = new TestOutput { Name = "Widget", ProcessedAge = 5 };
        await command( context, input );

        Assert.IsTrue( builderExecuted );
    }

    [TestMethod]
    public async Task Pipeline_should_skip_builder_when_invalid()
    {
        var context = CreateContextWithValidator<TestOutput, AlwaysFailValidator>();

        var builderExecuted = false;
        var command = PipelineFactory
            .Start<TestOutput>()
            .IfValidAsync( builder => builder
                .Pipe( ( ctx, arg ) =>
                {
                    builderExecuted = true;
                    return arg;
                } )
            )
            .Build();

        var input = new TestOutput { Name = "Widget", ProcessedAge = 5 };
        await command( context, input );

        Assert.IsFalse( builderExecuted );
    }

    [TestMethod]
    public async Task Pipeline_should_validate_and_cancel_on_failure()
    {
        var context = CreateContextWithValidator<TestOutput, AlwaysFailValidator>();

        var command = PipelineFactory
            .Start<TestOutput>()
            .ValidateAndCancelOnFailureAsync()
            .Build();

        var input = new TestOutput { Name = "Widget", ProcessedAge = 5 };
        await command( context, input );

        Assert.IsFalse( context.IsValid() );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task Pipeline_should_cancel_with_specific_validation_result()
    {
        var context = new PipelineContext();
        var failure = new NotFoundValidationFailure( "Item", "Item not found." );

        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + " processed" )
            .CancelWithValidationResult<string, string>( failure )
            .Pipe( ( ctx, arg ) => arg + " more" )
            .Build();

        var result = await command( context, "test" );

        Assert.IsFalse( context.IsValid() );
        Assert.IsTrue( context.IsCanceled );
        var failures = context.ValidationFailures().ToList();
        Assert.HasCount( 1, failures );
        Assert.IsInstanceOfType( failures[0], typeof( NotFoundValidationFailure ) );
    }

    [TestMethod]
    public async Task Pipeline_should_fail_when_no_validator_provider_registered()
    {
        var container = new ServiceContainer();
        var factory = PipelineContextFactory.CreateFactory( container, resetFactory: true );
        var context = factory.Create( Substitute.For<ILogger>() );

        var command = PipelineFactory
            .Start<TestOutput>()
            .ValidateAsync()
            .Build();

        var input = new TestOutput { Name = "Widget", ProcessedAge = 5 };
        await command( context, input );

        Assert.IsFalse( context.IsValid() );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task Pipeline_should_fail_when_no_validator_for_type()
    {
        var container = new ServiceContainer();
        var provider = new TestValidatorProvider();
        container.AddService( typeof( IValidatorProvider ), provider );
        var factory = PipelineContextFactory.CreateFactory( container, resetFactory: true );
        var context = factory.Create( Substitute.For<ILogger>() );

        var command = PipelineFactory
            .Start<TestOutput>()
            .ValidateAsync()
            .Build();

        var input = new TestOutput { Name = "Widget", ProcessedAge = 5 };
        await command( context, input );

        Assert.IsFalse( context.IsValid() );
        Assert.IsTrue( context.IsCanceled );
    }

    private class TestValidatorProvider : Hyperbee.Pipeline.Validation.IValidatorProvider
    {
        private readonly Dictionary<Type, object> _validators = new();

        public void Register<TModel>( FV.IValidator<TModel> validator ) where TModel : class
        {
            // Wrap FluentValidation validator in our adapter
            _validators[typeof( TModel )] = new Hyperbee.Pipeline.Validation.FluentValidation.FluentValidatorAdapter<TModel>( validator );
        }

        public Hyperbee.Pipeline.Validation.IValidator<TPlugin>? For<TPlugin>() where TPlugin : class
        {
            return _validators.TryGetValue( typeof( TPlugin ), out var validator )
                ? (Hyperbee.Pipeline.Validation.IValidator<TPlugin>) validator
                : null;
        }
    }
}
