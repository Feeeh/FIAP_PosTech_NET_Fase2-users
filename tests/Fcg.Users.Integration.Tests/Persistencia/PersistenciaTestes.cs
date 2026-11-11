using Fcg.Users.Application.Abstracoes;
using Fcg.Users.Application.Consultas;
using Fcg.Users.Application.IdentidadeAcesso;
using Fcg.Users.Domain.IdentidadeAcesso;
using Fcg.Users.Domain.SeedWork;
using Fcg.Users.Infrastructure.Consultas;
using Fcg.Users.Infrastructure.Mensageria;
using Fcg.Users.Infrastructure.Persistencia;
using Fcg.Users.Infrastructure.Repositorios;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit.Sdk;

namespace Fcg.Users.Integration.Tests.Persistencia;

/// <summary>
/// Integra EF Core, migrations, repositórios, Unit of Work e as consultas Dapper
/// contra SQL Server real. Cada teste recebe um banco isolado e descartável.
/// </summary>
public sealed class PersistenciaTestes : IAsyncLifetime
{
    private readonly string _nomeDoBanco = $"fcg_integracao_{Guid.NewGuid():N}";
    private string _connectionString = string.Empty;

    public async Task InitializeAsync()
    {
        _connectionString = CriarConnectionString();
        await using var contexto = CriarContexto();
        await contexto.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (string.IsNullOrEmpty(_connectionString))
        {
            return;
        }

        await using var contexto = CriarContexto();
        await contexto.Database.EnsureDeletedAsync();
    }

    [Fact]
    public async Task Cadastro_atravessa_aplicacao_repositorio_e_banco()
    {
        await using var contexto = CriarContexto();
        var correlacao = new ContextoDeCorrelacao();
        correlacao.Definir("cadastro-integracao");
        var cadastrar = new CadastrarUsuario(
            new RepositorioDeUsuarios(contexto),
            new HashDeTeste(),
            new UnidadeDeTrabalho(contexto, correlacao),
            new RelogioFixo(new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero)));

        var resposta = await cadastrar.ExecutarAsync(
            new CadastrarUsuarioComando("Ana Souza", "  ANA@fiap.com.br ", "SenhaForte1!"));

        contexto.ChangeTracker.Clear();
        var persistido = await contexto.Usuarios.SingleAsync(usuario => usuario.Id == resposta.UsuarioId);

        Assert.Equal("ana@fiap.com.br", persistido.Email.Valor);
        Assert.Equal(Perfil.Usuario, persistido.Perfil);
        var mensagem = await contexto.MensagensDeSaida.SingleAsync();
        VerificarEventoDeCadastro(mensagem, persistido);
    }

    private static void VerificarEventoDeCadastro(MensagemDeSaida mensagem, Usuario persistido)
    {
        Assert.Equal("user.created", mensagem.RoutingKey);
        Assert.Equal("UserCreatedEvent", mensagem.Tipo);
        Assert.Equal("cadastro-integracao", mensagem.CorrelationId);
        Assert.Null(mensagem.EnviadaEm);

        using var json = System.Text.Json.JsonDocument.Parse(mensagem.Conteudo);
        var raiz = json.RootElement;
        Assert.Equal(
            ["eventId", "occurredAt", "userId", "name", "email"],
            raiz.EnumerateObject().Select(campo => campo.Name).ToArray());
        Assert.Equal(mensagem.Id, raiz.GetProperty("eventId").GetGuid());
        Assert.Equal(persistido.Id, raiz.GetProperty("userId").GetGuid());
        Assert.Equal(persistido.DataDeCadastro, raiz.GetProperty("occurredAt").GetDateTimeOffset());
        Assert.Equal("Ana Souza", raiz.GetProperty("name").GetString());
        Assert.Equal("ana@fiap.com.br", raiz.GetProperty("email").GetString());
    }

    [Fact]
    public async Task Indice_unico_de_email_vira_conflito_de_dominio()
    {
        await using (var primeiroContexto = CriarContexto())
        {
            primeiroContexto.Usuarios.Add(CriarUsuario("ana@fiap.com.br"));
            await primeiroContexto.SaveChangesAsync();
        }

        await using var segundoContexto = CriarContexto();
        segundoContexto.Usuarios.Add(CriarUsuario("ana@fiap.com.br"));
        var unidadeDeTrabalho = new UnidadeDeTrabalho(segundoContexto, new ContextoDeCorrelacao());

        await Assert.ThrowsAsync<ExcecaoDeConflito>(() => unidadeDeTrabalho.SalvarAlteracoesAsync());

        await using var verificacao = CriarContexto();
        Assert.Equal(1, await verificacao.Usuarios.CountAsync());
        Assert.Empty(await verificacao.MensagensDeSaida.ToListAsync());
    }

    [Fact]
    public async Task Consultas_de_usuario_devolvem_perfil_e_paginacao_sem_credenciais()
    {
        // Arrange
        var usuario = CriarUsuario("ana@fiap.com.br");
        await using (var contexto = CriarContexto())
        {
            contexto.Usuarios.Add(usuario);
            await contexto.SaveChangesAsync();
        }

        IConsultasDeUsuarios consultas = new ConsultasDeUsuariosDapper(new FabricaDeConexoes(_connectionString));

        // Act
        var perfil = await consultas.ObterMeuPerfilAsync(usuario.Id);
        var pagina = await consultas.ListarUsuariosAsync(pagina: 1, tamanhoDaPagina: 20);

        // Assert
        Assert.NotNull(perfil);
        Assert.Equal("ana@fiap.com.br", perfil.Email);
        Assert.Equal("Usuario", perfil.Perfil);
        Assert.Equal(usuario.Id, Assert.Single(pagina.Itens).UsuarioId);
        Assert.Equal(1, pagina.TotalDeItens);
    }

    private UsuariosDbContext CriarContexto() => new(
        new DbContextOptionsBuilder<UsuariosDbContext>()
            .UseSqlServer(_connectionString)
            .Options);

    private string CriarConnectionString()
    {
        var servidorConfigurado = Environment.GetEnvironmentVariable("FCG_TEST_SQLSERVER");

        if (string.IsNullOrWhiteSpace(servidorConfigurado))
        {
            if (!OperatingSystem.IsWindows())
            {
                throw SkipException.ForSkip(
                    "Defina FCG_TEST_SQLSERVER com uma conexão SQL Server de teste fora do Windows.");
            }

            servidorConfigurado = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=true";
        }

        var construtor = new SqlConnectionStringBuilder(servidorConfigurado)
        {
            InitialCatalog = _nomeDoBanco
        };

        return construtor.ConnectionString;
    }

    private static Usuario CriarUsuario(string email) => Usuario.Cadastrar(
        Nome.Criar("Ana Souza"),
        Email.Criar(email),
        SenhaHash.APartirDoHash("hash-de-integracao"),
        new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero));

    private sealed class HashDeTeste : IHashDeSenha
    {
        public string Gerar(string senhaEmTextoPuro) => $"hash:{senhaEmTextoPuro}";

        public bool Confere(string senhaEmTextoPuro, string hashArmazenado) =>
            hashArmazenado == Gerar(senhaEmTextoPuro);
    }

    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

}
