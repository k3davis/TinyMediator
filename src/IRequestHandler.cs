using System.Threading;
using System.Threading.Tasks;

namespace TinyMediator;

public interface IRequestHandler<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}