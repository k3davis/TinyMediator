using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TinyMediator.SourceGen;

namespace TinyMediator.Tests
{
    public static class GeneratorTestHelper
    {
        public static Diagnostic[] GetDiagnostics(string source)
        {
            var compilation = TestCompilation.Create(source);

            var generator = new MediatorRegistrationGenerator(); // your actual source generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

            return [.. diagnostics];
        }
    }
}