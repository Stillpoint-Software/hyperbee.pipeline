using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Tests.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class CommandExtensionsTests
{
    [TestMethod]
    public void AddPipelineCommands_should_register_from_assembly()
    {
        var services = new ServiceCollection();

        services.AddPipelineCommands( typeof( TestPublicCommand ).Assembly );

        var provider = services.BuildServiceProvider();
        var commands = provider.GetServices<ICommandFunction<string>>();

        Assert.IsTrue( commands.Any( c => c.GetType() == typeof( TestPublicCommand ) ) );
    }

    [TestMethod]
    public void AddPipelineCommands_should_not_register_abstract()
    {
        var services = new ServiceCollection();

        services.AddPipelineCommands( typeof( TestPublicCommand ).Assembly );

        Assert.IsFalse( services.Any( s => s.ImplementationType == typeof( TestAbstractCommand ) ) );
    }

    [TestMethod]
    public void AddPipelineCommands_should_exclude_internal_by_default()
    {
        var services = new ServiceCollection();

        services.AddPipelineCommands( typeof( TestPublicCommand ).Assembly );

        Assert.IsFalse( services.Any( s => s.ImplementationType == typeof( TestInternalCommand ) ) );
    }

    [TestMethod]
    public void AddPipelineCommands_should_include_internal_when_requested()
    {
        var services = new ServiceCollection();

        services.AddPipelineCommands(
            typeof( TestPublicCommand ).Assembly,
            includeInternalTypes: true
        );

        Assert.IsTrue( services.Any( s => s.ImplementationType == typeof( TestInternalCommand ) ) );
    }

    [TestMethod]
    public void AddPipelineCommands_should_register_with_transient_by_default()
    {
        var services = new ServiceCollection();

        services.AddPipelineCommands( typeof( TestPublicCommand ).Assembly );

        var descriptor = services.First( s => s.ImplementationType == typeof( TestPublicCommand ) );
        Assert.AreEqual( ServiceLifetime.Transient, descriptor.Lifetime );
    }

    [TestMethod]
    public void AddPipelineCommands_should_register_with_specified_lifetime()
    {
        var services = new ServiceCollection();

        services.AddPipelineCommands(
            typeof( TestPublicCommand ).Assembly,
            lifetime: ServiceLifetime.Scoped
        );

        var descriptor = services.First( s => s.ImplementationType == typeof( TestPublicCommand ) );
        Assert.AreEqual( ServiceLifetime.Scoped, descriptor.Lifetime );
    }

    [TestMethod]
    public void AddPipelineCommands_should_apply_filter()
    {
        var services = new ServiceCollection();

        services.AddPipelineCommands(
            typeof( TestPublicCommand ).Assembly,
            filter: result => result.CommandType != typeof( TestPublicCommand )
        );

        Assert.IsFalse( services.Any( s => s.ImplementationType == typeof( TestPublicCommand ) ) );
        Assert.IsTrue( services.Any( s => s.ImplementationType == typeof( TestPublicFunctionCommand ) ) );
    }

    [TestMethod]
    public void AddPipelineCommands_should_return_same_service_collection()
    {
        var services = new ServiceCollection();

        var result = services.AddPipelineCommands( typeof( TestPublicCommand ).Assembly );

        Assert.AreSame( services, result );
    }

    [TestMethod]
    public void AddPipelineCommands_from_multiple_assemblies_should_register()
    {
        var services = new ServiceCollection();
        var assemblies = new[] { typeof( TestPublicCommand ).Assembly };

        services.AddPipelineCommands( assemblies );

        Assert.IsTrue( services.Any( s => s.ImplementationType == typeof( TestPublicCommand ) ) );
    }

    [TestMethod]
    public void AddPipelineCommandsFromAssemblyContaining_should_register()
    {
        var services = new ServiceCollection();

        services.AddPipelineCommandsFromAssemblyContaining<TestPublicCommand>();

        Assert.IsTrue( services.Any( s => s.ImplementationType == typeof( TestPublicCommand ) ) );
    }
}
