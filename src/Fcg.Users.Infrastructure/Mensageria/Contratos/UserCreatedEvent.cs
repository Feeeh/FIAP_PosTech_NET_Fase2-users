using System.Text.Json.Serialization;

namespace Fcg.Users.Infrastructure.Mensageria.Contratos;

/// <summary>
/// Usuário cadastrado no UsersAPI. Publicado pelo Users; consumido pelo Notifications para
/// enviar o e-mail de boas-vindas.
/// </summary>
/// <param name="EventId">Identificador único do evento.</param>
/// <param name="OccurredAt">Instante do cadastro.</param>
/// <param name="UserId">Usuário cadastrado.</param>
/// <param name="Name">Nome informado no cadastro.</param>
/// <param name="Email">E-mail do usuário, destino das boas-vindas.</param>
public sealed record UserCreatedEvent(
    [property: JsonRequired] Guid EventId,
    [property: JsonRequired] DateTimeOffset OccurredAt,
    [property: JsonRequired] Guid UserId,
    [property: JsonRequired] string Name,
    [property: JsonRequired] string Email) : IEventoDeIntegracao;
