namespace Fcg.Users.Domain.IdentidadeAcesso;

/// <summary>
/// Os dois níveis de acesso da Fase 1. Conjunto fechado de valores, sem identidade
/// própria — por isso é modelado como enum (o "enum rico" do DDD.md 12.2).
/// </summary>
public enum Perfil
{
    /// <summary>Acesso à plataforma e à própria biblioteca de jogos.</summary>
    Usuario = 0,

    /// <summary>Além do acima: cadastra jogos, administra usuários e cria promoções.</summary>
    Administrador = 1
}
