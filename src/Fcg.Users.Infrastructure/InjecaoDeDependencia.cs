using Fcg.Users.Application.Abstracoes;
using Fcg.Users.Application.Consultas;
using Fcg.Users.Application.IdentidadeAcesso;
using Fcg.Users.Domain.IdentidadeAcesso;
using Fcg.Users.Infrastructure.Consultas;
using Fcg.Users.Infrastructure.Mensageria;
using Fcg.Users.Infrastructure.Persistencia;
using Fcg.Users.Infrastructure.Repositorios;
using Fcg.Users.Infrastructure.Seguranca;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fcg.Users.Infrastructure;

/// <summary>
/// Registra tudo que a API precisa para funcionar. Os lifetimes seguem o guia de
/// decisões: Scoped para o que acompanha a requisição, Singleton só para o que é
/// imutável e seguro para concorrência.
/// </summary>
public static class InjecaoDeDependencia
{
    public const string NomeDaConexaoSqlServer = "SqlServer";

    public static IServiceCollection AdicionarInfraestrutura(
        this IServiceCollection servicos,
        IConfiguration configuracao)
    {
        ArgumentNullException.ThrowIfNull(configuracao);

        var connectionString = configuracao.GetConnectionString(NomeDaConexaoSqlServer)
            ?? throw new InvalidOperationException(
                $"A connection string '{NomeDaConexaoSqlServer}' não foi configurada.");

        servicos.AddDbContext<UsuariosDbContext>(opcoes => opcoes.UseSqlServer(connectionString));

        // Relógio do sistema. Os testes trocam por um RelogioFalso para controlar o "agora".
        servicos.AddSingleton(TimeProvider.System);

        // Imutável e sem estado por requisição: pode ser Singleton com segurança.
        servicos.AddSingleton<FabricaDeConexoes>(_ => new FabricaDeConexoes(connectionString));
        servicos.AddSingleton<IHashDeSenha, HashDeSenhaBCrypt>();
        servicos.AddSingleton<IGeradorDeToken, GeradorDeTokenJwt>();

        // Um por requisição: carregam o CorrelationId e o DbContext daquela requisição.
        servicos.AddScoped<IUnidadeDeTrabalho, UnidadeDeTrabalho>();

        servicos.AddScoped<IRepositorioDeUsuarios, RepositorioDeUsuarios>();

        servicos.AddScoped<IConsultasDeUsuarios, ConsultasDeUsuariosDapper>();

        // Mensageria: o cadastro grava UserCreatedEvent na outbox, e o despachante o publica.
        servicos
            .AdicionarMensageria(configuracao)
            .AdicionarPublicadorComOutbox<UsuariosDbContext>();

        return servicos;
    }

    /// <summary>
    /// Registra os casos de uso. São Scoped porque dependem de repositórios Scoped —
    /// um Singleton aqui capturaria o DbContext da primeira requisição (o erro que o
    /// guia chama de "Scoped capturado por Singleton").
    /// </summary>
    public static IServiceCollection AdicionarCasosDeUso(this IServiceCollection servicos)
    {
        servicos.AddScoped<CadastrarUsuario>();
        servicos.AddScoped<AutenticarUsuario>();
        servicos.AddScoped<PromoverAAdministrador>();
        servicos.AddScoped<DesativarUsuario>();

        return servicos;
    }
}
