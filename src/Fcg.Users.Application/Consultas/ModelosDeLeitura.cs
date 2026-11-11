namespace Fcg.Users.Application.Consultas;

/// <summary>Página de resultados, para que listas grandes não voltem inteiras.</summary>
public sealed record ResultadoPaginado<T>(
    IReadOnlyCollection<T> Itens,
    int Pagina,
    int TamanhoDaPagina,
    int TotalDeItens)
{
    public int TotalDePaginas => TamanhoDaPagina <= 0 ? 0 : (int)Math.Ceiling(TotalDeItens / (double)TamanhoDaPagina);
}

/// <summary>Read Model &lt;Lista de Usuários&gt;, visível apenas para o Administrador.</summary>
public sealed record UsuarioNaLista(
    Guid UsuarioId,
    string Nome,
    string Email,
    string Perfil,
    bool Ativo,
    DateTimeOffset DataDeCadastro);

/// <summary>Read Model &lt;Meu Perfil&gt;.</summary>
public sealed record MeuPerfil(Guid UsuarioId, string Nome, string Email, string Perfil);
