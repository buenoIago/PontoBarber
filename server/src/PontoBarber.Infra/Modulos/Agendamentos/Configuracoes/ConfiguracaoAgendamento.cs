using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

namespace PontoBarber.Infra.Modulos.Agendamentos.Configuracoes;

/// <summary>Mapeamento do agendamento (TDD §8.3).</summary>
public sealed class ConfiguracaoAgendamento : ConfiguracaoEntidade<Agendamento>
{
    public override void Configure(EntityTypeBuilder<Agendamento> builder)
    {
        base.Configure(builder);

        builder.ToTable("Agendamentos");
        builder.Property(agendamento => agendamento.ClienteId).IsRequired();
        builder.Property(agendamento => agendamento.ProfissionalId).IsRequired();
        builder.Property(agendamento => agendamento.ServicoId).IsRequired();
        builder.Property(agendamento => agendamento.NomeServicoSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(agendamento => agendamento.DuracaoSnapshot).IsRequired();
        builder.Property(agendamento => agendamento.PrecoSnapshot).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(agendamento => agendamento.Observacoes).HasMaxLength(500);
        builder.Property(agendamento => agendamento.Origem).HasConversion<int>().IsRequired();
        builder.Property(agendamento => agendamento.Situacao).HasConversion<int>().IsRequired();
        builder.Property(agendamento => agendamento.CanceladoEmUtc);
        builder.Property(agendamento => agendamento.CanceladoPorUsuarioId);
        builder.Property(agendamento => agendamento.MotivoCancelamento).HasMaxLength(500);
        builder.Property(agendamento => agendamento.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(agendamento => new { agendamento.BarbeariaId, agendamento.ProfissionalId, agendamento.IniciaEmUtc })
            .HasDatabaseName("IX_Appointments_Tenant_Pro_Start");
        builder.HasIndex(agendamento => new { agendamento.ClienteId, agendamento.IniciaEmUtc })
            .HasDatabaseName("IX_Appointments_Customer_Start");
        builder.HasIndex(agendamento => new { agendamento.BarbeariaId, agendamento.ProfissionalId, agendamento.IniciaEmUtc })
            .IsUnique()
            .HasDatabaseName("UX_Appointments_Pro_Start_Active")
            // Cancelados (1) não ocupam o horário: o índice cobre somente ativos.
            .HasFilter("\"Situacao\" = 0");

        // Chave alternativa exigida pelas FKs compostas de domínio.
        builder.HasAlternateKey(agendamento => new { agendamento.BarbeariaId, agendamento.Id });

        builder.HasOne<BarbeariaEntidade>()
            .WithMany()
            .HasForeignKey(agendamento => agendamento.BarbeariaId)
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<VinculoProfissional>()
            .WithMany()
            .HasForeignKey(agendamento => new { agendamento.BarbeariaId, agendamento.ProfissionalId })
            .HasPrincipalKey(vinculo => new { vinculo.BarbeariaId, vinculo.ProfissionalId })
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne<Servico>()
            .WithMany()
            .HasForeignKey(agendamento => new { agendamento.BarbeariaId, agendamento.ServicoId })
            .HasPrincipalKey(servico => new { servico.BarbeariaId, servico.Id })
            .OnDelete(DeleteBehavior.NoAction);
    }
}
