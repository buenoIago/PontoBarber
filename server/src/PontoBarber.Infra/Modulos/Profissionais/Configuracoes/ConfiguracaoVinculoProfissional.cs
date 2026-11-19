using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Profissionais.Configuracoes;

/// <summary>
/// Mapeamento do vínculo profissional-barbearia (TDD §8.3).
/// </summary>
public sealed class ConfiguracaoVinculoProfissional : ConfiguracaoEntidade<VinculoProfissional>
{
    public override void Configure(EntityTypeBuilder<VinculoProfissional> builder)
    {
        base.Configure(builder);

        builder.ToTable("VinculosProfissionais");

        builder.Property(entidade => entidade.ProfissionalId).IsRequired();
        builder.Property(entidade => entidade.Cargo).HasConversion<int>().IsRequired();
        builder.Property(entidade => entidade.Situacao).HasConversion<int>().IsRequired();

        builder.HasIndex(vinculo => new { vinculo.BarbeariaId, vinculo.ProfissionalId })
            .IsUnique()
            .HasDatabaseName("UX_Vinculos_Barbearia_Profissional");
    }
}
