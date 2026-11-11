using Fcg.Users.Domain.SeedWork;

namespace Fcg.Users.Domain.IdentidadeAcesso.Eventos;

/// <summary>Um novo usuário passou a existir na plataforma.</summary>
public sealed record UsuarioCadastrado(Guid UsuarioId, string Nome, string Email) : EventoDeDominio;

/// <summary>O nível de acesso do usuário mudou (por exemplo, promoção a Administrador).</summary>
public sealed record PerfilAlterado(Guid UsuarioId, Perfil PerfilAnterior, Perfil PerfilAtual) : EventoDeDominio;

/// <summary>
/// Usuário desativado. Dispara a política "sempre que Usuário Desativado,
/// invalidar credenciais" — a partir daqui ele não autentica mais.
/// </summary>
public sealed record UsuarioDesativado(Guid UsuarioId) : EventoDeDominio;

/// <summary>
/// Evento pivotal do Event Storming: separa Identidade e Acesso dos demais contextos.
/// </summary>
public sealed record UsuarioAutenticado(Guid UsuarioId, Perfil Perfil) : EventoDeDominio;
