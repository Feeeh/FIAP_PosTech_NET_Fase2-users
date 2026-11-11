using System.Data;
using Fcg.Users.Application.Consultas;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Fcg.Users.Infrastructure.Consultas;

/// <summary>
/// Abre conexões para as consultas de leitura. Uma conexão por consulta, sempre
/// descartada com <c>using</c>.
/// </summary>
public sealed class FabricaDeConexoes(string connectionString)
{
    public IDbConnection Abrir() => new SqlConnection(connectionString);
}

/// <summary>Read Models de Identidade e Acesso.</summary>
public sealed class ConsultasDeUsuariosDapper(FabricaDeConexoes fabricaDeConexoes) : IConsultasDeUsuarios
{
    private const int PrimeiraPagina = 1;
    private const int TamanhoMaximoDaPagina = 100;

    public async Task<ResultadoPaginado<UsuarioNaLista>> ListarUsuariosAsync(
        int pagina,
        int tamanhoDaPagina,
        CancellationToken cancellationToken = default)
    {
        var paginaSegura = Math.Max(pagina, PrimeiraPagina);
        var tamanhoSeguro = Math.Clamp(tamanhoDaPagina, PrimeiraPagina, TamanhoMaximoDaPagina);

        const string sql = """
            SELECT
                usuario.Id             AS UsuarioId,
                usuario.Nome           AS Nome,
                usuario.Email          AS Email,
                usuario.Perfil         AS Perfil,
                usuario.Ativo          AS Ativo,
                usuario.DataDeCadastro AS DataDeCadastro
            FROM Usuarios AS usuario
            ORDER BY usuario.Nome
            OFFSET @deslocamento ROWS FETCH NEXT @tamanho ROWS ONLY;

            SELECT COUNT(1) FROM Usuarios;
            """;

        var parametros = new { deslocamento = (paginaSegura - 1) * tamanhoSeguro, tamanho = tamanhoSeguro };

        using var conexao = fabricaDeConexoes.Abrir();
        var comando = new CommandDefinition(sql, parametros, cancellationToken: cancellationToken);
        using var resultado = await conexao.QueryMultipleAsync(comando);

        var itens = (await resultado.ReadAsync<UsuarioNaLista>()).ToList();
        var total = await resultado.ReadFirstAsync<int>();

        return new ResultadoPaginado<UsuarioNaLista>(itens, paginaSegura, tamanhoSeguro, total);
    }

    public async Task<MeuPerfil?> ObterMeuPerfilAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        // A senha (nem o hash) nunca sai em consulta de leitura.
        const string sql = """
            SELECT usuario.Id AS UsuarioId, usuario.Nome, usuario.Email, usuario.Perfil
            FROM Usuarios AS usuario
            WHERE usuario.Id = @usuarioId;
            """;

        using var conexao = fabricaDeConexoes.Abrir();
        var comando = new CommandDefinition(sql, new { usuarioId }, cancellationToken: cancellationToken);

        return await conexao.QueryFirstOrDefaultAsync<MeuPerfil>(comando);
    }
}
