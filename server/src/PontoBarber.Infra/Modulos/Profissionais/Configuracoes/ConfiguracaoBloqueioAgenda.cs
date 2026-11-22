using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Profissionais.Configuracoes;

/// <summary>Mapeamento do bloqueio pontual (TDD §8.3).</summary>
public sealed class ConfiguracaoBloqueioAgenda : ConfiguracaoEntidade<BloqueioAgenda>
{
    public override void Configure(EntityTypeBuilder<BloqueioAgenda> builder)
    {
        base.Configure(builder);

        builder.ToTable("BloqueiosAgenda");
        builder.Property(bloqueio => bloqueio.ProfissionalId).IsRequired();
        builder.Property(bloqueio => bloqueio.Motivo).HasMaxLength(300);

        builder.HasIndex(bloqueio => new
        {
            bloqueio.BarbeariaId,
            bloqueio.ProfissionalId,
            bloqueio.IniciaEmUtc
        }).HasDatabaseName("IX_Bloqueios_Barbearia_Profissional_Inicio");
    }
}
