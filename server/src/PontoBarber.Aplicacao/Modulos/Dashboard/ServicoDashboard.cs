using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia;
using PontoBarber.Aplicacao.Modulos.Dashboard.DTOs;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Dashboard;
using PontoBarber.Dominio.Modulos.Profissionais;

namespace PontoBarber.Aplicacao.Modulos.Dashboard;

/// <summary>
/// Indicadores calculados por consultas agregadas no banco, sem tabela de
/// saldo nem cache em memória. Todos os períodos usam o fuso da barbearia.
/// </summary>
public sealed class ServicoDashboard(
    IDashboardRepositorio repositorio,
    IBarbeariaRepositorio barbearias,
    IProfissionalRepositorio profissionais)
{
    private const int _limiteProximos = 10;

    public async Task<DashboardResponse> ObterAsync(
        Guid barbeariaId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirProprietarioAsync(barbeariaId, usuarioId, cancellationToken);

        var barbearia = await barbearias.ObterPorIdAsync(barbeariaId, cancellationToken)
            ?? throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Barbearia não encontrada.");

        var fuso = TimeZoneInfo.FindSystemTimeZoneById(barbearia.FusoHorarioId);
        var agoraUtc = DateTime.UtcNow;
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(agoraUtc, fuso));

        var dia = PeriodosDashboard.Dia(hoje, fuso);
        var semana = PeriodosDashboard.SemanaIso(hoje, fuso);
        var mes = PeriodosDashboard.Mes(hoje, fuso);
        var todasSituacoes = Enum.GetValues<SituacaoAgendamento>().ToList();

        // As consultas compartilham o mesmo DbContext escoped; executá-las em
        // paralelo geraria "A second operation was started on this context".
        var quantidadeHoje = await repositorio.ContarAgendamentosAsync(
            barbeariaId, dia, todasSituacoes, cancellationToken);
        var semanaQuantidade = await repositorio.ContarAgendamentosAsync(
            barbeariaId, semana, todasSituacoes, cancellationToken);
        var cancelamentos = await repositorio.ContarCancelamentosAsync(
            barbeariaId, mes, cancellationToken);
        var recebido = await repositorio.SomarPagamentosAsync(barbeariaId, mes, cancellationToken);
        var proximos = await repositorio.ListarProximosAsync(
            barbeariaId, agoraUtc, _limiteProximos, cancellationToken);

        return new DashboardResponse(
            quantidadeHoje,
            semanaQuantidade,
            cancelamentos,
            recebido,
            proximos.Select(item => new ProximoAgendamentoResponse(
                item.AgendamentoId, item.IniciaEmUtc, item.NomeCliente, item.NomeProfissional,
                item.Situacao.ToString())).ToList());
    }

    private async Task ExigirProprietarioAsync(
        Guid barbeariaId, string usuarioId, CancellationToken cancellationToken)
    {
        var perfil = await profissionais.ObterPorUsuarioAsync(usuarioId, cancellationToken);
        if (perfil is null)
            throw AcessoNegado();

        var vinculo = await profissionais.ObterVinculoAsync(barbeariaId, perfil.Id, cancellationToken);
        if (vinculo is null || vinculo.Situacao != StatusVinculo.Ativo ||
            vinculo.Cargo != CargoProfissional.Proprietario)
            throw AcessoNegado();
    }

    private static ExcecaoAutorizacaoAplicacao AcessoNegado() =>
        new(CodigosErro.TenantNegado, "Somente o proprietário pode acessar o dashboard.");
}
