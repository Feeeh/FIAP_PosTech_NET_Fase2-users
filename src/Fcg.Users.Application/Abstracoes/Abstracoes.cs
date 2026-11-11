using Fcg.Users.Domain.IdentidadeAcesso;

namespace Fcg.Users.Application.Abstracoes;

/// <summary>Token emitido na autenticação, com o instante em que deixa de valer.</summary>
public sealed record TokenDeAcesso(string Token, DateTimeOffset ExpiraEm);

/// <summary>
/// Emite o JWT. É infraestrutura exposta à aplicação por interface — o domínio
/// não conhece JWT (DDD.md 12.3, "não são Domain Services").
/// </summary>
public interface IGeradorDeToken
{
    TokenDeAcesso Gerar(Guid usuarioId, string email, Perfil perfil);
}

/// <summary>
/// Calcula e confere o hash da senha. A senha em texto puro só existe aqui e na
/// PoliticaDeSenha; nunca entra no agregado nem em log.
/// </summary>
public interface IHashDeSenha
{
    string Gerar(string senhaEmTextoPuro);

    bool Confere(string senhaEmTextoPuro, string hashArmazenado);
}

/// <summary>
/// Fecha a transação do caso de uso. Uma chamada por caso de uso, alterando um agregado.
/// </summary>
public interface IUnidadeDeTrabalho
{
    Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default);
}
