using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Jobs;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Jobs.Configuracoes;

public sealed class ConfiguracaoExecucaoJob : ConfiguracaoEntidade<ExecucaoJob>
{
    public override void Configure(EntityTypeBuilder<ExecucaoJob> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExecucoesJob");
        builder.Property(execucao => execucao.NomeJob).HasMaxLength(120).IsRequired();
        builder.Property(execucao => execucao.IniciadoEmUtc).IsRequired();
        builder.Property(execucao => execucao.Resultado).HasConversion<int>().IsRequired();
        builder.Property(execucao => execucao.Erro).HasMaxLength(1000);

        builder.HasIndex(execucao => new { execucao.NomeJob, execucao.IniciadoEmUtc })
            .HasDatabaseName("IX_JobExecutions_Name_Start");
    }
}
