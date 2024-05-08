using System.Security.Claims;

namespace Hyperbee.Pipeline.Auth.Tests.TestSupport;

public static class ClaimsPrincipalFixture
{
    public static ClaimsPrincipal Next( ClaimsPrincipal? principal = null )
    {
        ClaimsIdentity claimsIdentity = new ClaimsIdentity();
        claimsIdentity.AddClaim( new Claim( "Name", "test@comcast.net" ) );
        claimsIdentity.AddClaim( new Claim( "Role", "reader" ) );

        return new ClaimsPrincipal( new ClaimsIdentity( claimsIdentity ) );
    }
}
