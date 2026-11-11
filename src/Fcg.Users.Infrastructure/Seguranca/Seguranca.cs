using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fcg.Users.Application.Abstracoes;
using Fcg.Users.Domain.IdentidadeAcesso;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fcg.Users.Infrastructure.Seguranca;

/// <summary>
/// Configuração do JWT. A chave nunca fica no appsettings: vem de user-secrets em
/// desenvolvimento e de variável de ambiente em execução (RNF05).
/// </summary>
public sealed class OpcoesDeJwt
{
    public const string Secao = "Jwt";

    /// <summary>Tamanho mínimo da chave para HS256 — 256 bits.</summary>
    public const int TamanhoMinimoDaChaveEmBytes = 32;

    public string ChaveSecreta { get; set; } = string.Empty;

    public string Emissor { get; set; } = "fcg-users-api";

    public string Audiencia { get; set; } = "fcg-clientes";

    public int MinutosDeValidade { get; set; } = 60;

    /// <summary>
    /// Falha cedo e com mensagem clara se a chave não foi configurada ou é curta demais.
    /// Uma chave fraca torna a assinatura do token quebrável.
    /// </summary>
    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(ChaveSecreta))
        {
            throw new InvalidOperationException(
                "A chave do JWT não foi configurada. "
                + "Defina Jwt:ChaveSecreta via user-secrets ou variável de ambiente.");
        }

        if (Encoding.UTF8.GetByteCount(ChaveSecreta) < TamanhoMinimoDaChaveEmBytes)
        {
            throw new InvalidOperationException(
                $"A chave do JWT precisa ter pelo menos {TamanhoMinimoDaChaveEmBytes} bytes.");
        }
    }

    public SymmetricSecurityKey ObterChaveDeAssinatura() => new(Encoding.UTF8.GetBytes(ChaveSecreta));
}

/// <summary>
/// Emite o JWT assinado em HS256, com as claims que o Context Map publica como
/// contrato: <c>sub</c>, <c>email</c> e <c>role</c>.
/// <para>
/// O payload de um JWT é apenas assinado, não criptografado — qualquer um com o token
/// consegue lê-lo. Por isso não entra nada sigiloso aqui.
/// </para>
/// </summary>
public sealed class GeradorDeTokenJwt(IOptions<OpcoesDeJwt> opcoes) : IGeradorDeToken
{
    private const string ClaimDePerfil = "role";

    private readonly OpcoesDeJwt _opcoes = opcoes.Value;

    public TokenDeAcesso Gerar(Guid usuarioId, string email, Perfil perfil)
    {
        var expiraEm = DateTimeOffset.UtcNow.AddMinutes(_opcoes.MinutosDeValidade);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new(ClaimDePerfil, perfil.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _opcoes.Emissor,
            audience: _opcoes.Audiencia,
            claims: claims,
            expires: expiraEm.UtcDateTime,
            signingCredentials: new SigningCredentials(
                _opcoes.ObterChaveDeAssinatura(),
                SecurityAlgorithms.HmacSha256));

        return new TokenDeAcesso(new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }
}

/// <summary>
/// Hash de senha com BCrypt, que já embute salt aleatório por senha e um fator de
/// trabalho ajustável — duas senhas iguais geram hashes diferentes.
/// </summary>
public sealed class HashDeSenhaBCrypt : IHashDeSenha
{
    /// <summary>Custo do BCrypt. Cada incremento dobra o tempo de cálculo.</summary>
    private const int FatorDeTrabalho = 12;

    public string Gerar(string senhaEmTextoPuro) =>
        BCrypt.Net.BCrypt.HashPassword(senhaEmTextoPuro, FatorDeTrabalho);

    public bool Confere(string senhaEmTextoPuro, string hashArmazenado)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(senhaEmTextoPuro, hashArmazenado);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash corrompido ou em formato antigo: trata como senha errada, sem vazar o motivo.
            return false;
        }
    }
}
