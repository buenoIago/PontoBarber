using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia;
using PontoBarber.Aplicacao.Modulos.Disponibilidade;
using PontoBarber.Dominio.Compartilhado;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;

namespace PontoBarber.Aplicacao.Modulos.Agendamentos;

/// <summary>
/// Regras de funcionamento/disponibilidade compartilhadas por criação,
/// remarcação e sugestão. Conflitos com agendamentos, bloqueios e reservas
/// temporárias ficam no repositório, dentro da transação.
/// </summary>
public sealed class ValidadorHorarioAgendamento(
    IBarbeariaRepositorio barbearias,
    IHorarioFuncionamentoRepositorio horarios,
    IDisponibilidadeSemanalRepositorio disponibilidades)
{
    public async Task ValidarAsync(
        Guid barbeariaId,
        Guid profissionalId,
        DateTime inicioUtc,
        int duracaoMinutos,
        CancellationToken cancellationToken)
    {
        if (inicioUtc.Kind != DateTimeKind.Utc)
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["iniciaEmUtc"] = ["Informe o horário em UTC."]
            });

        if (inicioUtc <= DateTime.UtcNow)
            throw new ExcecaoHorarioAplicacao(
                CodigosErro.AgendaHorarioIndisponivel, "O horário informado já passou.");

        var barbearia = await barbearias.ObterPorIdAsync(barbeariaId, cancellationToken);
        if (barbearia is null)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Barbearia não encontrada.");

        var fuso = TimeZoneInfo.FindSystemTimeZoneById(barbearia.FusoHorarioId);
        var inicioLocal = TimeZoneInfo.ConvertTime(inicioUtc, fuso);
        var data = DateOnly.FromDateTime(inicioLocal);
        ConsultarDisponibilidade.ValidarJanela(
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuso)), data, null);

        var diaSemana = data.DayOfWeek == DayOfWeek.Sunday
            ? DiaSemana.Domingo
            : (DiaSemana)(int)data.DayOfWeek;
        var horaLocal = TimeOnly.FromDateTime(inicioLocal);
        var fimLocal = TimeOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            inicioUtc.AddMinutes(duracaoMinutos), fuso));

        var horario = (await horarios.ListarPorBarbeariaAsync(barbeariaId, cancellationToken))
            .FirstOrDefault(item => item.DiaSemana == diaSemana && !item.Fechado &&
                                    item.AbreAs.HasValue && item.FechaAs.HasValue);
        if (horario is null || horaLocal < horario.AbreAs || fimLocal > horario.FechaAs)
            throw new ExcecaoHorarioAplicacao(
                CodigosErro.AgendaForaFuncionamento, "O horário está fora do funcionamento da barbearia.");

        var janelas = await disponibilidades.ListarAsync(barbeariaId, profissionalId, cancellationToken);
        if (!janelas.Any(janela => janela.DiaSemana == diaSemana &&
                                   horaLocal >= janela.IniciaEm && fimLocal <= janela.TerminaEm))
            throw new ExcecaoHorarioAplicacao(
                CodigosErro.AgendaForaDisponibilidade, "O horário está fora da disponibilidade do profissional.");
    }

    /// <summary>Chave de lock por profissional e data no fuso da barbearia.</summary>
    public async Task<string> ObterChaveLockAsync(
        Guid barbeariaId,
        Guid profissionalId,
        DateTime inicioUtc,
        CancellationToken cancellationToken)
    {
        var barbearia = await barbearias.ObterPorIdAsync(barbeariaId, cancellationToken);
        if (barbearia is null)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Barbearia não encontrada.");

        var fuso = TimeZoneInfo.FindSystemTimeZoneById(barbearia.FusoHorarioId);
        var data = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(inicioUtc, fuso));
        return $"pb:{barbeariaId}:{profissionalId}:{data:yyyy-MM-dd}";
    }
}
