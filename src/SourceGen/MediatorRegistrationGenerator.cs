using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace TinyMediator.SourceGen;

[Generator]
public class MediatorRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.CompilationProvider, static (spc, compilation) =>
        {
            var registrations = HandlerDiscovery.Find(compilation);
            if (registrations is null) return; // TinyMediator core types aren't referenced by this compilation

            var sb = new StringBuilder();

            foreach (var reg in registrations)
            {
                var lifetime = ExtractLifetimeFromAttribute(reg.LifetimeAttribute);
                var registrationMethod = lifetime switch
                {
                    "Singleton" => "AddSingleton",
                    "Transient" => "AddTransient",
                    _ => "AddScoped"
                };

                var requestType = reg.RequestType.ToDisplayString();
                var handlerType = reg.HandlerType.ToDisplayString();

                sb.AppendLine(reg.ResponseType is null
                    ? $"        services.{registrationMethod}<IRequestHandler<{requestType}>, {handlerType}>();"
                    : $"        services.{registrationMethod}<IRequestHandler<{requestType}, {reg.ResponseType.ToDisplayString()}>, {handlerType}>();");
            }

            Diagnostic report = registrations.Count == 0
                ? Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "TM002",
                        title: "No Handlers Registered",
                        messageFormat: "No handler implementations were found to be registered.",
                        category: "TinyMediator",
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault: true),
                    Location.None)
                : Diagnostic.Create(
                    new DiagnosticDescriptor(
                        id: "TM998",
                        title: "Handlers Registered",
                        messageFormat: $"{registrations.Count} handler implementations were registered.",
                        category: "TinyMediator",
                        DiagnosticSeverity.Info,
                        isEnabledByDefault: true),
                    Location.None);

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
    /// found in the current compilation and its referenced assemblies as services in the
    /// dependency injection container.
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
        // this is transient to handle implementations of any lifetime
        services.AddTransient<IMediator, Mediator>();
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