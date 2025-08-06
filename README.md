# TinyMediator

A lightweight implementation of the Mediator pattern — with built-in source generation for automatic handler registration.
Designed for minimal boilerplate and fast compile-time discovery.

## Installation

via Nuget:

```
Install-Package TinyMediator
```
This will register both the mediator classes and the source-generating analyzer.

## Basic Usage
1. Define a Request and Handler

```csharp
public class Ping : IRequest<Pong> { }

public class PingHandler : IRequestHandler<Ping, Pong>
{
    public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        => Task.FromResult(new Pong());
}
```
2. Register Handlers Automatically

Add this line (or equivalent) to your `Program.cs` and all of your `IMediator` implementations
will be automatically registered from the generated source at compile-time.

```csharp
services.AddTinyMediatorHandlers();
```
3. Send Requests

```csharp
var response = await mediator.Send(new Ping());
```

## What's Inside
| Component                  | Description                                         |
|----------------------------|-----------------------------------------------------|
| `IRequest<T>` / `INotification` | Core abstractions for Mediator pattern         |
| `IMediator`                | Interface for sending requests and publishing notifications |
| Source Generator           | Auto-discovers and registers handlers at compile time |
| `AddMediatorHandlers()`    | Partial method generated for DI registration        |

## Contributions
There are many, more robust libraries out there that implement the mediator pattern. I made this library for the simplest use case and wanted source generation to automate registrations without using reflection. If this is generally useful to others I might be surprised, but any suggestions or contributions are welcome.

## License
MIT - Use, modify, and distribute it freely in open source and commercial projects.