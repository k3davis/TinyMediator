namespace TinyMediator;

/// <summary>
/// Specifies the lifetime of a TinyMediator handler in a dependency injection container.
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// A new instance is created for each scope. Typically used for request-based lifetimes.
    /// </summary>
    Scoped,

    /// <summary>
    /// A single instance is created and shared throughout the application's lifetime.
    /// </summary>
    Singleton,

    /// <summary>
    /// A new instance is created every time the service is requested.
    /// </summary>
    Transient
}