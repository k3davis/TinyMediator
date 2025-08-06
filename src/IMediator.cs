using System.Threading;
using System.Threading.Tasks;

namespace TinyMediator;

/// <summary>
/// Represents an implementation of a minimal mediator pattern using TinyMediator.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Send the request to the mediator.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default);
}