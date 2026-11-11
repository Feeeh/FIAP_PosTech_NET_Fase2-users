using Fcg.Users.Application.Abstracoes;
using Fcg.Users.Application.IdentidadeAcesso;
using Fcg.Users.Domain.IdentidadeAcesso;
using Fcg.Users.Domain.SeedWork;
using NSubstitute;

namespace Fcg.Users.Application.Tests.IdentidadeAcesso;

/// <summary>RF01 e RF02 pelo caso de uso, com repositórios substituídos por dublês.</summary>
public sealed class CadastrarUsuarioTestes
{
    private readonly IRepositorioDeUsuarios _repositorioDeUsuarios = Substitute.For<IRepositorioDeUsuarios>();
    private readonly IHashDeSenha _hashDeSenha = Substitute.For<IHashDeSenha>();
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho = Substitute.For<IUnidadeDeTrabalho>();

    private CadastrarUsuario CriarCasoDeUso() =>
        new(_repositorioDeUsuarios, _hashDeSenha, _unidadeDeTrabalho, RelogioFalso.Padrao());

    private static CadastrarUsuarioComando ComandoValido() =>
        new("Ana Souza", "ana@fiap.com.br", "SenhaForte1!");

    public CadastrarUsuarioTestes() =>
        _hashDeSenha.Gerar(Arg.Any<string>()).Returns("$2a$12$hashDeExemplo");

    [Fact]
    public async Task Cadastra_o_usuario_com_perfil_padrao_e_salva()
    {
        _repositorioDeUsuarios.ExisteComEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);

        var resposta = await CriarCasoDeUso().ExecutarAsync(ComandoValido());

        Assert.Equal("ana@fiap.com.br", resposta.Email);
        Assert.Equal(nameof(Perfil.Usuario), resposta.Perfil);
        await _repositorioDeUsuarios.Received(1).AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
        await _unidadeDeTrabalho.Received(1).SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejeita_email_ja_cadastrado_sem_salvar_nada()
    {
        _repositorioDeUsuarios.ExisteComEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);

        await Assert.ThrowsAsync<ExcecaoDeConflito>(() => CriarCasoDeUso().ExecutarAsync(ComandoValido()));

        await _repositorioDeUsuarios.DidNotReceive().AdicionarAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>());
        await _unidadeDeTrabalho.DidNotReceive().SalvarAlteracoesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Senha_fraca_e_recusada_antes_de_qualquer_hash()
    {
        var comandoComSenhaFraca = ComandoValido() with { Senha = "123" };

        await Assert.ThrowsAsync<ExcecaoDeValidacao>(
            () => CriarCasoDeUso().ExecutarAsync(comandoComSenhaFraca));

        // A senha nunca chega a ser hasheada: a política roda antes.
        _hashDeSenha.DidNotReceive().Gerar(Arg.Any<string>());
    }

    [Fact]
    public async Task Email_invalido_e_recusado_antes_de_consultar_o_banco()
    {
        var comandoComEmailInvalido = ComandoValido() with { Email = "sem-arroba" };

        await Assert.ThrowsAsync<ExcecaoDeValidacao>(
            () => CriarCasoDeUso().ExecutarAsync(comandoComEmailInvalido));

        await _repositorioDeUsuarios.DidNotReceive()
            .ExisteComEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_senha_guardada_e_sempre_o_hash_nunca_o_texto_puro()
    {
        _repositorioDeUsuarios.ExisteComEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);
        Usuario? usuarioSalvo = null;
        await _repositorioDeUsuarios.AdicionarAsync(
            Arg.Do<Usuario>(usuario => usuarioSalvo = usuario),
            Arg.Any<CancellationToken>());

        await CriarCasoDeUso().ExecutarAsync(ComandoValido());

        Assert.NotNull(usuarioSalvo);
        Assert.Equal("$2a$12$hashDeExemplo", usuarioSalvo.SenhaHash.Valor);
        Assert.NotEqual("SenhaForte1!", usuarioSalvo.SenhaHash.Valor);
    }
}

/// <summary>RF03 — autenticação, com atenção especial à mensagem genérica de falha.</summary>
public sealed class AutenticarUsuarioTestes
{
    private const string MensagemGenerica = "Credenciais inválidas.";

    private readonly IRepositorioDeUsuarios _repositorioDeUsuarios = Substitute.For<IRepositorioDeUsuarios>();
    private readonly IHashDeSenha _hashDeSenha = Substitute.For<IHashDeSenha>();
    private readonly IGeradorDeToken _geradorDeToken = Substitute.For<IGeradorDeToken>();
    private readonly IUnidadeDeTrabalho _unidadeDeTrabalho = Substitute.For<IUnidadeDeTrabalho>();

    private AutenticarUsuario CriarCasoDeUso() =>
        new(_repositorioDeUsuarios, _hashDeSenha, _geradorDeToken, _unidadeDeTrabalho);

    private static Usuario CriarUsuario() => Usuario.Cadastrar(
        Nome.Criar("Ana Souza"),
        Email.Criar("ana@fiap.com.br"),
        SenhaHash.APartirDoHash("$2a$12$hashDeExemplo"),
        RelogioFalso.InstanteDeReferencia);

    public AutenticarUsuarioTestes() =>
        _geradorDeToken.Gerar(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Perfil>())
            .Returns(new TokenDeAcesso("token-de-teste", RelogioFalso.InstanteDeReferencia.AddHours(1)));

    [Fact]
    public async Task Emite_o_token_quando_as_credenciais_conferem()
    {
        _repositorioDeUsuarios.ObterPorEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(CriarUsuario());
        _hashDeSenha.Confere(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var resposta = await CriarCasoDeUso().ExecutarAsync(
            new AutenticarUsuarioComando("ana@fiap.com.br", "SenhaForte1!"));

        Assert.Equal("token-de-teste", resposta.Token);
        Assert.Equal(nameof(Perfil.Usuario), resposta.Perfil);
    }

    [Fact]
    public async Task Email_inexistente_devolve_a_mensagem_generica()
    {
        _repositorioDeUsuarios.ObterPorEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns((Usuario?)null);

        var erro = await Assert.ThrowsAsync<ExcecaoDeAutenticacao>(
            () => CriarCasoDeUso().ExecutarAsync(
                new AutenticarUsuarioComando("naoexiste@fiap.com.br", "SenhaForte1!")));

        Assert.Equal(MensagemGenerica, erro.Message);
    }

    [Fact]
    public async Task Senha_errada_devolve_exatamente_a_mesma_mensagem_de_email_inexistente()
    {
        _repositorioDeUsuarios.ObterPorEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(CriarUsuario());
        _hashDeSenha.Confere(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var erro = await Assert.ThrowsAsync<ExcecaoDeAutenticacao>(
            () => CriarCasoDeUso().ExecutarAsync(new AutenticarUsuarioComando("ana@fiap.com.br", "SenhaErrada1!")));

        // Idêntica à do e-mail inexistente: impede descobrir quais e-mails existem.
        Assert.Equal(MensagemGenerica, erro.Message);
    }

    [Fact]
    public async Task Email_mal_formado_tambem_devolve_a_mensagem_generica()
    {
        var erro = await Assert.ThrowsAsync<ExcecaoDeAutenticacao>(
            () => CriarCasoDeUso().ExecutarAsync(new AutenticarUsuarioComando("sem-arroba", "SenhaForte1!")));

        Assert.Equal(MensagemGenerica, erro.Message);
    }

    [Fact]
    public async Task Usuario_desativado_nao_recebe_token()
    {
        var usuarioDesativado = CriarUsuario();
        usuarioDesativado.Desativar();
        _repositorioDeUsuarios.ObterPorEmailAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>())
            .Returns(usuarioDesativado);
        _hashDeSenha.Confere(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var erro = await Assert.ThrowsAsync<ExcecaoDeAutenticacao>(
            () => CriarCasoDeUso().ExecutarAsync(new AutenticarUsuarioComando("ana@fiap.com.br", "SenhaForte1!")));

        Assert.Equal(MensagemGenerica, erro.Message);
        _geradorDeToken.DidNotReceive().Gerar(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Perfil>());
    }
}
