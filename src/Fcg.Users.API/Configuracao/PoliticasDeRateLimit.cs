using System.Threading.RateLimiting;

namespace Fcg.Users.API;

/// <summary>
/// Limites de tentativas por IP nos dois endpoints anônimos.
/// <para>
/// Usa o limitador nativo do ASP.NET Core — não há dependência externa aqui. Só o
/// login e o cadastro são limitados: são as portas abertas, onde faz sentido frear
/// força bruta e criação de contas em massa.
/// </para>
/// </summary>
public static class PoliticasDeRateLimit
{
    public const string Login = "login";
    public const string Cadastro = "cadastro";

    private const int TentativasDeLoginPorJanela = 10;
    private const int CadastrosPorJanela = 5;
    private static readonly TimeSpan Janela = TimeSpan.FromMinutes(1);

    public static IServiceCollection AdicionarRateLimit(this IServiceCollection servicos)
    {
        servicos.AddRateLimiter(opcoes =>
        {
            opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            opcoes.AddPolicy(Login, contexto =>
                CriarJanelaFixaPorCliente(contexto, TentativasDeLoginPorJanela));

            opcoes.AddPolicy(Cadastro, contexto =>
                CriarJanelaFixaPorCliente(contexto, CadastrosPorJanela));
        });

        return servicos;
    }

    /// <summary>
    /// Uma janela por endereço de origem, para que o excesso de um cliente não
    /// bloqueie os demais.
    /// </summary>
    private static RateLimitPartition<string> CriarJanelaFixaPorCliente(HttpContext contexto, int limite) =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limite,
                Window = Janela,
                QueueLimit = 0
            });
}
