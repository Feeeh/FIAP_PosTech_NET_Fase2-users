namespace Fcg.Users.API;

/// <summary>Recebe as credenciais de implantação sem armazenar segredos no repositório.</summary>
public sealed class OpcoesDoAdministradorInicial
{
    public const string Secao = "AdministradorInicial";

    public string Nome { get; set; } = "Administrador FCG";

    public string Email { get; set; } = string.Empty;

    public string Senha { get; set; } = string.Empty;
}
