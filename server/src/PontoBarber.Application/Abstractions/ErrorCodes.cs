namespace PontoBarber.Application.Abstractions;

/// <summary>
/// Catálogo de códigos de erro estáveis definido no TDD §11.1.
/// </summary>
public static class ErrorCodes
{
    public const string AuthInvalidCredentials = "auth.invalid_credentials";
    public const string AuthLockedOut = "auth.locked_out";
    public const string AuthForbidden = "auth.forbidden";
    public const string TenantNotFound = "tenant.not_found";
    public const string TenantForbidden = "tenant.forbidden";
    public const string TenantInviteConflict = "tenant.invite_conflict";
    public const string ValidationFailed = "validation.failed";
    public const string ScheduleSlotUnavailable = "schedule.slot_unavailable";
    public const string ScheduleOutsideBusinessHours = "schedule.outside_business_hours";
    public const string ScheduleOutsideAvailability = "schedule.outside_availability";
    public const string ScheduleBlocked = "schedule.blocked";
    public const string AppointmentInvalidState = "appointment.invalid_state";
    public const string AppointmentNotStarted = "appointment.not_started";
    public const string AppointmentAlreadyStarted = "appointment.already_started";
    public const string PaymentAlreadyRegistered = "payment.already_registered";
    public const string SuggestionExpired = "suggestion.expired";
    public const string ConcurrencyConflict = "concurrency.conflict";
    public const string IdempotencyMissingKey = "idempotency.missing_key";
    public const string RateLimitExceeded = "rate_limit.exceeded";

    /// <summary>
    /// Código genérico necessário para cumprir o critério do ADR-017 de que
    /// todos os erros retornam ProblemDetails. Não consta no catálogo do TDD
    /// §11.1; inclusão documentada proposta na revisão da tarefa 1.6.
    /// </summary>
    public const string InternalError = "internal.error";
}
