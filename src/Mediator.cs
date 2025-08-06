using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TinyMediator;

public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
    {
        IRequestHandler<TRequest, TResponse> handler = serviceProvider
            .GetRequiredService<IRequestHandler<TRequest, TResponse>>();

        return await handler.Handle(request, cancellationToken);
    }
}