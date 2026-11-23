using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Auth;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Auth.Configuracoes;

public sealed class ConfiguracaoCodigoOtp : ConfiguracaoEntidade<CodigoOtp>
{
    public override void Configure(EntityTypeBuilder<CodigoOtp> builder)
    {
        base.Configure(builder);

        builder.ToTable("CodigosOtp");
        builder.Property(codigo => codigo.Email).HasMaxLength(256).IsRequired();
        builder.Property(codigo => codigo.Salt).HasMaxLength(200).IsRequired();
        builder.Property(codigo => codigo.Hash).HasMaxLength(200).IsRequired();
        builder.Property(codigo => codigo.CriadoEmUtc).IsRequired();
        builder.Property(codigo => codigo.ExpiraEmUtc).IsRequired();

        builder.HasIndex(codigo => codigo.Email)
            .IsUnique()
            .HasDatabaseName("UX_CodigosOtp_Email");
    }
}
