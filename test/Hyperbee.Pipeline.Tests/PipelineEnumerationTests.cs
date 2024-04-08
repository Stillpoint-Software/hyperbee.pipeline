using System.Threading.Tasks;
using Hyperbee.Pipeline.Context;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hyperbee.Pipeline.Tests;

[TestClass]
public class PipelineEnumerationTests
{
    [TestMethod]
    public async Task Pipeline_should_enumerate_command_of_commands()
    {
        var count = 0;

        var command1 = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg + "!" )
            .Pipe( ( ctx, arg ) => count += 10 )
            .Build();

        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg.Split( ' ' ) )
            .PipeAsync( async ( ctx, arg ) =>
            {
                foreach ( var e in arg )
                    await command1( ctx, e );

                return arg;
            } )
            .Pipe( ( ctx, arg ) => string.Join( ' ', arg ) )
            .Build();

        await command( new PipelineContext(), "e f" );

        Assert.AreEqual( count, 20 );
    }

    [TestMethod]
    public async Task Pipeline_should_enumerate()
    {
        var count = 0;

        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg.Split( ' ' ) )
            .ForEach<string>( builder => builder
                .Pipe( ( ctx, arg ) => arg + "!" )
                .Pipe( ( ctx, arg ) => count += 10 )
            )
            .Pipe( ( ctx, arg ) => count += 5 )
            .Build();

        await command( new PipelineContext(), "e f" );

        Assert.AreEqual( count, 25 );
    }

    [TestMethod]
    public async Task Pipeline_should_reduce()
    {
        var command = PipelineFactory
            .Start<string>()
            .Pipe( ( ctx, arg ) => arg.Split( ' ' ) )
            .Reduce<string, int>( ( a, v ) => a + v, builder => builder
                .Pipe( ( ctx, arg ) => int.Parse( arg ) + 10 )
            )
            .Pipe( ( ctx, arg ) => arg + 5 )
            .Build();

        var result = await command( new PipelineContext(), "1 2 3 4 5" );

        Assert.AreEqual( result, 70 );
    }
}