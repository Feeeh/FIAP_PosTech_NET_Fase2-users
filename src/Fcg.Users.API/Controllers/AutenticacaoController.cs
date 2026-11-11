using Fcg.Users.Application.IdentidadeAcesso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fcg.Users.API.Controllers;

/// <summary>Emissão do Token de Acesso (RF03).</summary>
[ApiController]
[Route("api/autenticacao")]
[Produces("application/json")]
public sealed class AutenticacaoController(AutenticarUsuario autenticarUsuario) : ControllerBase
{
    /// <summary>
    /// Autentica um usuário e devolve o JWT.
    /// </summary>
    /// <remarks>
    /// A resposta de falha é sempre a mesma, não importa se o e-mail não existe, se a
    /// senha está errada ou se a conta foi desativada. Isso impede que alguém descubra
    /// quais e-mails estão cadastrados testando o endpoint.
    /// <para>Este endpoint tem limite de tentativas, como freio a ataques de força bruta.</para>
    /// </remarks>
    /// <response code="200">Autenticado. O token vai no corpo da resposta.</response>
    /// <response code="401">Credenciais inválidas.</response>
    /// <response code="429">Tentativas demais em pouco tempo.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasDeRateLimit.Login)]
    [ProducesResponseType(typeof(AutenticacaoResposta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AutenticacaoResposta>> LoginAsync(
        [FromBody] AutenticarUsuarioComando comando,
        CancellationToken cancellationToken)
    {
        var resposta = await autenticarUsuario.ExecutarAsync(comando, cancellationToken);

        return Ok(resposta);
    }
}
