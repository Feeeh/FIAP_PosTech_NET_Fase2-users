using Fcg.Users.Domain.IdentidadeAcesso.Eventos;
using Fcg.Users.Domain.SeedWork;

namespace Fcg.Users.Domain.IdentidadeAcesso;

/// <summary>
/// Pessoa com conta na FCG, identificada por e-mail único.
/// <para>
/// Invariantes (DDD.md 12.1): e-mail sempre em formato válido; a senha só vira hash
/// depois de aprovada pela <see cref="PoliticaDeSenha"/>; um usuário desativado não autentica.
/// </para>
/// </summary>
public sealed class Usuario : RaizDeAgregado<Guid>
{
    private Usuario()
    {
    }

    private Usuario(Guid id, Nome nome, Email email, SenhaHash senhaHash, Perfil perfil, DateTimeOffset dataDeCadastro)
        : base(id)
    {
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
        Perfil = perfil;
        Ativo = true;
        DataDeCadastro = dataDeCadastro;
    }

    public Nome Nome { get; private set; } = null!;

    public Email Email { get; private set; } = null!;

    public SenhaHash SenhaHash { get; private set; } = null!;

    public Perfil Perfil { get; private set; }

    /// <summary>Usuário desativado continua existindo para histórico, mas não autentica.</summary>
    public bool Ativo { get; private set; }

    public DateTimeOffset DataDeCadastro { get; private set; }

    /// <summary>
    /// Cadastra um usuário. A senha já chega hasheada: a validação de força acontece
    /// antes, na PoliticaDeSenha, porque o domínio nunca recebe a senha em texto puro.
    /// <para>
    /// Aplica a política "sempre que Usuário Cadastrado, atribuir perfil Usuário".
    /// </para>
    /// </summary>
    public static Usuario Cadastrar(Nome nome, Email email, SenhaHash senhaHash, DateTimeOffset dataDeCadastro)
    {
        ArgumentNullException.ThrowIfNull(nome);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(senhaHash);

        var usuario = new Usuario(Guid.NewGuid(), nome, email, senhaHash, Perfil.Usuario, dataDeCadastro);
        usuario.RegistrarEvento(new UsuarioCadastrado(usuario.Id, nome.Valor, email.Valor)
        {
            OcorridoEm = dataDeCadastro
        });

        return usuario;
    }

    /// <summary>Eleva o usuário a Administrador. Repetir a promoção não faz nada — é idempotente.</summary>
    public void PromoverAAdministrador()
    {
        if (Perfil == Perfil.Administrador)
        {
            return;
        }

        var perfilAnterior = Perfil;
        Perfil = Perfil.Administrador;
        RegistrarEvento(new PerfilAlterado(Id, perfilAnterior, Perfil));
    }

    /// <summary>Rebaixa o usuário a perfil comum. Também é idempotente.</summary>
    public void RebaixarAUsuario()
    {
        if (Perfil == Perfil.Usuario)
        {
            return;
        }

        var perfilAnterior = Perfil;
        Perfil = Perfil.Usuario;
        RegistrarEvento(new PerfilAlterado(Id, perfilAnterior, Perfil));
    }

    /// <summary>Desativa o usuário, invalidando o acesso dele a partir de agora.</summary>
    public void Desativar()
    {
        if (!Ativo)
        {
            return;
        }

        Ativo = false;
        RegistrarEvento(new UsuarioDesativado(Id));
    }

    public void Reativar() => Ativo = true;

    /// <summary>
    /// Confirma que este usuário pode autenticar. Chamado depois da conferência da senha.
    /// </summary>
    /// <exception cref="ExcecaoDeAutenticacao">Quando o usuário está desativado.</exception>
    public void GarantirQuePodeAutenticar()
    {
        if (!Ativo)
        {
            // Mensagem genérica de propósito: não revela se a conta existe ou está desativada.
            throw new ExcecaoDeAutenticacao("Credenciais inválidas.");
        }

        RegistrarEvento(new UsuarioAutenticado(Id, Perfil));
    }
}
