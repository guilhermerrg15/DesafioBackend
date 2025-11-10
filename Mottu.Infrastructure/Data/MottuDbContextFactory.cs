using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Mottu.Infrastructure.Data;

// Esta classe instrui a ferramenta 'dotnet ef' a como criar o DbContext
public class MottuDbContextFactory : IDesignTimeDbContextFactory<MottuDbContext>
{
    public MottuDbContext CreateDbContext(string[] args)
    {
        // Esta Connection String deve ser a do seu host para o container postgres_db
        var connectionString = "Host=127.0.0.1;Port=5432;Database=mottudb;Username=mottuuser;Password=mottupass";

        var optionsBuilder = new DbContextOptionsBuilder<MottuDbContext>();
        
        optionsBuilder.UseNpgsql(connectionString,
            npgsqlOptions => 
            {
                // Garante que o tooling saiba onde procurar as Migrations
                npgsqlOptions.MigrationsAssembly(typeof(MottuDbContext).Assembly.FullName);
            }
        );

        return new MottuDbContext(optionsBuilder.Options);
    }
}