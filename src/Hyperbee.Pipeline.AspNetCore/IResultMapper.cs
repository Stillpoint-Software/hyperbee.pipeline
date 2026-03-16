using Hyperbee.Pipeline.Commands;
using Microsoft.AspNetCore.Http;

namespace Hyperbee.Pipeline.AspNetCore;

/// <summary>
/// Maps a <see cref="CommandResult"/> to an <see cref="IResult"/> for cross-cutting error handling.
/// </summary>
/// <remarks>
/// <para>
/// Register implementations in the DI container and inject into endpoints to apply shared
/// result mapping logic (e.g., converting specific exceptions to HTTP status codes).
/// </para>
/// <para>
/// The mapper runs before default error handling. Return an <see cref="IResult"/> to handle the
/// command result, or <see langword="null"/> to fall through to the default handling logic.
/// </para>
/// </remarks>
public interface IResultMapper
{
    /// <summary>
    /// Attempts to map the given command result to an <see cref="IResult"/>.
    /// </summary>
    /// <param name="commandResult">The command result to inspect.</param>
    /// <returns>
    /// An <see cref="IResult"/> if this mapper handles the command result;
    /// otherwise, <see langword="null"/> to delegate to default handling.
    /// </returns>
    IResult? Map( CommandResult commandResult );
}
