using System.Threading;
using System.Threading.Tasks;
using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class CommandFunctionTests
{
    [TestMethod]
    public async Task CommandFunction_ExecuteAsync_Should_Pass_CancellationToken_To_Context()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var mockContextFactory = Substitute.For<IPipelineContextFactory>();
        var mockContext = Substitute.For<IPipelineContext>();

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Setup the factory to return our mock context
        mockContextFactory.Create( logger, cancellationToken ).Returns( mockContext );

        // Create a test command function
        var testCommand = new TestCommandFunction( mockContextFactory, logger );

        // Act
        await testCommand.ExecuteAsync( 0, cancellationToken );

        // Assert
        mockContextFactory.Received( 1 ).Create( logger, cancellationToken );
    }

    [TestMethod]
    public async Task CommandFunction_ExecuteAsync_WithoutArgument_Should_Pass_CancellationToken_To_Context()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var mockContextFactory = Substitute.For<IPipelineContextFactory>();
        var mockContext = Substitute.For<IPipelineContext>();

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        // Setup the factory to return our mock context
        mockContextFactory.Create( logger, cancellationToken ).Returns( mockContext );

        // Create a test command function (parameterless version)
        var testCommand = new TestCommandFunctionParameterless( mockContextFactory, logger );

        // Act
        await testCommand.ExecuteAsync( cancellationToken );

        // Assert
        mockContextFactory.Received( 1 ).Create( logger, cancellationToken );
    }

    [TestMethod]
    public async Task CommandFunction_ExecuteAsync_Should_Pass_Default_CancellationToken_When_None_Provided()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var mockContextFactory = Substitute.For<IPipelineContextFactory>();
        var mockContext = Substitute.For<IPipelineContext>();

        // Setup the factory to return our mock context with default cancellation token
        mockContextFactory.Create( logger, default( CancellationToken ) ).Returns( mockContext );

        var testCommand = new TestCommandFunction( mockContextFactory, logger );

        // Act
        await testCommand.ExecuteAsync( 0 );

        // Assert
        mockContextFactory.Received( 1 ).Create( logger, default( CancellationToken ) );
    }

    [TestMethod]
    public async Task CommandFunction_ExecuteAsync_Should_Return_Context_With_Correct_CancellationToken()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var contextFactory = PipelineContextFactory.CreateFactory( resetFactory: true );

        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var testCommand = new TestCommandFunction( contextFactory, logger );

        // Act
        var result = await testCommand.ExecuteAsync( 0, cancellationToken );

        // Assert
        Assert.AreEqual( cancellationToken, result.Context.CancellationToken );
    }

    // Test implementation of CommandFunction<TStart, TOutput>
    private class TestCommandFunction : CommandFunction<int, int>
    {
        public TestCommandFunction( IPipelineContextFactory pipelineContextFactory, ILogger logger )
            : base( pipelineContextFactory, logger )
        {
        }

        protected override FunctionAsync<int, int> CreatePipeline()
        {
            return PipelineFactory
                .Start<int>()
                .Pipe( ( context, input ) => 42 )
                .Build();
        }
    }

    // Test implementation of CommandFunction<TOutput> (parameterless version)
    private class TestCommandFunctionParameterless : CommandFunction<int>
    {
        public TestCommandFunctionParameterless( IPipelineContextFactory pipelineContextFactory, ILogger logger )
            : base( pipelineContextFactory, logger )
        {
        }

        protected override FunctionAsync<Arg.Empty, int> CreatePipeline()
        {
            return PipelineFactory
                .Start<Arg.Empty>()
                .Pipe( ( context, input ) => 42 )
                .Build();
        }
    }
}
