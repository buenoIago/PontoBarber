using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PontoBarber.Domain.Common;

namespace PontoBarber.Infrastructure.Persistence.Configurations;

/// <summary>
/// Base para configurações concretas de entidades, aplicadas por
/// <see cref="ModelBuilder.ApplyConfigurationsFromAssembly(System.Reflection.Assembly)"/>.
/// </summary>
public abstract class EntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : class, IEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();

        if (typeof(ITenantOwned).IsAssignableFrom(typeof(TEntity)))
            builder.Property(nameof(ITenantOwned.TenantId)).IsRequired();
    }
}
