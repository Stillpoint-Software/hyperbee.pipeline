using FV = FluentValidation;

﻿using FluentValidation;
using Hyperbee.Pipeline.Validation.FluentValidation;
using Hyperbee.Pipeline.Validation.FluentValidation.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;

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
}
