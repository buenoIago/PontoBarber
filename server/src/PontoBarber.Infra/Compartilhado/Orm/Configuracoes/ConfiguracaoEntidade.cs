using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

/// <summary>
/// Base para configurações concretas de entidades, aplicadas por
/// <see cref="ModelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly)"/>.
/// A chave é declarada por nome para atender tanto a
/// <see cref="EntidadeBase{T}"/> quanto a entidades sem comportamento de
/// atualização (perfil e vínculo profissional).
/// </summary>
public abstract class ConfiguracaoEntidade<TEntidade> : IEntityTypeConfiguration<TEntidade>
    where TEntidade : class
{
    public virtual void Configure(EntityTypeBuilder<TEntidade> builder)
    {
        builder.HasKey("Id");
        builder.Property("Id").ValueGeneratedNever();

        if (typeof(IPertencenteTenant).IsAssignableFrom(typeof(TEntidade)))
            builder.Property(nameof(IPertencenteTenant.BarbeariaId)).IsRequired();
    }
}
