using TinyMediator.SourceGen;

namespace TinyMediator.Tests
{
    public class MediatorSendMismatchAnalyzerTests
    {
        [Fact]
        public async Task Send_SingleArg_Against_TwoArgHandler_Reports_TM003()
        {
            const string source = """
                using System.Threading;
                using System.Threading.Tasks;
                using TinyMediator;

                public record TemplatedEmailRequest;

                public class TemplatedEmailHandler : IRequestHandler<TemplatedEmailRequest, bool>
                {
                    public Task<bool> Handle(TemplatedEmailRequest request, CancellationToken ct = default)
                        => Task.FromResult(true);
                }

                public class Caller
                {
                    public async Task Run(IMediator mediator)
                    {
                        await mediator.Send(new TemplatedEmailRequest());
                    }
                }
                """;

            var diagnostics = await AnalyzerTestHelper.GetAnalyzerDiagnosticsAsync(source);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(MediatorSendMismatchAnalyzer.MismatchRule.Id, diagnostic.Id);
            Assert.Contains("TemplatedEmailHandler", diagnostic.GetMessage());
        }

        [Fact]
        public async Task Send_TypedResponse_Against_OneArgHandler_Reports_TM004()
        {
            const string source = """
                using System.Threading;
                using System.Threading.Tasks;
                using TinyMediator;

                public record PingRequest;

                public class PingHandler : IRequestHandler<PingRequest>
                {
                    public Task Handle(PingRequest request, CancellationToken ct = default)
                        => Task.CompletedTask;
                }

                public class Caller
                {
                    public async Task Run(IMediator mediator)
                    {
                        await mediator.Send<PingRequest, bool>(new PingRequest());
                    }
                }
                """;

            var diagnostics = await AnalyzerTestHelper.GetAnalyzerDiagnosticsAsync(source);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(MediatorSendMismatchAnalyzer.NoResponseRule.Id, diagnostic.Id);
        }

        [Fact]
        public async Task Send_WrongResponseType_Reports_TM005()
        {
            const string source = """
                using System.Threading;
                using System.Threading.Tasks;
                using TinyMediator;

                public record TemplatedEmailRequest;

                public class TemplatedEmailHandler : IRequestHandler<TemplatedEmailRequest, bool>
                {
                    public Task<bool> Handle(TemplatedEmailRequest request, CancellationToken ct = default)
                        => Task.FromResult(true);
                }

                public class Caller
                {
                    public async Task Run(IMediator mediator)
                    {
                        await mediator.Send<TemplatedEmailRequest, int>(new TemplatedEmailRequest());
                    }
                }
                """;

            var diagnostics = await AnalyzerTestHelper.GetAnalyzerDiagnosticsAsync(source);

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(MediatorSendMismatchAnalyzer.WrongResponseTypeRule.Id, diagnostic.Id);
        }

        [Fact]
        public async Task Send_MatchingShapes_Reports_Nothing()
        {
            const string source = """
                using System.Threading;
                using System.Threading.Tasks;
                using TinyMediator;

                public record TemplatedEmailRequest;

                public class TemplatedEmailHandler : IRequestHandler<TemplatedEmailRequest, bool>
                {
                    public Task<bool> Handle(TemplatedEmailRequest request, CancellationToken ct = default)
                        => Task.FromResult(true);
                }

                public class Caller
                {
                    public async Task Run(IMediator mediator)
                    {
                        var result = await mediator.Send<TemplatedEmailRequest, bool>(new TemplatedEmailRequest());
                    }
                }
                """;

            var diagnostics = await AnalyzerTestHelper.GetAnalyzerDiagnosticsAsync(source);

            Assert.Empty(diagnostics);
        }
    }
}