using Fcg.Users.Domain.IdentidadeAcesso;
using Fcg.Users.Domain.IdentidadeAcesso.Eventos;
using Fcg.Users.Domain.SeedWork;

namespace Fcg.Users.Domain.Tests.IdentidadeAcesso;

/// <summary>RF02 — a política de senha, com um teste por motivo de rejeição.</summary>
public sealed class PoliticaDeSenhaTestes
{
    [Theory]
    [InlineData("Ab1!efgh")]
    [InlineData("senhaFORTE123@")]
    [InlineData("!@#$%aA1zzz")]
    public void Aceita_senha_que_atende_a_todas_as_exigencias(string senhaValida) =>
        PoliticaDeSenha.Validar(senhaValida);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Ab1!")]
    public void Rejeita_senha_curta_demais(string? senhaCurta)
    {
        var erro = Assert.Throws<ExcecaoDeValidacao>(() => PoliticaDeSenha.Validar(senhaCurta));

        Assert.Contains("8 caracteres", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejeita_senha_sem_letra()
    {
        var erro = Assert.Throws<ExcecaoDeValidacao>(() => PoliticaDeSenha.Validar("12345678!"));

        Assert.Contains("letra", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejeita_senha_sem_numero()
    {
        var erro = Assert.Throws<ExcecaoDeValidacao>(() => PoliticaDeSenha.Validar("abcdefgh!"));

        Assert.Contains("número", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Rejeita_senha_sem_caractere_especial()
    {
        var erro = Assert.Throws<ExcecaoDeValidacao>(() => PoliticaDeSenha.Validar("abcdefg1"));

        Assert.Contains("especial", erro.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_mensagem_lista_todas_as_exigencias_que_faltam()
    {
        // "abc" falha em tamanho, número e caractere especial de uma vez.
        var erro = Assert.Throws<ExcecaoDeValidacao>(() => PoliticaDeSenha.Validar("abc"));

        Assert.Contains("8 caracteres", erro.Message, StringComparison.Ordinal);
        Assert.Contains("número", erro.Message, StringComparison.Ordinal);
        Assert.Contains("especial", erro.Message, StringComparison.Ordinal);
    }
}

/// <summary>RF01 — formato e normalização do e-mail.</summary>
public sealed class EmailTestes
{
    [Theory]
    [InlineData("ana@fiap.com.br")]
    [InlineData("ana.souza+jogos@alura.com")]
    public void Aceita_email_bem_formado(string emailValido) =>
        Assert.Equal(emailValido, Email.Criar(emailValido).Valor);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("sem-arroba")]
    [InlineData("ana@semponto")]
    [InlineData("@fiap.com")]
    [InlineData("ana@.com")]
    public void Rejeita_email_mal_formado(string? emailInvalido) =>
        Assert.Throws<ExcecaoDeValidacao>(() => Email.Criar(emailInvalido));

    [Fact]
    public void Normaliza_para_minusculas_e_sem_espacos()
    {
        var email = Email.Criar("  Ana@FIAP.com.BR  ");

        Assert.Equal("ana@fiap.com.br", email.Valor);
    }

    [Fact]
    public void Emails_que_normalizam_para_o_mesmo_valor_sao_iguais() =>
        Assert.Equal(Email.Criar("Ana@Fiap.com.br"), Email.Criar("ana@fiap.com.br"));
}

/// <summary>Invariantes do agregado Usuario.</summary>
public sealed class UsuarioTestes
{
    private static readonly DateTimeOffset Agora = new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

    private static Usuario CriarUsuarioAtivo() => Usuario.Cadastrar(
        Nome.Criar("Ana Souza"),
        Email.Criar("ana@fiap.com.br"),
        SenhaHash.APartirDoHash("$2a$12$hashDeExemplo"),
        Agora);

    [Fact]
    public void Todo_usuario_nasce_com_perfil_Usuario_e_ativo()
    {
        var usuario = CriarUsuarioAtivo();

        Assert.Equal(Perfil.Usuario, usuario.Perfil);
        Assert.True(usuario.Ativo);
        Assert.Contains(usuario.EventosDeDominio, evento => evento is UsuarioCadastrado);
    }

    [Fact]
    public void Promover_eleva_o_perfil_a_Administrador()
    {
        var usuario = CriarUsuarioAtivo();

        usuario.PromoverAAdministrador();

        Assert.Equal(Perfil.Administrador, usuario.Perfil);
        Assert.Contains(usuario.EventosDeDominio, evento => evento is PerfilAlterado);
    }

    [Fact]
    public void Promover_duas_vezes_nao_levanta_um_segundo_evento()
    {
        var usuario = CriarUsuarioAtivo();

        usuario.PromoverAAdministrador();
        usuario.PromoverAAdministrador();

        Assert.Single(usuario.EventosDeDominio, evento => evento is PerfilAlterado);
    }

    [Fact]
    public void Usuario_ativo_pode_autenticar()
    {
        var usuario = CriarUsuarioAtivo();

        usuario.GarantirQuePodeAutenticar();

        Assert.Contains(usuario.EventosDeDominio, evento => evento is UsuarioAutenticado);
    }

    [Fact]
    public void Usuario_desativado_nao_autentica()
    {
        var usuario = CriarUsuarioAtivo();
        usuario.Desativar();

        var erro = Assert.Throws<ExcecaoDeAutenticacao>(usuario.GarantirQuePodeAutenticar);

        // Mensagem genérica: não revela que a conta existe e está desativada.
        Assert.Equal("Credenciais inválidas.", erro.Message);
    }

    [Fact]
    public void O_hash_da_senha_nunca_aparece_ao_converter_para_texto()
    {
        var senhaHash = SenhaHash.APartirDoHash("$2a$12$hashSecretoQueNaoPodeVazar");

        Assert.DoesNotContain("hashSecretoQueNaoPodeVazar", senhaHash.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    public void Rejeita_nome_curto_demais(string? nomeInvalido) =>
        Assert.Throws<ExcecaoDeValidacao>(() => Nome.Criar(nomeInvalido));
}
