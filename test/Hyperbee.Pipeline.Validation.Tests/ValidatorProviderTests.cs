using FluentValidation;
using Hyperbee.Pipeline.Validation.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

namespace Hyperbee.Pipeline.Validation.Tests;

[TestClass]
public class ValidatorProviderTests
{
    [TestMethod]
    public void ValidatorProvider_should_return_registered_validator()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<TestOutput>, TestOutputValidator>();
        var serviceProvider = services.BuildServiceProvider();

        var provider = new ValidatorProvider( serviceProvider );

        var validator = provider.For<TestOutput>();

        Assert.IsNotNull( validator );
        Assert.IsInstanceOfType( validator, typeof( TestOutputValidator ) );
    }

    [TestMethod]
    public void ValidatorProvider_should_return_null_for_unregistered_type()
    {
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        var provider = new ValidatorProvider( serviceProvider );

        var validator = provider.For<TestOutput>();

        Assert.IsNull( validator );
    }
}
