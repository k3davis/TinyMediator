using Microsoft.Extensions.DependencyInjection;

namespace TinyMediator.Tests
{
    public class MediatorTests
    {
        [Fact]
        public async Task Mediator_Should_Invoke_Correct_Handler()
        {
            var services = new ServiceCollection();
            services.AddTinyMediatorHandlers(); // this should be generated
            var provider = services.BuildServiceProvider();

            var mediator = provider.GetRequiredService<IMediator>();

            var response = await mediator.Send<PingRequest, string>(new PingRequest(), new CancellationToken());

            Assert.Equal("Pong", response);
        }

        // sample request and handler for testing
        public class PingRequest { }

        public class PingHandler : IRequestHandler<PingRequest, string>
        {
            public Task<string> Handle(PingRequest request, CancellationToken c) => Task.FromResult("Pong");
        }
    }

    /*
    public static partial class MediatorServiceCollectionExtensions
    {
        public static partial void RegisterMediatorCore(IServiceCollection services)
        {
            services.AddSingleton<IMediator>(provider =>
            {
                var inner = new Mediator(provider);
                return new LoggingMediator(inner);
            });
        }
    }
    */
}