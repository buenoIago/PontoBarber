using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

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

        builder.HasAlternateKey(vinculo => new { vinculo.BarbeariaId, vinculo.ProfissionalId })
            .HasName("UX_Vinculos_Barbearia_Profissional");

        builder.HasOne<BarbeariaEntidade>()
            .WithMany()
            .HasForeignKey(vinculo => vinculo.BarbeariaId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<PerfilProfissional>()
            .WithMany()
            .HasForeignKey(vinculo => vinculo.ProfissionalId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
