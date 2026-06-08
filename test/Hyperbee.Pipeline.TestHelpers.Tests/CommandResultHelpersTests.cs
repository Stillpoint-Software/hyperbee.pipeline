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
}
