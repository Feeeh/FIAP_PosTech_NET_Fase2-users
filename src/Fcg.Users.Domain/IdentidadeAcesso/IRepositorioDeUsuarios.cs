namespace Fcg.Users.Domain.IdentidadeAcesso;

/// <summary>Contrato de persistência do agregado Usuario.</summary>
public interface IRepositorioDeUsuarios
{
    Task<Usuario?> ObterPorIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);

    Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancellationToken = default);

    /// <summary>Sustenta a invariante de e-mail único, reforçada por índice no banco.</summary>
    Task<bool> ExisteComEmailAsync(Email email, CancellationToken cancellationToken = default);

    Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
