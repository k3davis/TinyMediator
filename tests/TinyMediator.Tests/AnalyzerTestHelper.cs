using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TinyMediator.SourceGen;

namespace TinyMediator.Tests
{
    public static class AnalyzerTestHelper
    {
        public static async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(string source)
        {
            var compilation = TestCompilation.Create(source);

            // Fail loudly on a source that doesn't even compile, rather than silently reporting
            // zero analyzer diagnostics because the compilation was already broken.
            var compileErrors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToImmutableArray();

            if (!compileErrors.IsEmpty)
            {
                throw new InvalidOperationException(
                    "Test source failed to compile:\n" + string.Join("\n", compileErrors));
            }

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new MediatorSendMismatchAnalyzer());
            var withAnalyzers = compilation.WithAnalyzers(analyzers);

            return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        }
    }
}