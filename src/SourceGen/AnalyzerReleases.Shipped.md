; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.1.4

### New Rules

Rule ID | Category     | Severity | Notes
--------|--------------|----------|-------------------------------------------------------------
TM003   | TinyMediator | Error    | Send<TRequest> called but the handler returns a response.
TM004   | TinyMediator | Error    | Send<TRequest, TResponse> called but the handler returns nothing.
TM005   | TinyMediator | Error    | Send<TRequest, TResponse> called with a response type the handler doesn't return.
