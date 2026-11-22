namespace PontoBarber.Aplicacao.Compartilhado;

/// <summary>
/// Catálogo de códigos de erro estáveis definido no TDD §11.1.
/// </summary>
public static class CodigosErro
{
    public const string AutenticacaoCredenciaisInvalidas = "auth.invalid_credentials";
    public const string AutenticacaoBloqueada = "auth.locked_out";
    public const string AutenticacaoNegada = "auth.forbidden";
    public const string TenantNaoEncontrado = "tenant.not_found";
    public const string TenantNegado = "tenant.forbidden";
    public const string TenantConviteConflito = "tenant.invite_conflict";
    public const string ValidacaoFalhou = "validation.failed";
    public const string AgendaHorarioIndisponivel = "schedule.slot_unavailable";
    public const string AgendaForaFuncionamento = "schedule.outside_business_hours";
    public const string AgendaForaDisponibilidade = "schedule.outside_availability";
    public const string AgendaBloqueada = "schedule.blocked";

    /// <summary>
    /// Código do comportamento de timeout de lock definido em TDD §5.5.1
    /// (503 com Retry-After), ausente da tabela de códigos de TDD §11.1.
    /// </summary>
    public const string AgendaLockTimeout = "schedule.lock_timeout";
    public const string AgendamentoEstadoInvalido = "appointment.invalid_state";
    public const string AgendamentoNaoIniciado = "appointment.not_started";
    public const string AgendamentoJaIniciado = "appointment.already_started";
    public const string PagamentoJaRegistrado = "payment.already_registered";
    public const string SugestaoExpirada = "suggestion.expired";
    public const string ConcorrenciaConflito = "concurrency.conflict";
    public const string IdempotenciaChaveAusente = "idempotency.missing_key";
    public const string LimiteTaxaExcedido = "rate_limit.exceeded";

    /// <summary>
    /// Código genérico necessário para cumprir o critério do ADR-017 de que
    /// todos os erros retornam ProblemDetails. Não consta no catálogo do TDD
    /// §11.1; inclusão documentada proposta na revisão da tarefa 1.6.
    /// </summary>
    public const string ErroInterno = "internal.error";
}
