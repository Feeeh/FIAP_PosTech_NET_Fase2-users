namespace Fcg.Users.API;

/// <summary>Permite consultar o contrato HTTP em contêineres mesmo fora de Development.</summary>
public sealed class OpcoesDeSwagger
{
    public const string Secao = "Swagger";

    public bool Habilitado { get; set; }
}
