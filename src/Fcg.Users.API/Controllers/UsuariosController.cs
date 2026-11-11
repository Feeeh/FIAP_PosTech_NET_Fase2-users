using Fcg.Users.Application.Consultas;
using Fcg.Users.Application.IdentidadeAcesso;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fcg.Users.API.Controllers;

/// <summary>Cadastro e administração de usuários (RF01, RF02, RF06).</summary>
[ApiController]
[Route("api/usuarios")]
[Produces("application/json")]
[Authorize]
public sealed class UsuariosController(
    CadastrarUsuario cadastrarUsuario,
    PromoverAAdministrador promoverAAdministrador,
    DesativarUsuario desativarUsuario,
    IConsultasDeUsuarios consultasDeUsuarios,
    ILogger<UsuariosController> logger) : ControllerBase
{
    /// <summary>Cadastra um novo usuário na plataforma.</summary>
    /// <remarks>
    /// A senha precisa ter no mínimo 8 caracteres, com ao menos uma letra, um número e
    /// um caractere especial. Quando ela não atende, a resposta diz exatamente o que falta.
    /// <para>Todo usuário nasce com o perfil Usuário; a promoção a Administrador é um passo à parte.</para>
    /// </remarks>
    /// <response code="201">Usuário cadastrado.</response>
    /// <response code="400">E-mail em formato inválido ou senha fora da política.</response>
    /// <response code="409">Já existe um usuário com este e-mail.</response>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(PoliticasDeRateLimit.Cadastro)]
    [ProducesResponseType(typeof(UsuarioCadastradoResposta), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioCadastradoResposta>> CadastrarAsync(
        [FromBody] CadastrarUsuarioComando comando,
        CancellationToken cancellationToken)
    {
        var resposta = await cadastrarUsuario.ExecutarAsync(comando, cancellationToken);

        logger.LogInformation("Usuário cadastrado: {UsuarioId}.", resposta.UsuarioId);

        return CreatedAtAction(nameof(ObterMeuPerfilAsync), null, resposta);
    }

    /// <summary>Devolve o perfil do usuário do token.</summary>
    /// <response code="200">Perfil do usuário autenticado.</response>
    [HttpGet("eu")]
    [ProducesResponseType(typeof(MeuPerfil), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MeuPerfil>> ObterMeuPerfilAsync(CancellationToken cancellationToken)
    {
        var usuarioId = UsuarioAutenticado.ObterId(User);
        var perfil = await consultasDeUsuarios.ObterMeuPerfilAsync(usuarioId, cancellationToken);

        return perfil is null ? NotFound() : Ok(perfil);
    }

    /// <summary>Lista os usuários cadastrados. Exclusivo do Administrador.</summary>
    /// <param name="cancellationToken">Token de cancelamento da requisição.</param>
    /// <param name="pagina">Página desejada, começando em 1.</param>
    /// <param name="tamanhoDaPagina">Itens por página, no máximo 100.</param>
    /// <response code="200">Página de usuários.</response>
    /// <response code="403">O token é de um perfil Usuário.</response>
    [HttpGet]
    [Authorize(Roles = UsuarioAutenticado.PerfilAdministrador)]
    [ProducesResponseType(typeof(ResultadoPaginado<UsuarioNaLista>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ResultadoPaginado<UsuarioNaLista>>> ListarAsync(
        CancellationToken cancellationToken,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamanhoDaPagina = 20)
    {
        var usuarios = await consultasDeUsuarios.ListarUsuariosAsync(pagina, tamanhoDaPagina, cancellationToken);

        return Ok(usuarios);
    }

    /// <summary>Promove um usuário a Administrador. Exclusivo do Administrador.</summary>
    /// <response code="204">Perfil alterado.</response>
    /// <response code="404">Usuário não encontrado.</response>
    [HttpPatch("{usuarioId:guid}/perfil")]
    [Authorize(Roles = UsuarioAutenticado.PerfilAdministrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PromoverAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        await promoverAAdministrador.ExecutarAsync(usuarioId, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Desativa um usuário. Ele deixa de autenticar, mas o histórico dele é preservado.
    /// Exclusivo do Administrador.
    /// </summary>
    /// <response code="204">Usuário desativado.</response>
    /// <response code="404">Usuário não encontrado.</response>
    [HttpPost("{usuarioId:guid}/desativacao")]
    [Authorize(Roles = UsuarioAutenticado.PerfilAdministrador)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DesativarAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        await desativarUsuario.ExecutarAsync(usuarioId, cancellationToken);

        return NoContent();
    }
}
