using Fcg.Users.Application.Abstracoes;
using Fcg.Users.Domain.IdentidadeAcesso;
using Fcg.Users.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fcg.Users.API;

/// <summary>
/// Cria o primeiro Administrador, sem o qual não haveria como cadastrar o primeiro jogo.
/// <para>
/// As credenciais vêm da configuração (user-secrets em desenvolvimento, variável de
/// ambiente em execução) — nunca do código. Sem elas configuradas, o semeador
/// simplesmente não faz nada.
/// </para>
/// </summary>
public static class SemeadorDeDados
{
    public const string SecaoDeConfiguracao = OpcoesDoAdministradorInicial.Secao;

    public static async Task SemearAdministradorInicialAsync(this IServiceProvider provedorDeServicos)
    {
        using var escopo = provedorDeServicos.CreateScope();
        var servicos = escopo.ServiceProvider;

        var configuracao = servicos.GetRequiredService<IOptions<OpcoesDoAdministradorInicial>>().Value;
        var logger = servicos.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(SemeadorDeDados));

        var emailConfigurado = configuracao.Email;
        var senhaConfigurada = configuracao.Senha;

        if (string.IsNullOrWhiteSpace(emailConfigurado) || string.IsNullOrWhiteSpace(senhaConfigurada))
        {
            logger.LogInformation(
                "Administrador inicial não configurado ({Secao}:Email / {Secao}:Senha). Nenhum usuário foi semeado.",
                SecaoDeConfiguracao,
                SecaoDeConfiguracao);
            return;
        }

        var contexto = servicos.GetRequiredService<UsuariosDbContext>();
        var email = Email.Criar(emailConfigurado);

        if (await contexto.Usuarios.AnyAsync(usuario => usuario.Email.Valor == email.Valor))
        {
            logger.LogInformation("Administrador inicial já existe. Nada a semear.");
            return;
        }

        var administrador = CriarAdministrador(servicos, configuracao);

        await contexto.Usuarios.AddAsync(administrador);
        await servicos.GetRequiredService<IUnidadeDeTrabalho>().SalvarAlteracoesAsync();

        // O identificador permite localizar o cadastro sem expor dados pessoais.
        logger.LogInformation("Administrador inicial criado: {UsuarioId}.", administrador.Id);
    }

    private static Usuario CriarAdministrador(
        IServiceProvider servicos,
        OpcoesDoAdministradorInicial configuracao)
    {
        // A senha inicial obedece às mesmas regras para evitar uma conta privilegiada fraca.
        PoliticaDeSenha.Validar(configuracao.Senha);

        var hashDeSenha = servicos.GetRequiredService<IHashDeSenha>();
        var relogio = servicos.GetRequiredService<TimeProvider>();
        var administrador = Usuario.Cadastrar(
            Nome.Criar(configuracao.Nome),
            Email.Criar(configuracao.Email),
            SenhaHash.APartirDoHash(hashDeSenha.Gerar(configuracao.Senha)),
            relogio.GetUtcNow());

        administrador.PromoverAAdministrador();

        return administrador;
    }

}
