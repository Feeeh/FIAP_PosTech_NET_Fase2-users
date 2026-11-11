using Fcg.Users.Domain.IdentidadeAcesso;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fcg.Users.Infrastructure.Persistencia.Configuracoes;

/// <summary>Mapeamento do agregado Usuario.</summary>
public sealed class ConfiguracaoDeUsuario : IEntityTypeConfiguration<Usuario>
{
    private const int TamanhoMaximoDoHash = 200;
    private const int TamanhoMaximoDoPerfil = 20;

    public void Configure(EntityTypeBuilder<Usuario> usuario)
    {
        usuario.ToTable("Usuarios");
        usuario.HasKey(entidade => entidade.Id);

        usuario.OwnsOne(entidade => entidade.Nome, MapearNome);
        usuario.OwnsOne(entidade => entidade.Email, MapearEmail);
        usuario.OwnsOne(entidade => entidade.SenhaHash, MapearSenha);

        // Texto mantém o perfil legível nas consultas de leitura.
        usuario.Property(entidade => entidade.Perfil)
            .HasConversion<string>()
            .HasMaxLength(TamanhoMaximoDoPerfil)
            .IsRequired();

        usuario.Property(entidade => entidade.Ativo).IsRequired();
        usuario.Property(entidade => entidade.DataDeCadastro).IsRequired();
        usuario.Ignore(entidade => entidade.EventosDeDominio);
    }

    private static void MapearNome(OwnedNavigationBuilder<Usuario, Nome> nome)
    {
        nome.Property(valorDoNome => valorDoNome.Valor)
            .HasColumnName("Nome")
            .HasMaxLength(Nome.TamanhoMaximo)
            .IsRequired();
    }

    private static void MapearEmail(OwnedNavigationBuilder<Usuario, Email> email)
    {
        email.Property(valorDoEmail => valorDoEmail.Valor)
            .HasColumnName("Email")
            .HasMaxLength(Email.TamanhoMaximo)
            .IsRequired();

        // Invariante "e-mail único" reforçada no banco, não só em memória.
        email.HasIndex(valorDoEmail => valorDoEmail.Valor).IsUnique();
    }

    private static void MapearSenha(OwnedNavigationBuilder<Usuario, SenhaHash> senhaHash)
    {
        senhaHash.Property(valorDoHash => valorDoHash.Valor)
            .HasColumnName("SenhaHash")
            .HasMaxLength(TamanhoMaximoDoHash)
            .IsRequired();
    }
}
