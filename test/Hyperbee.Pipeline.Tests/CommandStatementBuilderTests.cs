using System.Threading.Tasks;
using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class CommandStatementBuilderTests
{
    // Test command implementations

    private class AppendExclamationCommand : CommandFunction<string, string>, ICommandFunction<string, string>
    {
        public AppendExclamationCommand()
            : base( Substitute.For<IPipelineContextFactory>(), Substitute.For<ILogger>() )
        {
        }

        protected override FunctionAsync<string, string> CreatePipeline()
        {
            return PipelineFactory
                .Start<string>()
                .Pipe( ( ctx, arg ) => arg + "!" )
                .Build();
        }
    }

    private class StringToLengthCommand : CommandFunction<string, int>, ICommandFunction<string, int>
    {
        public StringToLengthCommand()
            : base( Substitute.For<IPipelineContextFactory>(), Substitute.For<ILogger>() )
        {
        }

        protected override FunctionAsync<string, int> CreatePipeline()
        {
            return PipelineFactory
                .Start<string>()
                .Pipe( ( ctx, arg ) => arg.Length )
                .Build();
        }
    }

    private class LogProcedureCommand : CommandProcedure<string>, ICommandProcedure<string>
    {
        public string LastValue { get; private set; }

        public LogProcedureCommand()
            : base( Substitute.For<IPipelineContextFactory>(), Substitute.For<ILogger>() )
        {
        }

        protected override ProcedureAsync<string> CreatePipeline()
        {
            return PipelineFactory
                .Start<string>()
                .Call( ( ctx, arg ) => LastValue = arg )
                .BuildAsProcedure();
        }
    }

    // PipeAsync tests

    [TestMethod]
    public async Task PipeAsync_should_compose_command_into_pipeline()
    {
        // Arrange
        var command = new AppendExclamationCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .PipeAsync( command )
            .Build();

        // Act
        var result = await pipeline( new PipelineContext(), "world" );

        // Assert
        Assert.AreEqual( "hello world!", result );
    }

    [TestMethod]
    public async Task PipeAsync_should_compose_command_with_type_change()
    {
        // Arrange
        var command = new StringToLengthCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .PipeAsync( command )
            .Build();

        // Act
        var result = await pipeline( new PipelineContext(), "world" );

        // Assert
        Assert.AreEqual( "hello world".Length, result );
    }

    [TestMethod]
    public async Task PipeAsync_should_share_context_with_command()
    {
        // Arrange
        var command = new AppendExclamationCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) =>
            {
                ctx.Items.SetValue( "key", "shared" );
                return arg;
            } )
            .PipeAsync( command )
            .Build();

        // Act
        var context = new PipelineContext();
        await pipeline( context, "test" );

        // Assert
        Assert.IsTrue( context.Items.TryGetValue<string>( "key", out var value ) );
        Assert.AreEqual( "shared", value );
    }

    [TestMethod]
    public async Task PipeAsync_command_with_name_should_compose()
    {
        // Arrange
        var command = new AppendExclamationCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .PipeAsync( command, "append-exclamation" )
            .Build();

        // Act
        var result = await pipeline( new PipelineContext(), "world" );

        // Assert
        Assert.AreEqual( "hello world!", result );
    }

    // CallAsync tests

    [TestMethod]
    public async Task CallAsync_should_run_command_for_side_effect_and_preserve_input()
    {
        // Arrange
        var command = new LogProcedureCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .CallAsync( command )
            .Pipe( ( ctx, arg ) => arg + " done" )
            .Build();

        // Act
        var result = await pipeline( new PipelineContext(), "world" );

        // Assert
        Assert.AreEqual( "hello world done", result );
        Assert.AreEqual( "hello world", command.LastValue );
    }

    [TestMethod]
    public async Task CallAsync_command_with_name_should_compose()
    {
        // Arrange
        var command = new LogProcedureCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .CallAsync( command, "log-step" )
            .Pipe( ( ctx, arg ) => arg + " done" )
            .Build();

        // Act
        var result = await pipeline( new PipelineContext(), "world" );

        // Assert
        Assert.AreEqual( "hello world done", result );
        Assert.AreEqual( "hello world", command.LastValue );
    }

    // PipeIf tests

    [TestMethod]
    public async Task PipeIf_should_run_command_when_condition_is_true()
    {
        // Arrange
        var command = new AppendExclamationCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .PipeIf( ( ctx, arg ) => true, command )
            .Build();

        // Act
        var result = await pipeline( new PipelineContext(), "world" );

        // Assert
        Assert.AreEqual( "hello world!", result );
    }

    [TestMethod]
    public async Task PipeIf_should_skip_command_when_condition_is_false()
    {
        // Arrange
        var command = new AppendExclamationCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .PipeIf( ( ctx, arg ) => false, command )
            .Build();

        // Act
        var result = await pipeline( new PipelineContext(), "world" );

        // Assert - condition false passes through input unchanged
        Assert.AreEqual( "hello world", result );
    }

    // CallIf tests

    [TestMethod]
    public async Task CallIf_should_run_command_when_condition_is_true()
    {
        // Arrange
        var command = new LogProcedureCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .CallIf( ( ctx, arg ) => true, command )
            .Build();

        // Act
        await pipeline( new PipelineContext(), "world" );

        // Assert
        Assert.AreEqual( "hello world", command.LastValue );
    }

    [TestMethod]
    public async Task CallIf_should_skip_command_when_condition_is_false()
    {
        // Arrange
        var command = new LogProcedureCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .CallIf( ( ctx, arg ) => false, command )
            .Build();

        // Act
        await pipeline( new PipelineContext(), "world" );

        // Assert
        Assert.IsNull( command.LastValue );
    }

    [TestMethod]
    public async Task CallIf_should_preserve_input_when_condition_is_true()
    {
        // Arrange
        var command = new LogProcedureCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .CallIf( ( ctx, arg ) => true, command )
            .Pipe( ( ctx, arg ) => arg + " done" )
            .Build();

        // Act
        var result = await pipeline( new PipelineContext(), "world" );

        // Assert
        Assert.AreEqual( "hello world done", result );
        Assert.AreEqual( "hello world", command.LastValue );
    }

    // Implicit cast tests

    [TestMethod]
    public async Task Implicit_cast_should_convert_CommandFunction_to_FunctionAsync()
    {
        // Arrange
        var command = new AppendExclamationCommand();
        FunctionAsync<string, string> function = command;

        // Act
        var result = await function( new PipelineContext(), "test" );

        // Assert
        Assert.AreEqual( "test!", result );
    }

    [TestMethod]
    public async Task Implicit_cast_should_convert_CommandProcedure_to_ProcedureAsync()
    {
        // Arrange
        var command = new LogProcedureCommand();
        ProcedureAsync<string> procedure = command;

        // Act
        await procedure( new PipelineContext(), "test" );

        // Assert
        Assert.AreEqual( "test", command.LastValue );
    }

    [TestMethod]
    public async Task PipelineFunction_property_should_return_pipeline_delegate()
    {
        // Arrange
        ICommandFunction<string, string> command = new AppendExclamationCommand();

        // Act
        var result = await command.PipelineFunction( new PipelineContext(), "test" );

        // Assert
        Assert.AreEqual( "test!", result );
    }

    [TestMethod]
    public async Task PipelineFunction_property_should_return_procedure_delegate()
    {
        // Arrange
        var concreteCommand = new LogProcedureCommand();
        ICommandProcedure<string> command = concreteCommand;

        // Act
        await command.PipelineFunction( new PipelineContext(), "test" );

        // Assert
        Assert.AreEqual( "test", concreteCommand.LastValue );
    }
}
