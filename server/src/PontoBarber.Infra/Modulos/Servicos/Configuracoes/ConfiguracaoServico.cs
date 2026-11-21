using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Servicos.Configuracoes;

/// <summary>Mapeamento do serviço do catálogo (TDD §8.3).</summary>
public sealed class ConfiguracaoServico : ConfiguracaoEntidade<Servico>
{
    public override void Configure(EntityTypeBuilder<Servico> builder)
    {
        base.Configure(builder);

        builder.ToTable("Servicos");
        builder.Property(servico => servico.Nome).HasMaxLength(120).IsRequired();
        builder.Property(servico => servico.DuracaoMinutos).IsRequired();
        builder.Property(servico => servico.Preco).HasColumnType("decimal(18,2)");

        builder.HasIndex(servico => new { servico.BarbeariaId, servico.Nome })
            .IsUnique()
            .HasDatabaseName("UX_Servicos_Barbearia_Nome");
    }
}
