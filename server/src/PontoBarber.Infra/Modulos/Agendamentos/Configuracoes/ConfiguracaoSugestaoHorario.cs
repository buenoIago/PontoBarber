using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Agendamentos.Configuracoes;

/// <summary>Mapeamento da sugestão e da reserva temporária (TDD §8.3).</summary>
public sealed class ConfiguracaoSugestaoHorario : ConfiguracaoEntidade<SugestaoHorario>
{
    public override void Configure(EntityTypeBuilder<SugestaoHorario> builder)
    {
        base.Configure(builder);

        builder.ToTable("SugestoesHorario");
        builder.Property(sugestao => sugestao.AgendamentoId).IsRequired();
        builder.Property(sugestao => sugestao.ProfissionalId).IsRequired();
        builder.Property(sugestao => sugestao.Motivo).HasMaxLength(500).IsRequired();
        builder.Property(sugestao => sugestao.Situacao).HasConversion<int>().IsRequired();
        builder.Property(sugestao => sugestao.ExpiraEmUtc).IsRequired();
        builder.Property(sugestao => sugestao.AutorUsuarioId).IsRequired();

        builder.HasIndex(sugestao => new { sugestao.AgendamentoId, sugestao.Situacao })
            .IsUnique()
            .HasDatabaseName("UX_Suggestions_Appointment_Pending")
            .HasFilter("\"Situacao\" = 0");
        builder.HasIndex(sugestao => new { sugestao.Situacao, sugestao.ExpiraEmUtc })
            .HasDatabaseName("IX_Suggestions_Status_Expires");

        builder.HasOne<BarbeariaEntidade>()
            .WithMany()
            .HasForeignKey(sugestao => sugestao.BarbeariaId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Agendamento>()
            .WithMany()
            .HasForeignKey(sugestao => new { sugestao.BarbeariaId, sugestao.AgendamentoId })
            .HasPrincipalKey(agendamento => new { agendamento.BarbeariaId, agendamento.Id })
            .OnDelete(DeleteBehavior.NoAction);
    }
}
