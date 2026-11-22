using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Disponibilidade.DTOs;
using PontoBarber.Dominio.Compartilhado;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;

namespace PontoBarber.Aplicacao.Modulos.Disponibilidade;

/// <summary>
/// Calcula os horários livres do dia usando funcionamento, disponibilidade
/// semanal e bloqueios (Task 6.4). Agendamentos existentes só poderão ser
/// considerados quando a seção 7 existir — pendência registrada em tasks.md.
/// </summary>
public sealed class ConsultarDisponibilidade(
    IBarbeariaRepositorio barbearias,
    IServicoRepositorio servicos,
    IProfissionalRepositorio profissionais,
    IHorarioFuncionamentoRepositorio horarios,
    IDisponibilidadeSemanalRepositorio disponibilidades,
    IBloqueioAgendaRepositorio bloqueios,
    IAgendamentoRepositorio agendamentos)
{
    private const int _janelaPadrao = 15;
    private const int _limiteMaximo = 30;

    public async Task<DisponibilidadeResponse> ConsultarAsync(
        Guid barbeariaId,
        Guid servicoId,
        Guid? profissionalId,
        DateOnly data,
        int? dias,
        CancellationToken cancellationToken)
    {
        var barbearia = await barbearias.ObterPorIdAsync(barbeariaId, cancellationToken);
        if (barbearia is null)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Barbearia não encontrada.");

        var fuso = ObterFuso(barbearia.FusoHorarioId);
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTime.UtcNow, fuso));
        ValidarJanela(hoje, data, dias);

        var servico = await servicos.ObterPorIdAsync(barbeariaId, servicoId, cancellationToken);
        if (servico is null || !servico.Ativo)
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["servicoId"] = ["Serviço ativo não encontrado nesta barbearia."]
            });

        var vinculos = await profissionais.ListarProfissionaisAsync(barbeariaId, cancellationToken);
        var alvos = vinculos
            .Where(item => item.Vinculo.Situacao == StatusVinculo.Ativo)
            .Where(item => profissionalId is null || item.Perfil.Id == profissionalId.Value)
            .ToList();

        if (profissionalId.HasValue && alvos.Count == 0)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Profissional não encontrado nesta barbearia.");

        var diaSemana = ParaDiaSemana(data.DayOfWeek);
        var horario = (await horarios.ListarPorBarbeariaAsync(barbeariaId, cancellationToken))
            .FirstOrDefault(item => item.DiaSemana == diaSemana &&
                                    !item.Fechado &&
                                    item.AbreAs.HasValue &&
                                    item.FechaAs.HasValue);

        if (horario is null || alvos.Count == 0)
            return new DisponibilidadeResponse(data, barbearia.FusoHorarioId, [], false);

        var inicioDiaUtc = ParaUtc(data, horario.AbreAs!.Value, fuso);
        var fimDiaUtc = ParaUtc(data, horario.FechaAs!.Value, fuso);
        var bloqueiosDoDia = await bloqueios.ListarPeriodoAsync(
            barbeariaId, inicioDiaUtc, fimDiaUtc, cancellationToken);
        var agendamentosDoDia = await agendamentos.ListarAtivosPorPeriodoAsync(
            barbeariaId,
            alvos.Select(alvo => alvo.Perfil.Id).ToList(),
            inicioDiaUtc,
            fimDiaUtc,
            cancellationToken);

        var slots = new List<SlotDisponibilidade>();
        foreach (var alvo in alvos)
        {
            var janelas = await disponibilidades.ListarAsync(
                barbeariaId, alvo.Perfil.Id, cancellationToken);

            foreach (var janela in janelas.Where(item => item.DiaSemana == diaSemana))
            {
                var inicioMinuto = Math.Max(horario.AbreAs!.Value.Hour * 60 + horario.AbreAs.Value.Minute,
                    janela.IniciaEm.Hour * 60 + janela.IniciaEm.Minute);
                var fimMinuto = Math.Min(horario.FechaAs!.Value.Hour * 60 + horario.FechaAs.Value.Minute,
                    janela.TerminaEm.Hour * 60 + janela.TerminaEm.Minute);

                for (var minuto = inicioMinuto; minuto + servico.DuracaoMinutos <= fimMinuto;
                     minuto += servico.DuracaoMinutos)
                {
                    var hora = TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minuto));
                    var inicioSlot = ParaUtc(data, hora, fuso);
                    var fimSlot = ParaUtc(
                        data,
                        TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minuto + servico.DuracaoMinutos)),
                        fuso);

                    if (inicioSlot == DateTime.MinValue || fimSlot == DateTime.MinValue)
                        continue;

                    if (bloqueiosDoDia.Any(bloqueio => bloqueio.ProfissionalId == alvo.Perfil.Id &&
                                                     inicioSlot < bloqueio.TerminaEmUtc &&
                                                     fimSlot > bloqueio.IniciaEmUtc))
                        continue;

                    if (agendamentosDoDia.Any(agendamento => agendamento.ProfissionalId == alvo.Perfil.Id &&
                                                          inicioSlot < agendamento.TerminaEmUtc &&
                                                          fimSlot > agendamento.IniciaEmUtc))
                        continue;

                    slots.Add(new SlotDisponibilidade(
                        alvo.Perfil.Id, inicioSlot, fimSlot, $"{data:yyyy-MM-dd} {hora:HH:mm}"));
                }
            }
        }

        var distintos = slots
            .GroupBy(slot => new { slot.ProfissionalId, slot.StartsAtUtc })
            .Select(grupo => grupo.First())
            .OrderBy(slot => slot.ProfissionalId)
            .ThenBy(slot => slot.StartsAtUtc)
            .ToList();

        return new DisponibilidadeResponse(data, barbearia.FusoHorarioId, distintos, distintos.Count > 0);
    }

    /// <summary>Valida a janela padrão de 15 dias e o limite máximo de 30 dias.</summary>
    public static void ValidarJanela(DateOnly hoje, DateOnly data, int? dias)
    {
        var diasConsulta = dias ?? _janelaPadrao;
        if (diasConsulta < 1 || diasConsulta > _limiteMaximo)
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["dias"] = [$"Informe uma janela entre 1 e {_limiteMaximo} dias."]
            });

        if (data < hoje || data > hoje.AddDays(diasConsulta))
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["data"] = [$"A data deve estar entre {hoje:yyyy-MM-dd} e {hoje.AddDays(diasConsulta):yyyy-MM-dd}."]
            });
    }

    private static TimeZoneInfo ObterFuso(string fusoHorarioId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(fusoHorarioId);
        }
        catch (Exception excecao) when (excecao is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["fusoHorarioId"] = ["Fuso horário da barbearia inválido."]
            });
        }
    }

    private static DateTime ParaUtc(DateOnly data, TimeOnly hora, TimeZoneInfo fuso)
    {
        var local = DateTime.SpecifyKind(data.ToDateTime(hora), DateTimeKind.Unspecified);
        return fuso.IsInvalidTime(local) ? DateTime.MinValue : TimeZoneInfo.ConvertTimeToUtc(local, fuso);
    }

    private static DiaSemana ParaDiaSemana(DayOfWeek dia) =>
        dia == DayOfWeek.Sunday ? DiaSemana.Domingo : (DiaSemana)(int)dia;
}
