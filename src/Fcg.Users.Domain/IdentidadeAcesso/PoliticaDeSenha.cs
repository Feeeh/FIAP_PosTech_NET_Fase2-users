using Fcg.Users.Domain.SeedWork;

namespace Fcg.Users.Domain.IdentidadeAcesso;

/// <summary>
/// Serviço de Domínio: valida a força da senha antes do hash (RF02).
/// Não é método de entidade porque a regra é reutilizada no cadastro e na troca de senha,
/// e porque o Usuario nunca chega a ver a senha em texto puro.
/// </summary>
public static class PoliticaDeSenha
{
    public const int TamanhoMinimo = 8;

    /// <summary>
    /// Verifica a senha e, se houver problema, lança explicando exatamente o que falta —
    /// mitigação do risco de usabilidade registrado no DDD.md seção 15.
    /// </summary>
    /// <exception cref="ExcecaoDeValidacao">Quando a senha não atende à política.</exception>
    public static void Validar(string? senhaEmTextoPuro)
    {
        var senha = senhaEmTextoPuro ?? string.Empty;
        var exigenciasNaoAtendidas = new List<string>();

        if (senha.Length < TamanhoMinimo)
        {
            exigenciasNaoAtendidas.Add($"ter pelo menos {TamanhoMinimo} caracteres");
        }

        if (!senha.Any(char.IsLetter))
        {
            exigenciasNaoAtendidas.Add("conter ao menos uma letra");
        }

        if (!senha.Any(char.IsDigit))
        {
            exigenciasNaoAtendidas.Add("conter ao menos um número");
        }

        if (!senha.Any(EhCaractereEspecial))
        {
            exigenciasNaoAtendidas.Add("conter ao menos um caractere especial");
        }

        if (exigenciasNaoAtendidas.Count > 0)
        {
            throw new ExcecaoDeValidacao($"A senha precisa {string.Join(", ", exigenciasNaoAtendidas)}.");
        }
    }

    /// <summary>Especial é tudo que não é letra, número nem espaço.</summary>
    private static bool EhCaractereEspecial(char caractere) =>
        !char.IsLetterOrDigit(caractere) && !char.IsWhiteSpace(caractere);
}
