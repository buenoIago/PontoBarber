using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Agendamentos.Configuracoes;

/// <summary>Mapeamento do histórico append-only (TDD §8.3).</summary>
public sealed class ConfiguracaoHistoricoAgendamento : ConfiguracaoEntidade<HistoricoAgendamento>
{
    public override void Configure(EntityTypeBuilder<HistoricoAgendamento> builder)
    {
        base.Configure(builder);

        builder.ToTable("HistoricosAgendamento");
        builder.Property(evento => evento.AgendamentoId).IsRequired();
        builder.Property(evento => evento.TipoEvento).HasConversion<int>().IsRequired();
        builder.Property(evento => evento.AtorUsuarioId).HasMaxLength(450);
        builder.Property(evento => evento.OcorridoEmUtc).IsRequired();
        builder.Property(evento => evento.Detalhes).HasMaxLength(500);

        builder.HasIndex(evento => new { evento.AgendamentoId, evento.OcorridoEmUtc })
            .HasDatabaseName("IX_History_Appointment_Occurred");
    }
}
