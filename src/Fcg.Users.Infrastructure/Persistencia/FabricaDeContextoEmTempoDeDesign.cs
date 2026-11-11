using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Fcg.Users.Infrastructure.Persistencia;

/// <summary>
/// Usada apenas pelas ferramentas de linha de comando do EF Core
/// (<c>dotnet ef migrations add</c>, <c>dotnet ef database update</c>).
/// <para>
/// Existe para que gerar uma migration não precise subir a API inteira — nem exigir
/// que a chave do JWT esteja configurada só para criar um arquivo de migration.
/// Para gerar o script, basta o provedor; para aplicar, a connection string real
/// vem da variável de ambiente.
/// </para>
/// </summary>
public sealed class FabricaDeContextoEmTempoDeDesign : IDesignTimeDbContextFactory<UsuariosDbContext>
{
    private const string VariavelDeAmbienteDaConexao = "ConnectionStrings__SqlServer";

    /// <summary>Aponta para um banco inexistente: só o provedor importa ao gerar a migration.</summary>
    private const string ConexaoDeReferenciaParaGerarMigration =
        "Server=(localdb)\\mssqllocaldb;Database=fcg_design;Trusted_Connection=True;TrustServerCertificate=True";

    public UsuariosDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable(VariavelDeAmbienteDaConexao);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = ConexaoDeReferenciaParaGerarMigration;
        }

        var opcoes = new DbContextOptionsBuilder<UsuariosDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new UsuariosDbContext(opcoes);
    }
}
