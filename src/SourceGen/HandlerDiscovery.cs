using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TinyMediator.SourceGen;

/// <summary>
/// Discovers every type implementing <c>IRequestHandler&lt;TRequest&gt;</c> or
/// <c>IRequestHandler&lt;TRequest, TResponse&gt;</c> visible to a compilation, including types
/// compiled into referenced assemblies.
/// </summary>
internal static class HandlerDiscovery
{
    internal readonly struct HandlerRegistration
    {
        public HandlerRegistration(ITypeSymbol requestType, ITypeSymbol? responseType,
            INamedTypeSymbol handlerType, AttributeData? lifetimeAttribute)
        {
            RequestType = requestType;
            ResponseType = responseType;
            HandlerType = handlerType;
            LifetimeAttribute = lifetimeAttribute;
        }

        public ITypeSymbol RequestType { get; }

        /// <summary>Null for the fire-and-forget IRequestHandler&lt;TRequest&gt; shape.</summary>
        public ITypeSymbol? ResponseType { get; }

        public INamedTypeSymbol HandlerType { get; }
        public AttributeData? LifetimeAttribute { get; }
    }

    /// <summary>
    /// Returns every handler registration visible to <paramref name="compilation"/>, or null if
    /// TinyMediator's core types aren't referenced by it at all.
    /// </summary>
    internal static IReadOnlyList<HandlerRegistration>? Find(Compilation compilation)
    {
        var handlerInterface1 = compilation.GetTypeByMetadataName("TinyMediator.IRequestHandler`1");
        var handlerInterface2 = compilation.GetTypeByMetadataName("TinyMediator.IRequestHandler`2");
        if (handlerInterface1 is null || handlerInterface2 is null) return null;

        var results = new List<HandlerRegistration>();

        foreach (var type in GetCandidateTypes(compilation, handlerInterface1.ContainingAssembly))
        {
            if (type.IsAbstract) continue;

            var lifetimeAttr = type.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.Name == "HandlerLifetimeAttribute");

            foreach (var iface in type.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, handlerInterface2))
                {
                    results.Add(new HandlerRegistration(
                        iface.TypeArguments[0], iface.TypeArguments[1], type, lifetimeAttr));
                }
                else if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, handlerInterface1))
                {
                    results.Add(new HandlerRegistration(
                        iface.TypeArguments[0], null, type, lifetimeAttr));
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Walks the current assembly plus every referenced assembly that could plausibly declare a
    /// handler - i.e. every assembly that itself references the assembly declaring
    /// IRequestHandler&lt;&gt;. Any type implementing the interface must have a direct reference to
    /// it, so this skips the BCL and unrelated NuGet packages without missing real handlers.
    /// </summary>
    private static IEnumerable<INamedTypeSymbol> GetCandidateTypes(Compilation compilation, IAssemblySymbol coreAssembly)
    {
        // hashset handles dedupe
        var seen = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        var assemblies = new List<IAssemblySymbol>();

        void AddAssembly(IAssemblySymbol asm)
        {
            if (seen.Add(asm)) assemblies.Add(asm);
        }

        AddAssembly(compilation.Assembly);

        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol asm &&
                (SymbolEqualityComparer.Default.Equals(asm, coreAssembly) ||
                 asm.Modules.Any(m => m.ReferencedAssemblySymbols.Contains(coreAssembly, SymbolEqualityComparer.Default))))
            {
                AddAssembly(asm);
            }
        }

        return assemblies.SelectMany(a => Walk(a.GlobalNamespace));

        static IEnumerable<INamedTypeSymbol> Walk(INamespaceSymbol ns)
        {
            foreach (var member in ns.GetMembers())
            {
                switch (member)
                {
                    case INamespaceSymbol nested:
                        foreach (var t in Walk(nested)) yield return t;
                        break;
                    case INamedTypeSymbol type:
                        yield return type;
                        foreach (var nestedType in type.GetTypeMembers()) yield return nestedType;
                        break;
                }
            }
        }
    }
}