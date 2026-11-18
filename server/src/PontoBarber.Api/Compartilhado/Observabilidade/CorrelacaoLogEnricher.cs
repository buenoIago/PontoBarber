using System.Security.Claims;
using System.Security.Cryptography;
using PontoBarber.Api.Compartilhado.Tenant;
using Serilog.Core;
using Serilog.Events;

namespace PontoBarber.Api.Compartilhado.Observabilidade;

/// <summary>
/// Adiciona os dados de correlação da requisição e mascara propriedades
/// sensíveis antes de qualquer sink receber o evento.
/// </summary>
public sealed class CorrelacaoLogEnricher(IHttpContextAccessor accessor) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        Redacao(logEvent, propertyFactory);

        var contexto = accessor.HttpContext;
        if (contexto is null)
            return;

        Adicionar(logEvent, propertyFactory, "TraceId", contexto.TraceIdentifier);
        Adicionar(logEvent, propertyFactory, "RequestId", contexto.TraceIdentifier);

        var spanId = System.Diagnostics.Activity.Current?.SpanId.ToString();
        if (!string.IsNullOrWhiteSpace(spanId))
            Adicionar(logEvent, propertyFactory, "SpanId", spanId);

        var usuarioId = contexto.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(usuarioId))
            Adicionar(logEvent, propertyFactory, "UserId", Hash(usuarioId));

        if (contexto.Items.TryGetValue(ChavesContextoTenant.TenantId, out var tenant) && tenant is Guid tenantId)
            Adicionar(logEvent, propertyFactory, "TenantId", tenantId);
    }

    private static void Redacao(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        foreach (var nome in logEvent.Properties.Keys.Where(RedacaoDadosSensiveis.EHNomeSensivel).ToList())
        {
            logEvent.AddOrUpdateProperty(
                propertyFactory.CreateProperty(nome, RedacaoDadosSensiveis.ValorMascarado));
        }
    }

    private static void Adicionar(
        LogEvent logEvent,
        ILogEventPropertyFactory propertyFactory,
        string nome,
        object valor) =>
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(nome, valor));

    private static string Hash(string valor)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(valor));
        return Convert.ToHexString(bytes.AsSpan(0, 16));
    }
}
