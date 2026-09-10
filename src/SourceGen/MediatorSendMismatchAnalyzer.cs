using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TinyMediator.SourceGen;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class MediatorSendMismatchAnalyzer : DiagnosticAnalyzer
{
    public static readonly DiagnosticDescriptor MismatchRule = new(
        id: "TM003",
        title: "Fire-and-forget Send used on a handler with a response",
        messageFormat: "'{0}' is handled by '{1}', which implements IRequestHandler<{0}, {2}>. " +
                        "Use Send<{0}, {2}>(...) instead of Send<{0}>(...) to receive the '{2}' response.",
        category: "TinyMediator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "IMediator.Send<TRequest> discards any response. Calling it against a request whose " +
                      "handler implements IRequestHandler<TRequest, TResponse> silently drops the result " +
                      "and throws a DI resolution error at runtime instead of returning it at compile time.");

    public static readonly DiagnosticDescriptor NoResponseRule = new(
        id: "TM004",
        title: "Send called with a response type but the handler returns none",
        messageFormat: "'{0}' is handled by '{1}', which implements IRequestHandler<{0}> with no response. " +
                        "Use Send<{0}>(...) instead of Send<{0}, {2}>(...).",
        category: "TinyMediator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "IMediator.Send<TRequest, TResponse> requires a handler that returns TResponse. " +
                      "Calling it against a request whose handler implements the fire-and-forget " +
                      "IRequestHandler<TRequest> instead has no matching registration and throws a DI " +
                      "resolution error at runtime.");

    public static readonly DiagnosticDescriptor WrongResponseTypeRule = new(
        id: "TM005",
        title: "Send called with a response type that doesn't match the handler",
        messageFormat: "'{0}' is handled by '{1}', which implements IRequestHandler<{0}, {2}>, not " +
                        "IRequestHandler<{0}, {3}>. Use Send<{0}, {2}>(...) instead.",
        category: "TinyMediator",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The TResponse passed to IMediator.Send<TRequest, TResponse> must match the response " +
                      "type the request's handler actually implements, or there's no matching DI " +
                      "registration and the call throws at runtime instead of at compile time.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [MismatchRule, NoResponseRule, WrongResponseTypeRule];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var compilation = context.Compilation;

        var mediatorInterface = compilation.GetTypeByMetadataName("TinyMediator.IMediator");
        var registrations = HandlerDiscovery.Find(compilation);
        if (mediatorInterface is null || registrations is null) return;

        var twoArgHandlers = new Dictionary<ITypeSymbol, (INamedTypeSymbol HandlerType, ITypeSymbol ResponseType)>(
            SymbolEqualityComparer.Default);
        var oneArgHandlers = new Dictionary<ITypeSymbol, INamedTypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var r in registrations)
        {
            if (r.ResponseType is not null)
                twoArgHandlers[r.RequestType] = (r.HandlerType, r.ResponseType);
            else
                oneArgHandlers[r.RequestType] = r.HandlerType;
        }

        context.RegisterOperationAction(operationContext =>
        {
            var invocation = (IInvocationOperation)operationContext.Operation;
            var method = invocation.TargetMethod;

            if (method.Name != "Send") return;

            var containingType = method.ContainingType?.OriginalDefinition;
            var declaresOnMediator =
                SymbolEqualityComparer.Default.Equals(containingType, mediatorInterface) ||
                method.ContainingType?.AllInterfaces.Any(i =>
                    SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, mediatorInterface)) == true;

            if (!declaresOnMediator) return;

            switch (method.TypeArguments.Length)
            {
                case 1:
                    CheckFireAndForgetCall(operationContext, invocation, method.TypeArguments[0], twoArgHandlers);
                    break;
                case 2:
                    CheckTypedResponseCall(operationContext, invocation, method.TypeArguments[0],
                        method.TypeArguments[1], twoArgHandlers, oneArgHandlers);
                    break;
            }
        }, OperationKind.Invocation);
    }

    private static void CheckFireAndForgetCall(
        OperationAnalysisContext operationContext, IInvocationOperation invocation, ITypeSymbol requestType,
        Dictionary<ITypeSymbol, (INamedTypeSymbol HandlerType, ITypeSymbol ResponseType)> twoArgHandlers)
    {
        if (twoArgHandlers.TryGetValue(requestType, out var match))
        {
            operationContext.ReportDiagnostic(Diagnostic.Create(
                MismatchRule,
                invocation.Syntax.GetLocation(),
                requestType.ToDisplayString(),
                match.HandlerType.ToDisplayString(),
                match.ResponseType.ToDisplayString()));
        }
    }

    private static void CheckTypedResponseCall(
        OperationAnalysisContext operationContext, IInvocationOperation invocation,
        ITypeSymbol requestType, ITypeSymbol requestedResponseType,
        Dictionary<ITypeSymbol, (INamedTypeSymbol HandlerType, ITypeSymbol ResponseType)> twoArgHandlers,
        Dictionary<ITypeSymbol, INamedTypeSymbol> oneArgHandlers)
    {
        if (twoArgHandlers.TryGetValue(requestType, out var match))
        {
            if (!SymbolEqualityComparer.Default.Equals(match.ResponseType, requestedResponseType))
            {
                operationContext.ReportDiagnostic(Diagnostic.Create(
                    WrongResponseTypeRule,
                    invocation.Syntax.GetLocation(),
                    requestType.ToDisplayString(),
                    match.HandlerType.ToDisplayString(),
                    match.ResponseType.ToDisplayString(),
                    requestedResponseType.ToDisplayString()));
            }
            return;
        }

        if (oneArgHandlers.TryGetValue(requestType, out var handlerType))
        {
            operationContext.ReportDiagnostic(Diagnostic.Create(
                NoResponseRule,
                invocation.Syntax.GetLocation(),
                requestType.ToDisplayString(),
                handlerType.ToDisplayString(),
                requestedResponseType.ToDisplayString()));
        }
    }
}