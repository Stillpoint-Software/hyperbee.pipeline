using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Tests.TestSupport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class CommandScannerTests
{
    [TestMethod]
    public void Scanner_should_find_public_commands()
    {
        var scanner = CommandScanner.FindCommandsInAssembly( typeof( TestPublicCommand ).Assembly );
        var results = scanner.ToList();

        Assert.IsTrue( results.Any( r => r.CommandType == typeof( TestPublicCommand ) ) );
        Assert.IsTrue( results.Any( r => r.CommandType == typeof( TestPublicFunctionCommand ) ) );
        Assert.IsTrue( results.Any( r => r.CommandType == typeof( TestPublicProcedureCommand ) ) );
    }

    [TestMethod]
    public void Scanner_should_exclude_abstract_classes()
    {
        var scanner = CommandScanner.FindCommandsInAssembly( typeof( TestPublicCommand ).Assembly );
        var results = scanner.ToList();

        Assert.IsFalse( results.Any( r => r.CommandType == typeof( TestAbstractCommand ) ) );
    }

    [TestMethod]
    public void Scanner_should_exclude_internal_by_default()
    {
        var scanner = CommandScanner.FindCommandsInAssembly( typeof( TestPublicCommand ).Assembly );
        var results = scanner.ToList();

        Assert.IsFalse( results.Any( r => r.CommandType == typeof( TestInternalCommand ) ) );
    }

    [TestMethod]
    public void Scanner_should_include_internal_when_requested()
    {
        var scanner = CommandScanner.FindCommandsInAssembly(
            typeof( TestPublicCommand ).Assembly,
            includeInternalTypes: true
        );
        var results = scanner.ToList();

        Assert.IsTrue( results.Any( r => r.CommandType == typeof( TestInternalCommand ) ) );
    }

    [TestMethod]
    public void Scanner_should_scan_multiple_assemblies()
    {
        var assemblies = new[] { typeof( TestPublicCommand ).Assembly };
        var scanner = CommandScanner.FindCommandsInAssemblies( assemblies );
        var results = scanner.ToList();

        Assert.IsTrue( results.Any( r => r.CommandType == typeof( TestPublicCommand ) ) );
    }

    [TestMethod]
    public void Scanner_should_find_by_containing_type_generic()
    {
        var scanner = CommandScanner.FindCommandsInAssemblyContaining<TestPublicCommand>();
        var results = scanner.ToList();

        Assert.IsTrue( results.Any( r => r.CommandType == typeof( TestPublicCommand ) ) );
    }

    [TestMethod]
    public void Scanner_should_find_by_containing_type()
    {
        var scanner = CommandScanner.FindCommandsInAssemblyContaining( typeof( TestPublicCommand ) );
        var results = scanner.ToList();

        Assert.IsTrue( results.Any( r => r.CommandType == typeof( TestPublicCommand ) ) );
    }

    [TestMethod]
    public void Scanner_should_enumerate_results()
    {
        var scanner = CommandScanner.FindCommandsInAssembly( typeof( TestPublicCommand ).Assembly );
        var count = 0;

        foreach ( var _ in scanner )
        {
            count++;
        }

        Assert.IsTrue( count > 0 );
    }

    [TestMethod]
    public void Scanner_should_invoke_foreach_action()
    {
        var scanner = CommandScanner.FindCommandsInAssembly( typeof( TestPublicCommand ).Assembly );
        var types = new List<Type>();

        scanner.ForEach( result => types.Add( result.CommandType ) );

        Assert.IsTrue( types.Contains( typeof( TestPublicCommand ) ) );
    }

    [TestMethod]
    public void ScanResult_should_have_correct_properties()
    {
        var scanner = CommandScanner.FindCommandsInAssembly( typeof( TestPublicCommand ).Assembly );
        var result = scanner.First( r => r.CommandType == typeof( TestPublicCommand ) );

        Assert.AreEqual( typeof( TestPublicCommand ), result.CommandType );
        Assert.IsTrue( typeof( ICommand ).IsAssignableFrom( result.InterfaceType ) );
    }
}
