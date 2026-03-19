# Hyperbee.Pipeline.AspNetCore

The `Hyperbee.Pipeline.AspNetCore` library provides extensions that map `CommandResult` to ASP.NET Core
HTTP responses using `IResult`.

## Examples

### Map Command Results to HTTP Responses

```csharp
app.MapPost( "/items", async (
    CreateItemRequest request,
    ICommandFunction<CreateItemRequest, Item> command ) =>
{
    var commandResult = await command.ExecuteAsync( request );
    return commandResult.ToResult();
} );
```

### Transform Results with a Content Selector

```csharp
app.MapGet( "/items/{id}", async (
    Guid id,
    ICommandFunction<Guid, Item> command ) =>
{
    var commandResult = await command.ExecuteAsync( id );
    return commandResult.ToResult( item => new { item.Id, item.Name } );
} );
```

### File Results

```csharp
app.MapGet( "/reports/{id}", async (
    string id,
    ICommandFunction<string, Stream> command ) =>
{
    var commandResult = await command.ExecuteAsync( id );
    return commandResult.ToFileResult( "application/pdf" );
} );
```

### Custom Result Mapping with ResultMapper

Subclass `ResultMapper` to customize error handling, status codes, and success responses.
Override only the methods you need.

```csharp
public class BillingResultMapper : ResultMapper
{
    public override IResult? MapException( Exception exception ) => exception switch
    {
        WriteConflictRepositoryException => Results.Conflict( "Version mismatch. Reload and retry." ),
        CommandException { InnerException: WriteConflictRepositoryException }
            => Results.Conflict( "Version mismatch. Reload and retry." ),
        _ => base.MapException( exception )
    };
}
```

Register with DI and inject into endpoints:

```csharp
services.AddSingleton<ResultMapper, BillingResultMapper>();

app.MapPost( "/items", async (
    CreateItemRequest request,
    ICommandFunction<CreateItemRequest, Item> command,
    ResultMapper mapper ) =>
{
    var commandResult = await command.ExecuteAsync( request );
    return commandResult.ToResult( mapper );
} );
```

### One-Off Mapper with Lambdas

Use `ResultMapper.Create()` for one-off customizations without subclassing:

```csharp
var mapper = ResultMapper.Create(
    mapException: ex => ex is DbUpdateConcurrencyException
        ? Results.Conflict( "Version conflict." )
        : null
);

return commandResult.ToResult( x => x?.ToPresentation(), mapper );
```

### Response Decorators

Chain `.WithHeader()` and `.WithNoContent()` to customize successful responses:

```csharp
// Add an ETag header
return commandResult.ToResult( x => x?.ToPresentation(), mapper )
    .WithHeader( "ETag", commandResult.Result?.VersionTag );

// Return 204 No Content for write operations
return commandResult.ToResult( mapper ).WithNoContent();
```

### Inline Result Mapping

Use a `Func<CommandResult<T>, IResult?>` for one-off custom handling.
Return `null` to fall through to default handling.

```csharp
var commandResult = await command.ExecuteAsync( request );
return commandResult.ToResult( cr =>
{
    if ( cr.Context.IsError && cr.Context.Exception is DbUpdateConcurrencyException )
        return Results.Conflict( "Version conflict. Reload and retry." );
    return null;
} );
```

### Automatic Error Mapping

Validation failures are automatically converted to appropriate HTTP status codes with
RFC 7807 Problem Details responses:

| Failure Type | HTTP Status |
| --- | --- |
| `NotFoundValidationFailure` | 404 Not Found |
| `UnauthorizedValidationFailure` | 401 Unauthorized |
| `ForbiddenValidationFailure` | 403 Forbidden |
| `ApplicationValidationFailure` | 422 Unprocessable Entity |
| `ValidationFailure` | 422 Unprocessable Entity |

Override `GetStatusCode` on your `ResultMapper` subclass to customize these mappings.
