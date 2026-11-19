using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Infra.Compartilhado.Orm;

/// <summary>
/// Convenções de persistência compartilhadas por todas as entidades.
/// </summary>
public static class ConvencoesPersistencia
{
    private const string _tipoColunaUtc = "datetime2(3)";
    private const string _sufixoUtc = "Utc";

    private static DateTime NormalizarUtc(DateTime valor) => valor.Kind switch
    {
        DateTimeKind.Utc => valor,
        DateTimeKind.Local => valor.ToUniversalTime(),
        _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc)
    };

    private static readonly ValueConverter<DateTime, DateTime> _conversorUtc = new(
        valor => NormalizarUtc(valor),
        valor => NormalizarUtc(valor));

    private static readonly ValueConverter<DateTime?, DateTime?> _conversorUtcAnulavel = new(
        valor => valor.HasValue ? NormalizarUtc(valor.Value) : null,
        valor => valor.HasValue ? NormalizarUtc(valor.Value) : null);

    public static void Aplicar(ModelBuilder construtorModelo)
    {
        ArgumentNullException.ThrowIfNull(construtorModelo);

        foreach (var tipoEntidade in construtorModelo.Model.GetEntityTypes())
        {
            AplicarConvencaoTenant(tipoEntidade);
            AplicarConvencaoDataHora(tipoEntidade);
        }
    }

    private static void AplicarConvencaoTenant(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType tipoEntidade)
    {
        if (!typeof(IPertencenteTenant).IsAssignableFrom(tipoEntidade.ClrType))
            return;

        var tenantId = tipoEntidade.FindProperty(nameof(IPertencenteTenant.BarbeariaId));

        if (tenantId is null)
        {
            throw new InvalidOperationException(
                $"A entidade '{tipoEntidade.ClrType.FullName}' implementa IPertencenteTenant mas não possui a propriedade BarbeariaId.");
        }

        tenantId.IsNullable = false;
        tenantId.SetColumnName(nameof(IPertencenteTenant.BarbeariaId));
    }

    private static void AplicarConvencaoDataHora(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType tipoEntidade)
    {
        foreach (var propriedade in tipoEntidade.GetProperties())
        {
            if (propriedade.ClrType != typeof(DateTime) && propriedade.ClrType != typeof(DateTime?))
                continue;

            propriedade.SetColumnType(_tipoColunaUtc);

            if (!propriedade.Name.EndsWith(_sufixoUtc, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"A propriedade '{tipoEntidade.ClrType.FullName}.{propriedade.Name}' representa um instante " +
                    $"e deve terminar com '{_sufixoUtc}' (TDD §12.4).");
            }

            if (propriedade.ClrType == typeof(DateTime))
                propriedade.SetValueConverter(_conversorUtc);
            else
                propriedade.SetValueConverter(_conversorUtcAnulavel);
        }
    }
}
