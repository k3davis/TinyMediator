using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TinyMediator;

/// <inheritdoc />
public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    /// <inheritdoc />
    public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        IRequestHandler<TRequest, TResponse> handler = serviceProvider
            .GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        return await handler.Handle(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
    {
        IRequestHandler<TRequest> handler = serviceProvider
            .GetRequiredService<IRequestHandler<TRequest>>();

        await handler.Handle(request, cancellationToken);
    }
}