using System;

namespace TinyMediator;

/// <summary>
/// Sets an explicit lifetime for an <see cref="IRequestHandler{TRequest}"/> or <see cref="IRequestHandler{TRequest,TResponse}"/>.
/// Default is <see cref="ServiceLifetime.Scoped"/>.
/// </summary>
/// <param name="lifetime"></param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class HandlerLifetimeAttribute(ServiceLifetime lifetime) : Attribute
{
    /// <summary>
    /// The service lifetime of the TinyMediator handler.
    /// </summary>
    public ServiceLifetime Lifetime { get; } = lifetime;
}