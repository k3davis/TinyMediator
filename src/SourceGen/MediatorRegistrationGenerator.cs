using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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

                var handlerInterface = compilation.GetTypeByMetadataName("TinyMediator.IRequestHandler`2");
                if (handlerInterface == null) return;

                foreach (var iface in symbol.Interfaces)
                {
                    if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, handlerInterface))
                    {
                        var requestType = iface.TypeArguments[0].ToDisplayString();
                        var responseType = iface.TypeArguments[1].ToDisplayString();
                        var handlerType = symbol.ToDisplayString();

                        sb.AppendLine($"        services.AddScoped<IRequestHandler<{requestType}, {responseType}>, {handlerType}>();");
                        registrationCount++;
                    }
                }
            }

            string handlers = sb.ToString();

            string fullSource = $$"""
#nullable enable
using Microsoft.Extensions.DependencyInjection;

namespace TinyMediator;

public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    /// Register all <see cref="IMediator"/> implementations as scoped services.
    /// </summary>
    /// <param name="services"></param>
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
}