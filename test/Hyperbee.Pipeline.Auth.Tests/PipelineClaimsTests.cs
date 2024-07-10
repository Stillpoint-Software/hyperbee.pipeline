using System.ComponentModel.Design;
using System.Security.Claims;
using Hyperbee.Pipeline.Auth.Tests.TestSupport;
using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hyperbee.Pipeline.Auth.Tests;

[TestClass]
public class PipelineClaimsTests
{
    [TestMethod]
    public async Task Should_return_claim()
    {
        var logger = Substitute.For<ILogger>();
        var factory = CreateContextFactory();

        var command = PipelineFactory
            .Start<string>()
            .PipeIfClaim( new Claim( "Role", "reader" ), b => b.Pipe( Complex ) )
            .Build();

        var result = await command( factory.Create( logger ), "reader" );

        Assert.AreEqual( 6, result );
    }

    [TestMethod]
    public async Task Should_return_claim_with_auth()
    {
        var logger = Substitute.For<ILogger>();
        var factory = CreateContextFactory();

        var command = PipelineFactory
            .Start<string>()
            .WithAuth()
            .Build();

        var result = await command( factory.Create( logger ), "reader" );

        Assert.AreEqual( "reader", result );
    }

    [TestMethod]
    public async Task Should_withAuth_does_not_cancel()
    {
        var logger = Substitute.For<ILogger>();
        var factory = CreateContextFactory();

        var command = PipelineFactory
        .Start<string>()
            .WithAuth( ( context, argument, claimsPrincipal ) =>
            {
                return claimsPrincipal.HasClaim( x => x.Value == argument );
            } )
            .Pipe( Complex )
            .Build();

        var context = factory.Create( logger );
        var result = await command( context, "reader" );

        Assert.AreEqual( 6, result );
        Assert.IsTrue( context.Success );
    }

    [TestMethod]
    public async Task Should_withAuth_cancels_pipeline()
    {
        var logger = Substitute.For<ILogger>();
        var factory = CreateContextFactory();

        var command = PipelineFactory
            .Start<string>()
            .WithAuth( ( context, argument, claimsPrincipal ) =>
            {
                return claimsPrincipal.HasClaim( x => x.Value == argument );
            } )
            .Pipe( Complex )
            .Build();

        var context = factory.Create( logger );
        var result = await command( context, "test" );

        Assert.AreEqual( 0, result );
        Assert.IsTrue( context.IsCanceled );
    }


    private static int Complex( IPipelineContext context, string argument ) => argument.Length;

    private static IPipelineContextFactory CreateContextFactory()
    {
        var claimsPrincipal = ClaimsPrincipalFixture.Next();

        var container = new ServiceContainer();

        container.AddService( typeof( IClaimsPrincipalAccessor ), new TestClaimsPrincipalAccessor( claimsPrincipal ) );

        return PipelineContextFactory.CreateFactory( container, true );
    }

    private class TestClaimsPrincipalAccessor( ClaimsPrincipal claimsPrincipal ) : IClaimsPrincipalAccessor
    {
        public ClaimsPrincipal ClaimsPrincipal => claimsPrincipal;
    }
}
