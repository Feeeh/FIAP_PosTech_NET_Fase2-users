using Fcg.Users.Domain.SeedWork;

namespace Fcg.Users.Domain.IdentidadeAcesso;

/// <summary>Nome de exibição do usuário. Objeto de Valor: imutável e auto-validável.</summary>
public sealed class Nome : ObjetoDeValor
{
    public const int TamanhoMinimo = 2;
    public const int TamanhoMaximo = 150;

    private Nome()
    {
    }

    private Nome(string valor) => Valor = valor;

    public string Valor { get; private set; } = string.Empty;

    public static Nome Criar(string? valor)
    {
        var nomeLimpo = valor?.Trim() ?? string.Empty;

        if (nomeLimpo.Length < TamanhoMinimo)
        {
            throw new ExcecaoDeValidacao($"O nome precisa ter pelo menos {TamanhoMinimo} caracteres.");
        }

        if (nomeLimpo.Length > TamanhoMaximo)
        {
            throw new ExcecaoDeValidacao($"O nome pode ter no máximo {TamanhoMaximo} caracteres.");
        }

        return new Nome(nomeLimpo);
    }

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Valor;
    }

    public override string ToString() => Valor;
}
