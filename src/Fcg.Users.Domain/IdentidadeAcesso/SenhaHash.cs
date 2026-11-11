using Fcg.Users.Domain.SeedWork;

namespace Fcg.Users.Domain.IdentidadeAcesso;

/// <summary>
/// Guarda o hash da senha — nunca a senha em texto puro. O algoritmo em si é
/// infraestrutura (BCrypt); o domínio só sabe que existe um hash e que ele nunca é exposto.
/// </summary>
public sealed class SenhaHash : ObjetoDeValor
{
    private SenhaHash()
    {
    }

    private SenhaHash(string valor) => Valor = valor;

    public string Valor { get; private set; } = string.Empty;

    /// <summary>Recebe um hash já calculado pela infraestrutura.</summary>
    public static SenhaHash APartirDoHash(string? hashJaCalculado)
    {
        if (string.IsNullOrWhiteSpace(hashJaCalculado))
        {
            throw new ExcecaoDeValidacao("O hash da senha é obrigatório.");
        }

        return new SenhaHash(hashJaCalculado);
    }

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Valor;
    }

    /// <summary>Não expõe o hash: evita vazamento acidental em log ou mensagem de erro.</summary>
    public override string ToString() => "*** hash protegido ***";
}
