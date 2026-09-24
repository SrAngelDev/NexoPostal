using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexopostal.Shared.Cqrs;
using Xunit;

namespace Nexopostal.Tests.Cqrs;

public class CqrsExecutionTests
{
    public sealed record SensitiveCommand(string Password) : ICommand<int>;

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, error));
    }

    [Fact]
    public async Task Command_ExecutesOnce_AndDoesNotLogPayload()
    {
        var logger = new CapturingLogger<CqrsExecutionBehavior<SensitiveCommand, int>>();
        var behavior = new CqrsExecutionBehavior<SensitiveCommand, int>(logger);
        var calls = 0;
        var result = await behavior.Handle(new("never-log-this-password"), _ => Task.FromResult(++calls), default);
        Assert.Equal(1, result);
        Assert.Equal(1, calls);
        Assert.Single(logger.Messages);
        Assert.Contains("Command SensitiveCommand", logger.Messages[0]);
        Assert.DoesNotContain("never-log-this-password", logger.Messages[0]);
    }

    [Fact]
    public async Task Failure_IsPropagatedWithoutRetryOrSensitiveExceptionLogging()
    {
        var logger = new CapturingLogger<CqrsExecutionBehavior<SensitiveCommand, int>>();
        var behavior = new CqrsExecutionBehavior<SensitiveCommand, int>(logger);
        var calls = 0;
        var error = new InvalidOperationException("private-provider-payload");
        var caught = await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(new("secret"),
            _ => { calls++; throw error; }, default));
        Assert.Same(error, caught);
        Assert.Equal(1, calls);
        Assert.DoesNotContain("private-provider-payload", string.Join("\n", logger.Messages));
    }

    [Fact]
    public async Task AlreadyCancelledCommand_NeverInvokesHandler()
    {
        var behavior = new CqrsExecutionBehavior<SensitiveCommand, int>(
            new CapturingLogger<CqrsExecutionBehavior<SensitiveCommand, int>>());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var calls = 0;
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(new("secret"),
            _ => Task.FromResult(++calls), cancellation.Token));
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ApplicationRequests_HaveExactlyOneKindAndOneRegisteredHandler()
    {
        var assemblies = new[] {
            typeof(NexoPostal.Auth.Application.AuthCqrsRegistration).Assembly,
            typeof(Nexopostal.Ciudadano.Application.CiudadanoCqrsRegistration).Assembly,
            typeof(Nexopostal.Intranet.Application.IntranetCqrsRegistration).Assembly,
            typeof(Nexopostal.Reparto.Application.RepartoCqrsRegistration).Assembly
        };
        foreach (var assembly in assemblies)
        {
            var services = new ServiceCollection().AddApplicationCqrs(assembly);
            var requests = assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract &&
                t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))).ToList();
            Assert.NotEmpty(requests);
            foreach (var request in requests)
            {
                Assert.True(typeof(ICommand).IsAssignableFrom(request) ^ typeof(IQuery).IsAssignableFrom(request), request.FullName);
                var response = request.GetInterfaces().Single(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IRequest<>)).GenericTypeArguments[0];
                var handler = typeof(IRequestHandler<,>).MakeGenericType(request, response);
                Assert.Single(services, d => d.ServiceType == handler);
            }
        }
        // This existing GET route performs writes; its application operation must remain a command.
        Assert.True(typeof(ICommand).IsAssignableFrom(typeof(Nexopostal.Ciudadano.Application.Pagos.VerificarPagoCommand)));
    }
}
