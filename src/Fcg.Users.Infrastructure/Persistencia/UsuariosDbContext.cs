using Fcg.Users.Domain.IdentidadeAcesso;
using Fcg.Users.Domain.SeedWork;
using Microsoft.EntityFrameworkCore;
using Fcg.Users.Infrastructure.Mensageria;

namespace Fcg.Users.Infrastructure.Persistencia;

/// <summary>
/// Persiste somente Identidade e a outbox, garantindo uma transação para usuário e evento.
/// </summary>
public sealed class UsuariosDbContext(DbContextOptions<UsuariosDbContext> opcoes) : DbContext(opcoes)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<MensagemDeSaida> MensagensDeSaida => Set<MensagemDeSaida>();

    /// <summary>
    /// Recolhe os eventos levantados pelos agregados desta transação e limpa a lista,
    /// para que a infraestrutura possa publicá-los depois do commit.
    /// </summary>
    public IReadOnlyCollection<EventoDeDominio> ColetarEventosDeDominio()
    {
        var raizesComEventos = ChangeTracker
            .Entries<RaizDeAgregado<Guid>>()
            .Select(entrada => entrada.Entity)
            .Where(raiz => raiz.EventosDeDominio.Count > 0)
            .ToList();

        var eventos = raizesComEventos.SelectMany(raiz => raiz.EventosDeDominio).ToList();

        foreach (var raiz in raizesComEventos)
        {
            raiz.LimparEventosDeDominio();
        }

        return eventos;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UsuariosDbContext).Assembly);
    }
}
