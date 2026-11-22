using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Aplicacao.Modulos.Agendamentos;

namespace PontoBarber.Api.Modulos.Agendamentos;

/// <summary>
/// Operação idempotente de expiração de sugestões. O agendamento da execução
/// depende da decisão de scheduler (ADR-027); este endpoint é o ponto de
/// acionamento de qualquer adaptador futuro, sem inventar tecnologia agora.
/// </summary>
[ApiController]
[Route("api/v1/jobs")]
public sealed class ExpiracaoController(ServicoCicloVidaAgendamentos cicloVida) : ControllerBase
{
    [HttpPost("expirar-sugestoes")]
    [Authorize]
    public async Task<IResult> Expirar(CancellationToken cancellationToken)
    {
        var quantidade = await cicloVida.ExpirarSugestoesAsync(cancellationToken);
        return Results.Ok(new { expiradas = quantidade });
    }
}
