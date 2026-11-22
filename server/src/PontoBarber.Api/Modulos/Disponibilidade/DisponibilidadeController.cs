using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PontoBarber.Aplicacao.Modulos.Disponibilidade;

namespace PontoBarber.Api.Modulos.Disponibilidade;

/// <summary>Horários livres por serviço, profissional e data (TDD §10.2).</summary>
[ApiController]
[Route("api/v1/barbearias/{barbeariaId:guid}/disponibilidade")]
public sealed class DisponibilidadeController(ConsultarDisponibilidade servico) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IResult> Consultar(
        Guid barbeariaId,
        [FromQuery] Guid servicoId,
        [FromQuery] Guid? profissionalId,
        [FromQuery] DateOnly date,
        [FromQuery] int? dias,
        CancellationToken cancellationToken) =>
        Results.Ok(await servico.ConsultarAsync(
            barbeariaId, servicoId, profissionalId, date, dias, cancellationToken));
}
