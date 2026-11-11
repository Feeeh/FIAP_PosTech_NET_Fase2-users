namespace Fcg.Users.Application.Consultas;

/// <summary>Consultas de Identidade e Acesso.</summary>
public interface IConsultasDeUsuarios
{
    Task<ResultadoPaginado<UsuarioNaLista>> ListarUsuariosAsync(
        int pagina,
        int tamanhoDaPagina,
        CancellationToken cancellationToken = default);

    Task<MeuPerfil?> ObterMeuPerfilAsync(Guid usuarioId, CancellationToken cancellationToken = default);
}
