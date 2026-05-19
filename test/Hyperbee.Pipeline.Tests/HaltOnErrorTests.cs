using System.Threading.Tasks;
using Hyperbee.Pipeline.Commands;
using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class HaltOnErrorTests
{
    // A composed command whose inner pipeline throws. Composition binds the
    // command's PipelineFunction onto the shared parent context, so the inner
    // Build() is what captures the exception.

    private sealed class ThrowingFunctionCommand : CommandFunction<string, string>, ICommandFunction<string, string>
    {
        public ThrowingFunctionCommand()
            : base( Substitute.For<IPipelineContextFactory>(), Substitute.For<ILogger>() )
        {
        }

        protected override FunctionAsync<string, string> CreatePipeline()
        {
            return PipelineFactory
                .Start<string>()
                .Pipe( ThrowString )
                .Build();
        }

        private static string ThrowString( IPipelineContext context, string argument )
            => throw new InvalidOperationException( "boom" );
    }

    private sealed class ThrowingProcedureCommand : CommandProcedure<string>, ICommandProcedure<string>
    {
        public ThrowingProcedureCommand()
            : base( Substitute.For<IPipelineContextFactory>(), Substitute.For<ILogger>() )
        {
        }

        protected override ProcedureAsync<string> CreatePipeline()
        {
            return PipelineFactory
                .Start<string>()
                .Call( ThrowVoid )
                .BuildAsProcedure();
        }

        private static void ThrowVoid( IPipelineContext context, string argument )
            => throw new InvalidOperationException( "boom" );
    }

    [TestMethod]
    public async Task Composed_command_error_should_halt_outer_pipeline_by_default()
    {
        // Arrange
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .PipeAsync( (ICommandFunction<string, string>) new ThrowingFunctionCommand() )
            .Pipe( ( _, arg ) =>
            {
                followUpRan = true;
                return arg;
            } )
            .Build();

        var context = new PipelineContext();

        // Act
        var result = await pipeline( context, "input" );

        // Assert - the step after the composed command never ran (the Ringba fix)
        Assert.IsFalse( followUpRan );
        Assert.IsNull( result );
        Assert.IsTrue( context.IsError );
        Assert.IsInstanceOfType( context.Exception, typeof( InvalidOperationException ) );
        Assert.AreEqual( "boom", context.Exception.Message );
        // halt reuses the cancellation short-circuit
        Assert.IsTrue( context.IsCanceled );
        Assert.IsFalse( context.Success );
    }

    [TestMethod]
    public async Task Composed_command_error_should_run_through_when_HaltOnError_false()
    {
        // Arrange - explicit legacy opt-out
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .PipeAsync( (ICommandFunction<string, string>) new ThrowingFunctionCommand() )
            .Pipe( ( _, arg ) =>
            {
                followUpRan = true;
                return arg ?? "ran-with-default";
            } )
            .Build();

        var context = new PipelineContext { HaltOnError = false };

        // Act
        var result = await pipeline( context, "input" );

        // Assert - prior behavior preserved: subsequent steps still run
        Assert.IsTrue( followUpRan );
        Assert.AreEqual( "ran-with-default", result );
        Assert.IsTrue( context.IsError ); // exception is still recorded
        Assert.IsFalse( context.IsCanceled ); // but the pipeline did not halt
    }

    [TestMethod]
    public async Task Boundary_should_not_throw_on_error_when_Throws_is_false()
    {
        // Arrange
        var pipeline = PipelineFactory
            .Start<string>()
            .PipeAsync( (ICommandFunction<string, string>) new ThrowingFunctionCommand() )
            .Build();

        var context = new PipelineContext(); // Throws defaults false

        // Act - boundary returns a result, does not throw
        var result = await pipeline( context, "input" );

        // Assert
        Assert.IsNull( result );
        Assert.IsTrue( context.IsError );
    }

    [TestMethod]
    public async Task Boundary_should_throw_on_error_when_Throws_is_true()
    {
        // Arrange
        var pipeline = PipelineFactory
            .Start<string>()
            .PipeAsync( (ICommandFunction<string, string>) new ThrowingFunctionCommand() )
            .Build();

        var context = new PipelineContext().Clone( throws: true );

        // Act & Assert - existing Throws semantics unchanged
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => pipeline( context, "input" ) );
    }

    [TestMethod]
    public async Task Composed_procedure_error_should_halt_outer_pipeline_by_default()
    {
        // Arrange
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .CallAsync( (ICommandProcedure<string>) new ThrowingProcedureCommand() )
            .Pipe( ( _, arg ) =>
            {
                followUpRan = true;
                return arg;
            } )
            .Build();

        var context = new PipelineContext();

        // Act
        var result = await pipeline( context, "input" );

        // Assert - follow-up skipped; Call preserves input, surfaced as the
        // cancellation value when the halt short-circuits at the boundary
        Assert.IsFalse( followUpRan );
        Assert.AreEqual( "input", result );
        Assert.IsTrue( context.IsError );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public void Manual_PipelineContext_should_default_to_HaltOnError()
    {
        Assert.IsTrue( new PipelineContext().HaltOnError );
        Assert.IsTrue( new PipelineContext().Clone().HaltOnError );
        Assert.IsFalse( new PipelineContext { HaltOnError = false }.Clone().HaltOnError );
    }

    [TestMethod]
    public async Task Plain_cancellation_should_remain_distinct_from_error()
    {
        // Arrange - a cancel (not an error) must not set IsError
        var pipeline = PipelineFactory
            .Start<int>()
            .Pipe( ( _, _ ) => 1 )
            .Pipe( ( ctx, _ ) =>
            {
                ctx.CancelAfter();
                return 2;
            } )
            .Pipe( ( _, _ ) => 3 )
            .Build();

        var context = new PipelineContext();

        // Act
        var result = await pipeline( context, 0 );

        // Assert - existing cancellation behavior is unaffected by halt-on-error
        Assert.AreEqual( 2, result );
        Assert.IsTrue( context.IsCanceled );
        Assert.IsFalse( context.IsError );
    }

    // --- hardening: composition shapes, nesting, conditionals, enumeration, parallel ---

    private sealed class ThrowingIntCommand : CommandFunction<int, int>, ICommandFunction<int, int>
    {
        public ThrowingIntCommand()
            : base( Substitute.For<IPipelineContextFactory>(), Substitute.For<ILogger>() )
        {
        }

        protected override FunctionAsync<int, int> CreatePipeline()
            => PipelineFactory.Start<int>().Pipe( ThrowInt ).Build();

        private static int ThrowInt( IPipelineContext context, int argument )
            => throw new InvalidOperationException( "boom" );
    }

    // A command whose pipeline composes another (throwing) command - command of command.
    private sealed class NestingCommand : CommandFunction<string, string>, ICommandFunction<string, string>
    {
        public NestingCommand()
            : base( Substitute.For<IPipelineContextFactory>(), Substitute.For<ILogger>() )
        {
        }

        protected override FunctionAsync<string, string> CreatePipeline()
            => PipelineFactory
                .Start<string>()
                .PipeAsync( (ICommandFunction<string, string>) new ThrowingFunctionCommand() )
                .Build();
    }

    private sealed class SideEffectProcedureCommand : CommandProcedure<string>, ICommandProcedure<string>
    {
        public bool Ran { get; private set; }

        public SideEffectProcedureCommand()
            : base( Substitute.For<IPipelineContextFactory>(), Substitute.For<ILogger>() )
        {
        }

        protected override ProcedureAsync<string> CreatePipeline()
            => PipelineFactory
                .Start<string>()
                .Call( ( _, _ ) => Ran = true )
                .BuildAsProcedure();
    }

    [TestMethod]
    public async Task Nested_composed_command_error_should_halt_through_all_levels()
    {
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .PipeAsync( (ICommandFunction<string, string>) new NestingCommand() ) // composes ThrowingFunctionCommand
            .Pipe( ( _, arg ) => { followUpRan = true; return arg; } )
            .Build();

        var context = new PipelineContext();

        var result = await pipeline( context, "input" );

        Assert.IsFalse( followUpRan );
        Assert.IsNull( result );
        Assert.IsTrue( context.IsError );
        Assert.AreEqual( "boom", context.Exception.Message );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task PipeIf_true_with_throwing_command_should_halt()
    {
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .PipeIf( ( _, _ ) => true, (ICommandFunction<string, string>) new ThrowingFunctionCommand() )
            .Pipe( ( _, arg ) => { followUpRan = true; return arg; } )
            .Build();

        var context = new PipelineContext();

        await pipeline( context, "input" );

        Assert.IsFalse( followUpRan );
        Assert.IsTrue( context.IsError );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task PipeIf_false_should_not_error_or_halt()
    {
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .PipeIf( ( _, _ ) => false, (ICommandFunction<string, string>) new ThrowingFunctionCommand() )
            .Pipe( ( _, arg ) => { followUpRan = true; return arg + "-ok"; } )
            .Build();

        var context = new PipelineContext();

        var result = await pipeline( context, "input" );

        Assert.IsTrue( followUpRan );
        Assert.AreEqual( "input-ok", result );
        Assert.IsFalse( context.IsError );
        Assert.IsFalse( context.IsCanceled );
    }

    [TestMethod]
    public async Task CallIf_true_with_throwing_command_should_halt()
    {
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .CallIf( ( _, _ ) => true, (ICommandProcedure<string>) new ThrowingProcedureCommand() )
            .Pipe( ( _, arg ) => { followUpRan = true; return arg; } )
            .Build();

        var context = new PipelineContext();

        await pipeline( context, "input" );

        Assert.IsFalse( followUpRan );
        Assert.IsTrue( context.IsError );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task PipeAsync_with_selector_throwing_command_should_halt()
    {
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .PipeAsync( new ThrowingIntCommand(), selector: ( _, arg ) => arg.Length )
            .Pipe( ( _, arg ) => { followUpRan = true; return arg; } )
            .Build();

        var context = new PipelineContext();

        await pipeline( context, "input" );

        Assert.IsFalse( followUpRan );
        Assert.IsTrue( context.IsError );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task Error_should_prevent_subsequent_side_effect_command()
    {
        var sideEffect = new SideEffectProcedureCommand();

        var pipeline = PipelineFactory
            .Start<string>()
            .PipeAsync( (ICommandFunction<string, string>) new ThrowingFunctionCommand() )
            .CallAsync( (ICommandProcedure<string>) sideEffect )
            .Build();

        var context = new PipelineContext();

        await pipeline( context, "input" );

        // the side effect (e.g. "send email") must not run after an upstream error
        Assert.IsFalse( sideEffect.Ran );
        Assert.IsTrue( context.IsError );
    }

    [TestMethod]
    public async Task Value_type_pipeline_error_should_return_default_and_record_error()
    {
        var pipeline = PipelineFactory
            .Start<int>()
            .PipeAsync( (ICommandFunction<int, int>) new ThrowingIntCommand() )
            .Pipe( ( _, n ) => n + 1 )
            .Build();

        var context = new PipelineContext();

        var result = await pipeline( context, 41 );

        Assert.AreEqual( 0, result ); // default(int), not 42
        Assert.IsTrue( context.IsError );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task ForEach_composed_command_error_should_halt_remaining_iterations_and_outer()
    {
        var processed = 0;
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( _, arg ) => arg.Split( ' ' ) )
            .ForEach().Type<string>( builder => builder
                .Pipe( ( _, e ) => { processed++; return e; } )
                .PipeAsync( (ICommandFunction<string, string>) new ThrowingFunctionCommand() )
            )
            .Pipe( ( _, arg ) => { followUpRan = true; return arg; } )
            .Build();

        var context = new PipelineContext();

        await pipeline( context, "a b c" );

        // only the first element is processed before the composed command halts
        Assert.AreEqual( 1, processed );
        Assert.IsFalse( followUpRan );
        Assert.IsTrue( context.IsError );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task WaitAll_branch_error_should_be_isolated_to_fork_and_not_auto_halt_parent()
    {
        WaitAllResult[] captured = null;
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .WaitAll<string, string, string>(
                b => b.Create(
                    branch => branch.Pipe( ( _, arg ) => arg + "-ok" ),
                    branch => branch.PipeAsync( (ICommandFunction<string, string>) new ThrowingFunctionCommand() )
                ),
                ( _, input, results ) => { captured = results; return input; }
            )
            .Pipe( ( _, arg ) => { followUpRan = true; return arg; } )
            .Build();

        var context = new PipelineContext();

        var result = await pipeline( context, "input" );

        // the failing branch's error is contained on its forked context...
        Assert.IsNotNull( captured );
        Assert.HasCount( 2, captured );
        var anyForkErrored = false;
        foreach ( var r in captured )
            anyForkErrored |= r.Context.IsError;
        Assert.IsTrue( anyForkErrored );

        // ...and does NOT auto-halt the parent (join semantics are the reducer's job)
        Assert.IsFalse( context.IsError );
        Assert.IsFalse( context.IsCanceled );
        Assert.IsTrue( followUpRan );
        Assert.AreEqual( "input", result );
    }

    [TestMethod]
    public async Task WaitAll_reducer_can_propagate_branch_error_to_halt_parent()
    {
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .WaitAll<string, string, string>(
                b => b.Create(
                    branch => branch.Pipe( ( _, arg ) => arg ),
                    branch => branch.PipeAsync( (ICommandFunction<string, string>) new ThrowingFunctionCommand() )
                ),
                ( ctx, input, results ) =>
                {
                    foreach ( var r in results )
                    {
                        if ( r.Context.IsError )
                        {
                            ctx.CancelAfter(); // recommended pattern to propagate a branch failure
                            break;
                        }
                    }

                    return input;
                }
            )
            .Pipe( ( _, arg ) => { followUpRan = true; return arg; } )
            .Build();

        var context = new PipelineContext();

        await pipeline( context, "input" );

        Assert.IsFalse( followUpRan );
        Assert.IsTrue( context.IsCanceled );
    }

    [TestMethod]
    public async Task Programmatic_context_Exception_without_throw_should_not_halt()
    {
        // halt-on-error triggers only on a thrown-and-caught exception, not on a
        // programmatic context.Exception assignment (consistent with the validation
        // / CancelAfter pattern for deliberate short-circuiting).
        var followUpRan = false;

        var pipeline = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) =>
            {
                ctx.Exception = new InvalidOperationException( "annotated, not thrown" );
                return arg;
            } )
            .Pipe( ( _, arg ) => { followUpRan = true; return arg + "-ok"; } )
            .Build();

        var context = new PipelineContext();

        var result = await pipeline( context, "input" );

        Assert.IsTrue( followUpRan );
        Assert.AreEqual( "input-ok", result );
        Assert.IsTrue( context.IsError );      // recorded
        Assert.IsFalse( context.IsCanceled );  // but not halted
    }
}
