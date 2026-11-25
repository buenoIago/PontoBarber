using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Idempotencia;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Idempotencia.Configuracoes;

public sealed class ConfiguracaoRegistroIdempotencia : ConfiguracaoEntidade<RegistroIdempotencia>
{
    public override void Configure(EntityTypeBuilder<RegistroIdempotencia> builder)
    {
        base.Configure(builder);

        builder.ToTable("RegistrosIdempotencia");
        builder.Property(registro => registro.UsuarioId).IsRequired();
        builder.Property(registro => registro.Chave).HasMaxLength(200).IsRequired();
        builder.Property(registro => registro.HashRequisicao).HasMaxLength(200).IsRequired();
        builder.Property(registro => registro.JsonResposta).HasMaxLength(4000);
        builder.Property(registro => registro.CriadoEmUtc).IsRequired();
        builder.Property(registro => registro.ExpiraEmUtc).IsRequired();

        builder.HasIndex(registro => new { registro.UsuarioId, registro.Chave })
            .IsUnique()
            .HasDatabaseName("UX_Idempotency_User_Key");
        builder.HasIndex(registro => registro.ExpiraEmUtc)
            .HasDatabaseName("IX_Idempotency_Expires");
    }
}
