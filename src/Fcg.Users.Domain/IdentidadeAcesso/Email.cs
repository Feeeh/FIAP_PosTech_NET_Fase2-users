using System.Net.Mail;
using Fcg.Users.Domain.SeedWork;

namespace Fcg.Users.Domain.IdentidadeAcesso;

/// <summary>
/// E-mail do usuário. É a chave natural da identidade, então é sempre normalizado
/// (sem espaços, em minúsculas) para que "Ana@FIAP.com" e "ana@fiap.com" não gerem
/// dois cadastros diferentes.
/// </summary>
public sealed class Email : ObjetoDeValor
{
    public const int TamanhoMaximo = 254;

    private Email()
    {
    }

    private Email(string valor) => Valor = valor;

    public string Valor { get; private set; } = string.Empty;

    public static Email Criar(string? valor)
    {
        var emailNormalizado = valor?.Trim().ToLowerInvariant() ?? string.Empty;

        if (string.IsNullOrEmpty(emailNormalizado))
        {
            throw new ExcecaoDeValidacao("O e-mail é obrigatório.");
        }

        if (emailNormalizado.Length > TamanhoMaximo)
        {
            throw new ExcecaoDeValidacao($"O e-mail pode ter no máximo {TamanhoMaximo} caracteres.");
        }

        if (!TemFormatoValido(emailNormalizado))
        {
            throw new ExcecaoDeValidacao("O e-mail informado não tem um formato válido.");
        }

        return new Email(emailNormalizado);
    }

    /// <summary>
    /// Usa o parser da própria BCL em vez de uma expressão regular caseira — a regra de
    /// e-mail é cheia de exceções e regex própria costuma errar. O domínio precisa ter
    /// um ponto, o que o parser sozinho não exige.
    /// </summary>
    private static bool TemFormatoValido(string email) =>
        MailAddress.TryCreate(email, out var enderecoValido)
        && enderecoValido.Host.Contains('.')
        && !enderecoValido.Host.StartsWith('.')
        && !enderecoValido.Host.EndsWith('.');

    protected override IEnumerable<object?> ObterComponentesDeIgualdade()
    {
        yield return Valor;
    }

    public override string ToString() => Valor;
}
