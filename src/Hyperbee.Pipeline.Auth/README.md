# Hyperbee.Pipeline.Claims

The `Hyperbee.Pipeline.Auth` library is a set of extentsions to `Hyperbee.Pipeline` that adds support for authorization within the pipeline.


## Examples

```csharp
// Will return the claim if available
var command = PipelineFactory
    .Start<string>()
    .PipeIfClaim( Claim )
    .Build();

```

```csharp
// WithAuth takes a function to validate the claim. 
var command = PipelineFactory
    .Start<string>()
    .WithAuth( ValidateClaim )
    .Build();

private async Task<bool> ValidateClaim( IPipelineContext context, string roleValue, ClaimsPrincipal claimsPrincipal )
    {
        return claimsPrincipal.HasClaim( x => x.Value == roleValue );
    }
```

## Dependacy Injection

```csharp
// Add httpContextAccessor if using web api
 services.AddHttpContextAccessor();

// Add with the pipelines
services.AddClaimPrincipalAccessor();
```

Or create your own claims principal use for the pipelines:

```csharp
services.AddPipeline( (factoryServices, rootProvider) =>
{
    factoryServices.AddClaimPrincipalAccessor( IClaimsPrincipal claimsPrincipal )
} );
```

