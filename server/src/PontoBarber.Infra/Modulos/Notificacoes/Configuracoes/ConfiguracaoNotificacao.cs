using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Modulos.Notificacoes;
using PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

namespace PontoBarber.Infra.Modulos.Notificacoes.Configuracoes;

public sealed class ConfiguracaoNotificacao : ConfiguracaoEntidade<Notificacao>
{
    public override void Configure(EntityTypeBuilder<Notificacao> builder)
    {
        base.Configure(builder);

        builder.ToTable("Notificacoes");
        builder.Property(notificacao => notificacao.DestinatarioUsuarioId).HasMaxLength(450).IsRequired();
        builder.Property(notificacao => notificacao.Tipo).HasConversion<int>().IsRequired();
        builder.Property(notificacao => notificacao.Mensagem).HasMaxLength(500).IsRequired();
        builder.Property(notificacao => notificacao.EventId).IsRequired();
        builder.Property(notificacao => notificacao.CriadoEmUtc).IsRequired();

        builder.HasIndex(notificacao => new { notificacao.EventId, notificacao.DestinatarioUsuarioId })
            .IsUnique()
            .HasDatabaseName("UX_Notifications_Event_Recipient");
        builder.HasIndex(notificacao => new { notificacao.DestinatarioUsuarioId, notificacao.CriadoEmUtc })
            .HasDatabaseName("IX_Notifications_Recipient_Created");
        builder.HasIndex(notificacao => new { notificacao.DestinatarioUsuarioId, notificacao.LidoEmUtc })
            .HasDatabaseName("IX_Notifications_Recipient_Unread");
    }
}
