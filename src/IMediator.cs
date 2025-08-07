using System.Threading;
using System.Threading.Tasks;

namespace TinyMediator;

/// <summary>
/// A basic abstraction of the Mediator pattern.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Send the request to the mediator with no response (fire-and-forget).
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send the request to the mediator with a typed response.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default);
}