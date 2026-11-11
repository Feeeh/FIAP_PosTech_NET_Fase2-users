using Fcg.Users.Application.Abstracoes;
using Fcg.Users.Domain.IdentidadeAcesso;
using Fcg.Users.Domain.SeedWork;

namespace Fcg.Users.Application.IdentidadeAcesso;

/// <summary>Dados recebidos para validar e cadastrar a conta.</summary>
public sealed record CadastrarUsuarioComando(string Nome, string Email, string Senha);

/// <summary>Confirma o cadastro sem expor credenciais.</summary>
public sealed record UsuarioCadastradoResposta(Guid UsuarioId, string Nome, string Email, string Perfil);

/// <summary>
/// RF01 e RF02 — cadastra um usuário validando e-mail e força da senha.
/// Coordena; quem decide as regras é o domínio.
/// </summary>
public sealed class CadastrarUsuario(
    IRepositorioDeUsuarios repositorioDeUsuarios,
    IHashDeSenha hashDeSenha,
    IUnidadeDeTrabalho unidadeDeTrabalho,
    TimeProvider relogio)
{
    public async Task<UsuarioCadastradoResposta> ExecutarAsync(
        CadastrarUsuarioComando comando,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var nome = Nome.Criar(comando.Nome);
        var email = Email.Criar(comando.Email);

        // A força da senha é conferida ANTES do hash: depois de hasheada não dá mais.
        PoliticaDeSenha.Validar(comando.Senha);

        if (await repositorioDeUsuarios.ExisteComEmailAsync(email, cancellationToken))
        {
            throw new ExcecaoDeConflito("Já existe um usuário cadastrado com este e-mail.");
        }

        var senhaHash = SenhaHash.APartirDoHash(hashDeSenha.Gerar(comando.Senha));
        var usuario = Usuario.Cadastrar(nome, email, senhaHash, relogio.GetUtcNow());

        await repositorioDeUsuarios.AdicionarAsync(usuario, cancellationToken);
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        return new UsuarioCadastradoResposta(usuario.Id, nome.Valor, email.Valor, usuario.Perfil.ToString());
    }
}

/// <summary>Credenciais transitórias usadas somente na autenticação.</summary>
public sealed record AutenticarUsuarioComando(string Email, string Senha);

/// <summary>Entrega a credencial assinada e seu prazo ao cliente.</summary>
public sealed record AutenticacaoResposta(string Token, DateTimeOffset ExpiraEm, string Perfil);

/// <summary>
/// RF03 — autentica e emite o JWT.
/// <para>
/// Toda falha devolve a mesma mensagem, decisão registrada no Event Storming
/// (ponto de atenção "enumeração de usuários"): a resposta não pode revelar
/// se o e-mail existe, se a senha está errada ou se a conta foi desativada.
/// </para>
/// </summary>
public sealed class AutenticarUsuario(
    IRepositorioDeUsuarios repositorioDeUsuarios,
    IHashDeSenha hashDeSenha,
    IGeradorDeToken geradorDeToken,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    private const string MensagemGenericaDeFalha = "Credenciais inválidas.";

    public async Task<AutenticacaoResposta> ExecutarAsync(
        AutenticarUsuarioComando comando,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        Usuario? usuario = null;

        // E-mail malformado não é motivo para uma resposta diferente: também é falha genérica.
        try
        {
            var email = Email.Criar(comando.Email);
            usuario = await repositorioDeUsuarios.ObterPorEmailAsync(email, cancellationToken);
        }
        catch (ExcecaoDeValidacao)
        {
            throw new ExcecaoDeAutenticacao(MensagemGenericaDeFalha);
        }

        if (usuario is null || !hashDeSenha.Confere(comando.Senha ?? string.Empty, usuario.SenhaHash.Valor))
        {
            throw new ExcecaoDeAutenticacao(MensagemGenericaDeFalha);
        }

        usuario.GarantirQuePodeAutenticar();
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);

        var token = geradorDeToken.Gerar(usuario.Id, usuario.Email.Valor, usuario.Perfil);

        return new AutenticacaoResposta(token.Token, token.ExpiraEm, usuario.Perfil.ToString());
    }
}

/// <summary>RF06 — promove um usuário a Administrador.</summary>
public sealed class PromoverAAdministrador(
    IRepositorioDeUsuarios repositorioDeUsuarios,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task ExecutarAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await repositorioDeUsuarios.ObterPorIdAsync(usuarioId, cancellationToken)
            ?? throw new ExcecaoDeNaoEncontrado("Usuário não encontrado.");

        usuario.PromoverAAdministrador();
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }
}

/// <summary>RF06 — desativa um usuário, invalidando o acesso dele.</summary>
public sealed class DesativarUsuario(
    IRepositorioDeUsuarios repositorioDeUsuarios,
    IUnidadeDeTrabalho unidadeDeTrabalho)
{
    public async Task ExecutarAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        var usuario = await repositorioDeUsuarios.ObterPorIdAsync(usuarioId, cancellationToken)
            ?? throw new ExcecaoDeNaoEncontrado("Usuário não encontrado.");

        usuario.Desativar();
        await unidadeDeTrabalho.SalvarAlteracoesAsync(cancellationToken);
    }
}
