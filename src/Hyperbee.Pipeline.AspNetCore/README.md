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
