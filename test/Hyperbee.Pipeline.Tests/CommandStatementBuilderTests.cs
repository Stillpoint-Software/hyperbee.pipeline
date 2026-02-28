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

    private class DoubleIntCommand : CommandFunction<int, int>, ICommandFunction<int, int>
    {
        public DoubleIntCommand()
            : base( Substitute.For<IPipelineContextFactory>(), Substitute.For<ILogger>() )
        {
        }

        protected override FunctionAsync<int, int> CreatePipeline()
        {
            return PipelineFactory
                .Start<int>()
                .Pipe( ( ctx, arg ) => arg * 2 )
                .Build();
        }
    }

    private class LogIntProcedureCommand : CommandProcedure<int>, ICommandProcedure<int>
    {
        public int LastValue { get; private set; }

        public LogIntProcedureCommand()
            : base( Substitute.For<IPipelineContextFactory>(), Substitute.For<ILogger>() )
        {
        }

        protected override ProcedureAsync<int> CreatePipeline()
        {
            return PipelineFactory
                .Start<int>()
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
            .PipeAsync( (ICommandFunction<string, string>) command )
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
            .PipeAsync( (ICommandFunction<string, int>) command )
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
            .PipeAsync( (ICommandFunction<string, string>) command )
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
            .PipeAsync( (ICommandFunction<string, string>) command, "append-exclamation" )
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
            .CallAsync( (ICommandProcedure<string>) command )
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
            .CallAsync( (ICommandProcedure<string>) command, "log-step" )
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
            .PipeIf( ( ctx, arg ) => true, (ICommandFunction<string, string>) command )
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
            .PipeIf( ( ctx, arg ) => false, (ICommandFunction<string, string>) command )
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
            .CallIf( ( ctx, arg ) => true, (ICommandProcedure<string>) command )
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
            .CallIf( ( ctx, arg ) => false, (ICommandProcedure<string>) command )
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
            .CallIf( ( ctx, arg ) => true, (ICommandProcedure<string>) command )
            .Pipe( ( ctx, arg ) => arg + " done" )
            .Build();

        // Act
        var result = await pipeline( new PipelineContext(), "world" );

        // Assert
        Assert.AreEqual( "hello world done", result );
        Assert.AreEqual( "hello world", command.LastValue );
    }

    // PipelineFunction property tests

    [TestMethod]
    public async Task PipelineFunction_should_return_function_delegate()
    {
        // Arrange
        ICommandFunction<string, string> command = new AppendExclamationCommand();

        // Act
        var result = await command.PipelineFunction( new PipelineContext(), "test" );

        // Assert
        Assert.AreEqual( "test!", result );
    }

    [TestMethod]
    public async Task PipelineFunction_should_return_procedure_delegate()
    {
        // Arrange
        var concreteCommand = new LogProcedureCommand();
        ICommandProcedure<string> command = concreteCommand;

        // Act
        await command.PipelineFunction( new PipelineContext(), "test" );

        // Assert
        Assert.AreEqual( "test", concreteCommand.LastValue );
    }

    // Selector overload tests

    [TestMethod]
    public async Task PipeAsync_with_selector_should_map_type_before_command()
    {
        // Arrange - string pipeline, command takes int, selector maps string -> int
        var command = new DoubleIntCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .PipeAsync( command, selector: ( ctx, arg ) => arg.Length )
            .Build();

        // Act
        var result = await pipeline( new PipelineContext(), "world" );

        // Assert - "hello world".Length == 11, doubled == 22
        Assert.AreEqual( "hello world".Length * 2, result );
    }

    [TestMethod]
    public async Task PipeAsync_with_selector_should_share_context()
    {
        // Arrange
        var command = new DoubleIntCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) =>
            {
                ctx.Items.SetValue( "key", "shared" );
                return arg;
            } )
            .PipeAsync( command, selector: ( ctx, arg ) => arg.Length )
            .Build();

        // Act
        var context = new PipelineContext();
        await pipeline( context, "hi" );

        // Assert
        Assert.IsTrue( context.Items.TryGetValue<string>( "key", out var value ) );
        Assert.AreEqual( "shared", value );
    }

    [TestMethod]
    public async Task CallAsync_with_selector_should_map_type_before_procedure_and_preserve_input()
    {
        // Arrange - string pipeline, procedure takes int, selector maps string -> int
        var command = new LogIntProcedureCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => $"hello {arg}" )
            .CallAsync( command, selector: ( ctx, arg ) => arg.Length )
            .Pipe( ( ctx, arg ) => arg + " done" )
            .Build();

        // Act
        var result = await pipeline( new PipelineContext(), "world" );

        // Assert - original string preserved, procedure received the length
        Assert.AreEqual( "hello world done", result );
        Assert.AreEqual( "hello world".Length, command.LastValue );
    }

    [TestMethod]
    public async Task CallAsync_with_selector_should_share_context_and_preserve_input()
    {
        // Arrange
        var command = new LogIntProcedureCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) =>
            {
                ctx.Items.SetValue( "key", "shared" );
                return $"hello {arg}";
            } )
            .CallAsync( command, selector: ( ctx, arg ) => arg.Length )
            .Build();

        // Act
        var context = new PipelineContext();
        var result = await pipeline( context, "world" );

        // Assert - input string preserved, context shared, procedure received the length
        Assert.AreEqual( "hello world", result );
        Assert.AreEqual( "hello world".Length, command.LastValue );
        Assert.IsTrue( context.Items.TryGetValue<string>( "key", out var value ) );
        Assert.AreEqual( "shared", value );
    }
}