using System.Threading;
using System.Threading.Tasks;

namespace TinyMediator;

/// <summary>
/// An interface for implementing Mediator messages that return a typed response.
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public interface IRequestHandler<TRequest, TResponse>
{
    /// <summary>
    /// Handle the Mediator message with a typed response.
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// An interface for implementiong Mediator messages that return no response (fire-and-forget).
/// </summary>
/// <typeparam name="TRequest"></typeparam>
public interface IRequestHandler<TRequest>
{
    /// <summary>
    /// Handle the Mediator request with no response (fire-and-forget).
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task Handle(TRequest request, CancellationToken cancellationToken = default);
}