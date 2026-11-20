using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

namespace PontoBarber.Infra.Modulos.Barbearia.Configuracoes;

/// <summary>
/// Mapeamento da barbearia, incluindo o índice único global do convite
/// (TDD §8.3: UX_Barbearias_CodigoConvite).
/// </summary>
public sealed class ConfiguracaoBarbearia : ConfiguracaoEntidade<BarbeariaEntidade>
{
    public override void Configure(EntityTypeBuilder<BarbeariaEntidade> builder)
    {
        base.Configure(builder);

        builder.ToTable("Barbearias");

        builder.Property(entidade => entidade.Nome).HasMaxLength(200).IsRequired();
        builder.Property(entidade => entidade.TelefoneComercial).HasMaxLength(20).IsRequired();
        builder.Property(entidade => entidade.EmailComercial).HasMaxLength(256).IsRequired();
        builder.Property(entidade => entidade.Cidade).HasMaxLength(120).IsRequired();
        builder.Property(entidade => entidade.Endereco).HasMaxLength(300).IsRequired();
        builder.Property(entidade => entidade.Instagram).HasMaxLength(100);
        builder.Property(entidade => entidade.FusoHorarioId).HasMaxLength(100).IsRequired();

        builder.Property(entidade => entidade.CodigoConvite)
            .HasMaxLength(8)
            .IsFixedLength()
            .IsRequired();

        builder.HasIndex(entidade => entidade.CodigoConvite)
            .IsUnique()
            .HasDatabaseName("UX_Barbearias_CodigoConvite");

        builder.Property(entidade => entidade.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();
    }
}
