using System.Reflection;
using Serilog.Core;
using Serilog.Events;

namespace PontoBarber.Api.Compartilhado.Logging;

/// <summary>
/// Mascara propriedades sensíveis ao desconstruir objetos em logs estruturados.
/// </summary>
public sealed class PoliticaRedacaoSensiveis : IDestructuringPolicy
{
    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        var tipo = value.GetType();

        if (value is string || tipo.IsPrimitive || tipo.IsEnum || value is DateTime || value is Guid)
        {
            result = null!;
            return false;
        }

        var propriedades = tipo
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(propriedade => propriedade.CanRead && propriedade.GetIndexParameters().Length == 0)
            .Select(propriedade =>
            {
                var valor = propriedade.GetValue(value);
                valor = RedacaoDadosSensiveis.MascararValor(propriedade.Name, valor);
                return new LogEventProperty(
                    propriedade.Name,
                    propertyValueFactory.CreatePropertyValue(valor, destructureObjects: true));
            })
            .ToList();

        result = new StructureValue(propriedades, tipo.Name);
        return true;
    }
}
