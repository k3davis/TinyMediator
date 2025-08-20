using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace TinyMediator.SourceGen;

[Generator]
public class MediatorRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax,
                transform: static (ctx, _) => (ClassDeclarationSyntax)ctx.Node)
            .Where(static cds => cds != null);

        var combined = context.CompilationProvider.Combine(classDeclarations.Collect());

        context.RegisterSourceOutput(combined, (spc, data) =>
        {
            int registrationCount = 0;
            var (compilation, classes) = data;

            // handler registration method
            StringBuilder sb = new();

            foreach (var classDecl in classes)
            {
                var model = compilation.GetSemanticModel(classDecl.SyntaxTree);
                if (model.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol symbol || symbol.IsAbstract) continue;

                var handlerInterface1 = compilation.GetTypeByMetadataName("TinyMediator.IRequestHandler`1");
                var handlerInterface2 = compilation.GetTypeByMetadataName("TinyMediator.IRequestHandler`2");
                if (handlerInterface1 == null && handlerInterface2 == null) return;

                bool implementsHandler = symbol.Interfaces.Any(i =>
                    SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, handlerInterface1) ||
                    SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, handlerInterface2));

                if (implementsHandler)
                {
                    foreach (var iface in symbol.Interfaces)
                    {
                        var requestType = iface.TypeArguments[0].ToDisplayString();
                        var handlerType = symbol.ToDisplayString();

                        // detect lifetime attribute
                        var lifetimeAttr = symbol.GetAttributes()
                            .FirstOrDefault(a => a.AttributeClass?.Name == "HandlerLifetimeAttribute");

                        string lifetime = ExtractLifetimeFromAttribute(lifetimeAttr);

                        var registrationMethod = lifetime switch
                        {
                            "Singleton" => "AddSingleton",
                            "Transient" => "AddTransient",
                            _ => "AddScoped"
                        };

                        if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, handlerInterface1))
                        {
                            sb.AppendLine($"        services.{registrationMethod}<IRequestHandler<{requestType}>, {handlerType}>();");
                            registrationCount++;
                        }
                        else if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, handlerInterface2))
                        {
                            var responseType = iface.TypeArguments[1].ToDisplayString();

                            sb.AppendLine($"        services.{registrationMethod}<IRequestHandler<{requestType}, {responseType}>, {handlerType}>();");
                            registrationCount++;
                        }
                    }
                }
            }

            Diagnostic report;
            if (registrationCount == 0)
            {
                report = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "TM002",
                        title: "No Handlers Registered",
                        messageFormat: "No handler implementations were found to be registered.",
                        category: "TinyMediator",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    Location.None);
            }
            else
            {
                // vs seems to suppress this..?
                report = Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "TM998",
                        title: "Handlers Registered",
                        messageFormat: $"{registrationCount} handler implementations were registered.",
                        category: "TinyMediator",
                        DiagnosticSeverity.Info,
                        isEnabledByDefault: true),
                    Location.None);
            }
            spc.ReportDiagnostic(report);

            string handlers = sb.ToString();

            string fullSource = $$"""
#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace TinyMediator;

public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers all implementations of <see cref="IRequestHandler{TRequest,TResponse}"/> or
    /// <see cref="IRequestHandler{TRequest}"/> 
    /// found in the current application domain as services in the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the handlers to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <remarks>
    /// This method is intended for use with TinyMediator and supports automatic discovery
    /// and registration of handler types. All public classes implementing <c>IRequestHandler&lt;,&gt;</c>
    /// will be registered with the configured lifetime (scoped by default).
    /// </remarks>
    public static IServiceCollection AddTinyMediatorHandlers(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
{{handlers}}
        return services;
    }
}
""";

            spc.AddSource("MediatorServiceCollectionExtensions.g.cs", SourceText.From(fullSource, Encoding.UTF8));
        });
    }

    static string ExtractLifetimeFromAttribute(AttributeData? attr)
    {
        if (attr is null || attr.ConstructorArguments.Length == 0)
            return "Scoped";

        var arg = attr.ConstructorArguments[0];
        if (arg.Value is int enumValue)
            return ((ServiceLifetime)enumValue).ToString();

        return "Scoped";
    }
}