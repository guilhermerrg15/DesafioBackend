using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mottu.Infrastructure.Data;
using Mottu.Core.Services;

namespace Mottu.Tests.IntegrationTests;


public class ApiTestBase : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Remove the real DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<MottuDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add InMemory database for testing
            var dbName = $"TestDb_{Guid.NewGuid()}";
            services.AddDbContext<MottuDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // Replace IMessageService with a mock
            var messageServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IMessageService));
            if (messageServiceDescriptor != null)
            {
                services.Remove(messageServiceDescriptor);
            }

            services.AddSingleton<IMessageService, MockMessageService>();
        });
        
        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ConnectionStrings:DefaultConnection", "Host=localhost;Port=5432;Database=testdb;Username=test;Password=test" }
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<MottuDbContext>();
                db.Database.EnsureDeleted();
            }
            catch
            {
                // Ignore errors during disposal
            }
        }
        base.Dispose(disposing);
    }
}

// Mock implementation of IMessageService for testing
public class MockMessageService : IMessageService
{
    public Task PublishMotoCadastradaAsync(int motoId, int ano, string modelo, string placa)
    {
        // Mock implementation - does nothing
        return Task.CompletedTask;
    }
}

