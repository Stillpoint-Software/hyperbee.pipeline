using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Hyperbee.Pipeline.Auth;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddClaimPrincipalAccessor<T>(
        this IServiceCollection services,
        T claimsPrincipalAccessor
    ) where T : IClaimsPrincipalAccessor
    {
        return services.AddTransient<IClaimsPrincipalAccessor>( _ => claimsPrincipalAccessor );

    }

    //For web API
    public static IServiceCollection AddClaimPrincipalAccessor(
        this IServiceCollection services
    )
    {
        return services.AddTransient<IClaimsPrincipalAccessor, HttpClaimsAccessor>();
    }
}

public interface IClaimsPrincipalAccessor
{
    public ClaimsPrincipal ClaimsPrincipal { get; }
}

public class HttpClaimsAccessor : IClaimsPrincipalAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    public HttpClaimsAccessor( IHttpContextAccessor httpContextAccessor )
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal ClaimsPrincipal
    {
        get { return _httpContextAccessor.HttpContext.User; }
    }
}



