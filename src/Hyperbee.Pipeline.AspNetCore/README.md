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

### Transform Results Before Returning

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

### Custom Result Mapping

Use a `resultMapper` to handle specific exceptions or customize both error and success responses.
The mapper receives the full `CommandResult` and returns an `IResult`, or `null` to fall through
to default handling.

```csharp
app.MapPost( "/items", async (
    CreateItemRequest request,
    ICommandFunction<CreateItemRequest, Item> command ) =>
{
    var commandResult = await command.ExecuteAsync( request );
    return commandResult.ToResult( cr =>
    {
        if ( cr.Context.IsError && cr.Context.Exception is DbUpdateConcurrencyException )
            return Results.Conflict( "Version conflict. Reload and retry." );
        return null; // fall through to default handling
    } );
} );
```

### Cross-Cutting Result Mapping with IResultMapper

For shared result mapping logic, implement the `IResultMapper` interface and register it with DI.
This is useful when multiple endpoints need the same exception-to-HTTP-status mapping.

```csharp
public class ConflictResultMapper : IResultMapper
{
    public IResult? Map( CommandResult commandResult )
    {
        if ( commandResult.Context.IsError &&
             commandResult.Context.Exception is DbUpdateConcurrencyException )
        {
            return Results.Conflict( "Version conflict. Reload and retry." );
        }

        return null; // fall through to default handling
    }
}
```

Register with DI and inject into endpoints:

```csharp
services.AddSingleton<IResultMapper, ConflictResultMapper>();

app.MapPost( "/items", async (
    CreateItemRequest request,
    ICommandFunction<CreateItemRequest, Item> command,
    IResultMapper resultMapper ) =>
{
    var commandResult = await command.ExecuteAsync( request );
    return commandResult.ToResult( resultMapper );
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
| `ApplicationValidationFailure` | 400 Bad Request |
| `ValidationFailure` | 400 Bad Request |
