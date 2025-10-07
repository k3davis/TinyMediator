# TinyMediator

[![NuGet version (TinyMediator)](https://img.shields.io/nuget/v/TinyMediator.svg?style=flat-square)](https://www.nuget.org/packages/TinyMediator/)

A lightweight implementation of the Mediator pattern—with built-in source generation for automatic handler registration. Designed for minimal boilerplate and fast compile-time discovery.

## Installation

via Nuget:

```
Install-Package TinyMediator
```
This will register both the mediator classes and the source-generating analyzer.

## Features

This library is intended to be clean and purpose-built, without fluff. But it does include:

* ✅ A mimimal mediator abstraction, supporting "fire-and-forget" and typed responses
* ✅ Source-generated DI registration (no reflection)
* ✅ Configurable handler lifetimes (scoped by default)
* ✅ No unnecessary marker interfaces

## Basic Usage
1. Define a Request and Handler

```csharp
public class Ping { }

public class PingHandler : IRequestHandler<Ping, Pong>
{
    public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        => Task.FromResult(new Pong());
}
```
2. Register Handlers Automatically

Add this line (or equivalent) to your `Program.cs` and all of your `IRequestHandler` implementations
will be automatically registered from the generated source at compile time.

```csharp
services.AddTinyMediatorHandlers();
```
3. Send Requests

```csharp
var response = await mediator.Send(new Ping());
```

## Handler Lifetimes
By default, handlers are registered with a scoped lifetime for easy use of scoped services like HttpClients and DbContexts. If you want your handler to have a different lifetime, you can declare that:

```csharp
[HandlerLifetime(TinyMediator.ServiceLifetime.Singleton)]
public class PingHandler : IRequestHandler<Ping, Pong>
{
    public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        => Task.FromResult(new Pong());
}
```

## What's Inside
| Component                  | Description                                         |
|----------------------------|-----------------------------------------------------|
| `IRequestHandler<TRequest>` | Mediator pattern "fire and forget" abstraction         |
| `IRequestHandler<TRequest, TResponse>` | Mediator pattern abstraction with typed response         |
| Source Generator           | Auto-discovers and registers handlers at compile time |
| `AddTinyMediatorHandlers()`    | Method generated for DI registration        |

## Contributions
There are many robust (and complicated, or commercial) libraries out there that implement the Mediator pattern. I made this library for the simplest use cases and wanted source generation to automate registrations without using reflection. If this is generally useful to others I might be surprised, but any suggestions or contributions are welcome.

## License
MIT - Use, modify, and distribute it freely in open source and commercial projects.
