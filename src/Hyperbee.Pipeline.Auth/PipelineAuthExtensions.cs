using System.Security.Claims;
using Hyperbee.Pipeline.Context;
using Microsoft.Extensions.DependencyInjection;

namespace Hyperbee.Pipeline.Auth;

public static class PipelineAuthExtensions
{
    private const string ClaimsPrincipalKey = nameof( ClaimsPrincipalKey );

    public static IPipelineBuilder<TInput, TNext> PipeIfClaim<TInput, TOutput, TNext>(
        this IPipelineBuilder<TInput, TOutput> builder,
        Claim claim,
        Func<IPipelineStartBuilder<TOutput, TOutput>, IPipelineBuilder<TOutput, TNext>> childBuilder )
    {
        return (IPipelineBuilder<TInput, TNext>) builder
            .PipeIf( ( context, arg ) =>
                {
                    var claimsPrincipal = context.GetClaimsPrincipal();

                    return claimsPrincipal.HasClaim( x => x.Type == claim.Type && x.Value == claim.Value );
                }
                , childBuilder );

    }

    public static IPipelineBuilder<TInput, TOutput> WithAuth<TInput, TOutput>(
        this IPipelineStartBuilder<TInput, TOutput> builder,
        Func<IPipelineContext, TOutput, ClaimsPrincipal, bool> validateClaims )
    {
        return builder
            .WithAuth()
            .Call( ( context, argument ) =>
            {
                var claimsPrincipal = context.GetClaimsPrincipal();

                if ( !validateClaims( context, argument, claimsPrincipal ) )
                    context.CancelAfter();

            } );
    }

    public static IPipelineBuilder<TInput, TOutput> WithAuth<TInput, TOutput>(
        this IPipelineStartBuilder<TInput, TOutput> builder )
    {
        return builder.HookAsync( async ( context, argument, next ) =>
        {
            context.GetClaimsPrincipal();

            return await next( context, argument );

        } );
    }

    public static ClaimsPrincipal GetClaimsPrincipal( this IPipelineContext context )
    {
        if ( context.Items.TryGetValue<ClaimsPrincipal>( ClaimsPrincipalKey, out var claimsPrincipal ) )
            return claimsPrincipal;

        var claimsPrincipalAccessor = context.ServiceProvider.GetService<IClaimsPrincipalAccessor>();
        claimsPrincipal = claimsPrincipalAccessor.ClaimsPrincipal;

        context.Items.SetValue( ClaimsPrincipalKey, claimsPrincipal );
        return claimsPrincipal;
    }


}
