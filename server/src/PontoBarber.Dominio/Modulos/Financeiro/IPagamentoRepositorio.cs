using PontoBarber.Dominio.Modulos.Idempotencia;

namespace PontoBarber.Dominio.Modulos.Financeiro;

public enum ResultadoPagamento
{
    Sucesso,
    Duplicado,
    ChaveEmConflito
}

/// <summary>
/// Persistência do pagamento e do registro de idempotência na mesma transação.
/// </summary>
public interface IPagamentoRepositorio
{
    Task<Pagamento?> ObterPorAgendamentoAsync(
        Guid barbeariaId, Guid agendamentoId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, Pagamento>> ObterPorAgendamentosAsync(
        Guid barbeariaId,
        IReadOnlyCollection<Guid> agendamentoIds,
        CancellationToken cancellationToken);

    Task<RegistroIdempotencia?> ObterIdempotenciaAsync(
        string usuarioId, string chave, CancellationToken cancellationToken);

    Task RemoverIdempotenciaExpiradaAsync(
        RegistroIdempotencia registro, CancellationToken cancellationToken);

    Task<ResultadoPagamento> RegistrarAsync(
        Pagamento pagamento,
        RegistroIdempotencia registro,
        string autorUsuarioId,
        CancellationToken cancellationToken);
}
