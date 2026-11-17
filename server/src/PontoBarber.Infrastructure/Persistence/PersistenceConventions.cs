using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PontoBarber.Domain.Common;

namespace PontoBarber.Infrastructure.Persistence;

/// <summary>
/// Convenções de persistência compartilhadas por todas as entidades.
/// </summary>
public static class PersistenceConventions
{
    private const string _utcColumnType = "datetime2(3)";
    private const string _utcSuffix = "Utc";

    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static readonly ValueConverter<DateTime, DateTime> _utcConverter = new(
        value => NormalizeUtc(value),
        value => NormalizeUtc(value));

    private static readonly ValueConverter<DateTime?, DateTime?> _nullableUtcConverter = new(
        value => value.HasValue ? NormalizeUtc(value.Value) : null,
        value => value.HasValue ? NormalizeUtc(value.Value) : null);

    public static void Apply(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            ApplyTenantConvention(entityType);
            ApplyDateTimeConvention(entityType);
        }
    }

    private static void ApplyTenantConvention(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
    {
        if (!typeof(ITenantOwned).IsAssignableFrom(entityType.ClrType))
            return;

        var tenantId = entityType.FindProperty(nameof(ITenantOwned.TenantId));

        if (tenantId is null)
        {
            throw new InvalidOperationException(
                $"A entidade '{entityType.ClrType.FullName}' implementa ITenantOwned mas não possui a propriedade TenantId.");
        }

        tenantId.IsNullable = false;
        tenantId.SetColumnName(nameof(ITenantOwned.TenantId));
    }

    private static void ApplyDateTimeConvention(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entityType)
    {
        foreach (var property in entityType.GetProperties())
        {
            if (property.ClrType != typeof(DateTime) && property.ClrType != typeof(DateTime?))
                continue;

            property.SetColumnType(_utcColumnType);

            if (!property.Name.EndsWith(_utcSuffix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"A propriedade '{entityType.ClrType.FullName}.{property.Name}' representa um instante " +
                    $"e deve terminar com '{_utcSuffix}' (TDD §12.4).");
            }

            if (property.ClrType == typeof(DateTime))
                property.SetValueConverter(_utcConverter);
            else
                property.SetValueConverter(_nullableUtcConverter);
        }
    }
}
