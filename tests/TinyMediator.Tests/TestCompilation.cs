using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TinyMediator.Tests
{
    internal static class TestCompilation
    {
        public static CSharpCompilation Create(string source, string assemblyName = "TestAssembly")
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = Net100.References.All
                .Add(MetadataReference.CreateFromFile(typeof(IRequestHandler<,>).Assembly.Location));

            return CSharpCompilation.Create(
                assemblyName,
                [syntaxTree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
        }
    }
}