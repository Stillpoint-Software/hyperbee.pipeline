using FluentValidation;
using Hyperbee.Pipeline.Validation;
using Hyperbee.Pipeline.Validation.FluentValidation;
using Hyperbee.Pipeline.Validation.FluentValidation.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using FV = FluentValidation;

namespace Hyperbee.Pipeline.Validation.FluentValidation.Tests;

[TestClass]
public class ValidatorProviderTests
{
    [TestMethod]
    public void ValidatorProvider_should_return_registered_validator()
    {
        var services = new ServiceCollection();
        services.AddSingleton<FV.IValidator<TestOutput>, TestOutputValidator>();
        var serviceProvider = services.BuildServiceProvider();

        var provider = new FluentValidatorProvider( serviceProvider );

        var validator = provider.For<TestOutput>();

        Assert.IsNotNull( validator );
        Assert.IsInstanceOfType<Hyperbee.Pipeline.Validation.IValidator<TestOutput>>( validator );
    }

    [TestMethod]
    public void ValidatorProvider_should_return_null_for_unregistered_type()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var provider = new FluentValidatorProvider( serviceProvider );

        var validator = provider.For<TestOutput>();

        Assert.IsNull( validator );
    }

    [TestMethod]
    public void AddPipelineValidation_UseFluentValidation_should_register_provider()
    {
        var services = new ServiceCollection();
        services.AddPipelineValidation( config =>
            config.UseFluentValidation() );

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetService<IValidatorProvider>();

        Assert.IsNotNull( provider );
        Assert.IsInstanceOfType<FluentValidatorProvider>( provider );
    }

    [TestMethod]
    public void AddPipelineValidation_UseFluentValidation_with_scan_should_resolve_validator()
    {
        var services = new ServiceCollection();
        services.AddPipelineValidation( config =>
            config.UseFluentValidation( options =>
                options.ScanAssembly( typeof( TestOutputValidator ).Assembly ) ) );

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IValidatorProvider>();
        var validator = provider.For<TestOutput>();

        Assert.IsNotNull( validator );
    }

    [TestMethod]
    public void AddPipelineValidation_UseFluentValidation_returns_services_for_chaining()
    {
        var services = new ServiceCollection();

        // Verify AddPipelineValidation returns IServiceCollection so further registrations can chain
        var returned = services.AddPipelineValidation( config =>
            config.UseFluentValidation() );

        Assert.AreSame( services, returned );
    }

    [TestMethod]
    public void ScanAssembly_with_default_lifetime_should_register_validators_as_scoped()
    {
        var services = new ServiceCollection();
        services.AddPipelineValidation( config =>
            config.UseFluentValidation( options =>
                options.ScanAssembly( typeof( TestOutputValidator ).Assembly ) ) );

        var descriptor = services.FirstOrDefault( d => d.ServiceType == typeof( FV.IValidator<TestOutput> ) );

        Assert.IsNotNull( descriptor );
        Assert.AreEqual( ServiceLifetime.Scoped, descriptor.Lifetime );
    }

    [TestMethod]
    public void ScanAssembly_with_singleton_lifetime_should_register_validators_as_singleton()
    {
        var services = new ServiceCollection();
        services.AddPipelineValidation( config =>
            config.UseFluentValidation( options =>
                options.ScanAssembly( typeof( TestOutputValidator ).Assembly, ServiceLifetime.Singleton ) ) );

        var descriptor = services.FirstOrDefault( d => d.ServiceType == typeof( FV.IValidator<TestOutput> ) );

        Assert.IsNotNull( descriptor );
        Assert.AreEqual( ServiceLifetime.Singleton, descriptor.Lifetime );
    }

    [TestMethod]
    public void AddPipelineValidation_UseFluentValidation_with_preregistered_validators_should_resolve()
    {
        var services = new ServiceCollection();

        // Simulate a user who already registered their FV validators separately
        services.AddSingleton<FV.IValidator<TestOutput>, TestOutputValidator>();

        // UseFluentValidation without scanner - just wires the provider
        services.AddPipelineValidation( config => config.UseFluentValidation() );

        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<IValidatorProvider>();
        var validator = provider.For<TestOutput>();

        Assert.IsNotNull( validator );
    }
}
