using Fcg.Users.Domain.IdentidadeAcesso;
using Fcg.Users.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Users.Infrastructure.Repositorios;

/// <summary>Repositório do agregado Usuario sobre EF Core.</summary>
public sealed class RepositorioDeUsuarios(UsuariosDbContext contexto) : IRepositorioDeUsuarios
{
    public Task<Usuario?> ObterPorIdAsync(Guid usuarioId, CancellationToken cancellationToken = default) =>
        contexto.Usuarios.FirstOrDefaultAsync(usuario => usuario.Id == usuarioId, cancellationToken);

    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        return contexto.Usuarios.FirstOrDefaultAsync(
            usuario => usuario.Email.Valor == email.Valor,
            cancellationToken);
    }

    public Task<bool> ExisteComEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        return contexto.Usuarios.AnyAsync(usuario => usuario.Email.Valor == email.Valor, cancellationToken);
    }

    public async Task AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
        await contexto.Usuarios.AddAsync(usuario, cancellationToken);
}
