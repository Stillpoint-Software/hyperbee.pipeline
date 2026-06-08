using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.TestHelpers;
using NSubstitute;

namespace Hyperbee.Pipeline.TestHelpers.Tests;

[TestClass]
public class CommandFunctionHelpersTests
{
    [TestMethod]
    public async Task MockSuccessfulResult_procedure_returns_success()
    {
        var procedure = Substitute.For<ICommandProcedure<string>>();

        procedure.MockSuccessfulResult();

        var result = await procedure.ExecuteAsync( "input" );

        Assert.IsNotNull( result );
        Assert.IsFalse( result.Context.IsError );
    }

    [TestMethod]
    public async Task MockExceptionCommandResult_procedure_returns_error()
    {
        var exception = new InvalidOperationException( "boom" );
        var procedure = Substitute.For<ICommandProcedure<string>>();

        procedure.MockExceptionCommandResult( exception );

        var result = await procedure.ExecuteAsync( "input" );

        Assert.IsTrue( result.Context.IsError );
        Assert.AreSame( exception, result.Context.Exception );
        Assert.ThrowsExactly<InvalidOperationException>( () => result.Context.ThrowIfError() );
    }
}
