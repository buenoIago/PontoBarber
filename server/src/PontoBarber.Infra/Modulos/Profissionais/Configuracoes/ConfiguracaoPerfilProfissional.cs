using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Profissionais.Configuracoes;

/// <summary>
/// Mapeamento do perfil profissional (TDD §8.1/§8.3).
/// </summary>
public sealed class ConfiguracaoPerfilProfissional : ConfiguracaoEntidade<PerfilProfissional>
{
    public override void Configure(EntityTypeBuilder<PerfilProfissional> builder)
    {
        base.Configure(builder);

        builder.ToTable("Profissionais");

        builder.Property(entidade => entidade.UsuarioId).HasMaxLength(450).IsRequired();
        builder.Property(entidade => entidade.NomeCompleto).HasMaxLength(200).IsRequired();
        builder.Property(entidade => entidade.Telefone).HasMaxLength(20);

        builder.HasIndex(entidade => entidade.UsuarioId)
            .IsUnique()
            .HasDatabaseName("UX_Profissionais_UsuarioId");
    }
}
