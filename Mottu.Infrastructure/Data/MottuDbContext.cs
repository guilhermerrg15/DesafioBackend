// Mottu.Infrastructure/Data/MottuDbContext.cs
using Microsoft.EntityFrameworkCore;
using Mottu.Core.Entities;

namespace Mottu.Infrastructure.Data 
{
    public class MottuDbContext : DbContext
{
    public MottuDbContext(DbContextOptions<MottuDbContext> options)
        : base(options)
    {
    }

    public DbSet<Moto> Motos { get; set; }
    public DbSet<Entregador> Entregadores { get; set; }
    public DbSet<Locacao> Locacoes { get; set; }
    public DbSet<Notificacao> Notificacoes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // configuracoes de unicidade
        modelBuilder.Entity<Moto>()
            .HasIndex(m => m.Placa)
            .IsUnique();
            
        modelBuilder.Entity<Entregador>()
            .HasIndex(e => e.Cnpj)
            .IsUnique();
            
        modelBuilder.Entity<Entregador>()
            .HasIndex(e => e.NumeroCnh)
            .IsUnique();

        // configuracoes de relacionamento
        modelBuilder.Entity<Locacao>()
            .HasOne(l => l.Moto)
            .WithMany() // ou WithMany(m => m.Locacoes) se adicionar ICollection em Moto
            .HasForeignKey(l => l.MotoId);

        base.OnModelCreating(modelBuilder);
    }
}
}
