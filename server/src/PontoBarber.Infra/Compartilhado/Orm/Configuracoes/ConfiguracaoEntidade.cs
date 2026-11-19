using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Infra.Compartilhado.Orm.Configuracoes;

/// <summary>
/// Base para configurações concretas de entidades, aplicadas por
/// <see cref="ModelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly)"/>.
/// </summary>
public abstract class ConfiguracaoEntidade<TEntidade> : IEntityTypeConfiguration<TEntidade>
    where TEntidade : class, IEntidade
{
    public virtual void Configure(EntityTypeBuilder<TEntidade> builder)
    {
        builder.HasKey(entidade => entidade.Id);
        builder.Property(entidade => entidade.Id).ValueGeneratedNever();

        if (typeof(IPertencenteTenant).IsAssignableFrom(typeof(TEntidade)))
            builder.Property(nameof(IPertencenteTenant.BarbeariaId)).IsRequired();
    }
}
