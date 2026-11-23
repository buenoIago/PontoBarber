using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Financeiro;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Financeiro.Configuracoes;

public sealed class ConfiguracaoPagamento : ConfiguracaoEntidade<Pagamento>
{
    public override void Configure(EntityTypeBuilder<Pagamento> builder)
    {
        base.Configure(builder);

        builder.ToTable("Financeiro");
        builder.Property(pagamento => pagamento.AgendamentoId).IsRequired();
        builder.Property(pagamento => pagamento.Valor).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(pagamento => pagamento.Metodo).HasConversion<int>().IsRequired();
        builder.Property(pagamento => pagamento.RegistradoEmUtc).IsRequired();
        builder.Property(pagamento => pagamento.RegistradoPorUsuarioId).HasMaxLength(450).IsRequired();

        builder.HasIndex(pagamento => pagamento.AgendamentoId)
            .IsUnique()
            .HasDatabaseName("UX_Payments_Appointment");
        builder.HasIndex(pagamento => new { pagamento.BarbeariaId, pagamento.RegistradoEmUtc })
            .HasDatabaseName("IX_Payments_Tenant_Registered");
    }
}
