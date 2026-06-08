using Hyperbee.Pipeline.TestHelpers;
using Hyperbee.Pipeline.Validation;

namespace Hyperbee.Pipeline.TestHelpers.Tests;

[TestClass]
public class CommandResultHelpersTests
{
    [TestMethod]
    public void CreateWithException_generic_wires_error_state_and_throws()
    {
        var exception = new InvalidOperationException( "boom" );

        var result = CommandResultHelpers.CreateWithException<string>( exception );

        Assert.IsTrue( result.Context.IsError );
        Assert.AreSame( exception, result.Context.Exception );
        Assert.AreEqual( typeof( string ), result.CommandType );
        Assert.ThrowsExactly<InvalidOperationException>( () => result.Context.ThrowIfError() );
    }

    [TestMethod]
    public void CreateSuccess_procedure_is_not_error_and_is_valid()
    {
        var result = CommandResultHelpers.CreateSuccess();

        Assert.IsNotNull( result.Context );
        Assert.IsFalse( result.Context.IsError );
        Assert.IsTrue( result.Context.GetValidationResult()!.IsValid );
    }

    [TestMethod]
    public void CreateWithException_procedure_wires_error_state_and_throws()
    {
        var exception = new InvalidOperationException( "boom" );

        var result = CommandResultHelpers.CreateWithException( exception );

        Assert.IsTrue( result.Context.IsError );
        Assert.AreSame( exception, result.Context.Exception );
        Assert.ThrowsExactly<InvalidOperationException>( () => result.Context.ThrowIfError() );
    }
}
