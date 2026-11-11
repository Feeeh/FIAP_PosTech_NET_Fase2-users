using Fcg.Users.Domain.IdentidadeAcesso.Eventos;
using Fcg.Users.Domain.SeedWork;
using Fcg.Users.Infrastructure.Mensageria.Contratos;

namespace Fcg.Users.Infrastructure.Mensageria;

/// <summary>Publica somente o cadastro; os demais fatos de Identity permanecem internos ao serviço.</summary>
public static class MapeadorDeEventosDeIntegracao
{
    public static MensagemDeSaida? Mapear(EventoDeDominio evento, string correlationId)
    {
        if (evento is not UsuarioCadastrado cadastro)
        {
            return null;
        }

        var contrato = new UserCreatedEvent(
            Guid.NewGuid(),
            cadastro.OcorridoEm,
            cadastro.UsuarioId,
            cadastro.Nome,
            cadastro.Email);

        return MensagemDeSaida.Criar(contrato, RotasDosEventos.UsuarioCriado, correlationId);
    }
}
