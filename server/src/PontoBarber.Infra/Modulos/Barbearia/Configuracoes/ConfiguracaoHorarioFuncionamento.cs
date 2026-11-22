using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Barbearia.Configuracoes;

/// <summary>Mapeamento do funcionamento por dia da semana (TDD §8.3).</summary>
public sealed class ConfiguracaoHorarioFuncionamento : ConfiguracaoEntidade<HorarioFuncionamento>
{
    public override void Configure(EntityTypeBuilder<HorarioFuncionamento> builder)
    {
        base.Configure(builder);

        builder.ToTable("HorariosFuncionamento");
        builder.Property(horario => horario.DiaSemana).HasConversion<int>().IsRequired();

        builder.HasIndex(horario => new { horario.BarbeariaId, horario.DiaSemana })
            .IsUnique()
            .HasDatabaseName("UX_Horarios_Barbearia_Dia");
    }
}
