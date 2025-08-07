using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TinyMediator.SourceGen;

namespace TinyMediator.Tests
{
    public static class GeneratorTestHelper
    {
        public static Diagnostic[] GetDiagnostics(string source)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IRequestHandler<,>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location),
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                [syntaxTree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            var generator = new MediatorRegistrationGenerator(); // your actual source generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

            return [.. diagnostics];
        }
    }

}
