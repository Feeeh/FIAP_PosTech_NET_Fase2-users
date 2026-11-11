using Fcg.Users.Application.Abstracoes;
using Fcg.Users.Domain.SeedWork;
using Fcg.Users.Infrastructure.Mensageria;
using Microsoft.EntityFrameworkCore;

namespace Fcg.Users.Infrastructure.Persistencia;

/// <summary>
/// Salva usuário e evento na mesma transação; o índice único continua traduzido em conflito de domínio.
/// </summary>
public sealed class UnidadeDeTrabalho(
    UsuariosDbContext contexto,
    ContextoDeCorrelacao correlacao) : IUnidadeDeTrabalho
{
    public async Task<int> SalvarAlteracoesAsync(CancellationToken cancellationToken = default)
    {
        var eventos = contexto.ColetarEventosDeDominio();

        foreach (var evento in eventos)
        {
            var mensagem = MapeadorDeEventosDeIntegracao.Mapear(evento, correlacao.CorrelationId);

            if (mensagem is not null)
            {
                contexto.MensagensDeSaida.Add(mensagem);
            }
        }

        int registrosAfetados;

        try
        {
            registrosAfetados = await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (EhViolacaoDeChaveUnica(excecao))
        {
            // Rede de segurança da condição de corrida: dois pedidos simultâneos passam
            // pela checagem em memória, mas o índice único do banco barra o segundo.
            throw new ExcecaoDeConflito(
                "Esta operação conflita com um registro que já existe. Tente novamente.");
        }

        return registrosAfetados;
    }

    /// <summary>
    /// Códigos do SQL Server para violação de índice único (2601) e de constraint única (2627).
    /// </summary>
    private static bool EhViolacaoDeChaveUnica(DbUpdateException excecao) =>
        excecao.InnerException is Microsoft.Data.SqlClient.SqlException erroDoBanco
        && erroDoBanco.Number is 2601 or 2627;
}
