using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Agendamentos.DTOs;
using PontoBarber.Aplicacao.Modulos.Barbearia;
using PontoBarber.Dominio.Compartilhado;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Financeiro;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;

namespace PontoBarber.Aplicacao.Modulos.Agendamentos;

/// <summary>
/// Criação pública e manual, consulta por perfil. As regras de reserva são
/// compartilhadas pelos dois fluxos; a integridade é garantida no repositório.
/// </summary>
public sealed class ServicoAgendamentos(
    IAgendamentoRepositorio repositorio,
    IIdentidade identidade,
    IProfissionalRepositorio profissionais,
    IServicoRepositorio servicos,
    IBarbeariaRepositorio barbearias,
    ValidadorHorarioAgendamento validador,
    IPagamentoRepositorio pagamentos)
{
    public async Task<AgendamentoResponse> CriarPublicoAsync(
        Guid barbeariaId,
        CriarAgendamentoRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var conta = await identidade.ObterContaAsync(usuarioId, cancellationToken);
        if (conta is null || conta.Tipo != ServicoConta.TipoCliente)
            throw AcessoNegado();

        // Esta validação é do caso de uso público: o agendamento manual da
        // barbearia e o histórico não são afetados pela desativação.
        var barbearia = await barbearias.ObterPorIdAsync(barbeariaId, cancellationToken);
        if (barbearia is null)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Barbearia não encontrada.");
        if (!barbearia.Ativo)
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["barbeariaId"] = ["Barbearia inativa não recebe novos agendamentos."]
            });

        return await ConfirmarAsync(
            barbeariaId, request, conta.Id, OrigemAgendamento.Publico, null, cancellationToken);
    }

    public async Task<AgendamentoResponse> CriarManualAsync(
        Guid barbeariaId,
        CriarAgendamentoRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var perfil = await profissionais.ObterPorUsuarioAsync(usuarioId, cancellationToken);
        if (perfil is null)
            throw AcessoNegado();

        var vinculo = await profissionais.ObterVinculoAsync(barbeariaId, perfil.Id, cancellationToken);
        if (vinculo is null || vinculo.Situacao != StatusVinculo.Ativo)
            throw AcessoNegado();

        if (vinculo.Cargo != CargoProfissional.Proprietario && perfil.Id != request.ProfissionalId)
            throw AcessoNegado("Um barbeiro só pode agendar na própria agenda.");

        var clienteId = request.ClienteId;
        if (string.IsNullOrWhiteSpace(clienteId))
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["clienteId"] = ["Campo obrigatório."]
            });

        return await ConfirmarAsync(
            barbeariaId, request, clienteId, OrigemAgendamento.Manual, vinculo, cancellationToken);
    }

    public async Task<IReadOnlyList<AgendamentoResponse>> ListarAsync(
        Guid barbeariaId,
        DateOnly de,
        DateOnly ate,
        Guid? profissionalId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var perfil = await profissionais.ObterPorUsuarioAsync(usuarioId, cancellationToken);
        if (perfil is null)
            throw AcessoNegado();

        var vinculo = await profissionais.ObterVinculoAsync(barbeariaId, perfil.Id, cancellationToken);
        if (vinculo is null || vinculo.Situacao != StatusVinculo.Ativo)
            throw AcessoNegado();

        var fuso = await ObterFusoAsync(barbeariaId, cancellationToken);
        var inicioUtc = ParaUtc(de, new TimeOnly(0, 0), fuso);
        var fimUtc = ParaUtc(ate.AddDays(1), new TimeOnly(0, 0), fuso);
        var filtroProfissional = vinculo.Cargo == CargoProfissional.Proprietario
            ? profissionalId
            : perfil.Id;

        var agendamentos = await repositorio.ListarPorBarbeariaAsync(
            barbeariaId, inicioUtc, fimUtc, filtroProfissional, null, cancellationToken);
        return agendamentos.Select(item => ParaResponse(item)).ToList();
    }

    public async Task<IReadOnlyList<AgendamentoResponse>> ListarDoClienteAsync(
        string usuarioId,
        DateOnly de,
        DateOnly ate,
        CancellationToken cancellationToken)
    {
        var conta = await identidade.ObterContaAsync(usuarioId, cancellationToken);
        if (conta is null || conta.Tipo != ServicoConta.TipoCliente)
            throw AcessoNegado();

        var fuso = await ObterFusoPadraoAsync(usuarioId, cancellationToken);
        var inicioUtc = ParaUtc(de, new TimeOnly(0, 0), fuso);
        var fimUtc = ParaUtc(ate.AddDays(1), new TimeOnly(0, 0), fuso);
        var agendamentos = await repositorio.ListarDoClienteAsync(conta.Id, inicioUtc, fimUtc, cancellationToken);
        return agendamentos.Select(item => ParaResponse(item)).ToList();
    }

    public async Task<AgendamentoResponse> ObterAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var agendamento = await repositorio.ObterPorIdAsync(barbeariaId, agendamentoId, cancellationToken);
        if (agendamento is null)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Agendamento não encontrado.");

        var conta = await identidade.ObterContaAsync(usuarioId, cancellationToken);
        if (conta is null)
            throw AcessoNegado();

        if (conta.Tipo == ServicoConta.TipoCliente)
        {
            if (agendamento.ClienteId != conta.Id)
                throw AcessoNegado();
            return ParaResponse(agendamento);
        }

        var perfil = await profissionais.ObterPorUsuarioAsync(usuarioId, cancellationToken);
        if (perfil is null)
            throw AcessoNegado();

        var vinculo = await profissionais.ObterVinculoAsync(barbeariaId, perfil.Id, cancellationToken);
        if (vinculo is null || vinculo.Situacao != StatusVinculo.Ativo)
            throw AcessoNegado();

        if (vinculo.Cargo != CargoProfissional.Proprietario &&
            agendamento.ProfissionalId != perfil.Id)
            throw AcessoNegado();

        var pagamento = await pagamentos.ObterPorAgendamentoAsync(
            barbeariaId, agendamentoId, cancellationToken);
        var resumo = pagamento is null
            ? null
            : new PagamentoResumo(
                pagamento.Id, pagamento.Valor, pagamento.Metodo.ToString(), pagamento.RegistradoEmUtc);
        return ParaResponse(agendamento, resumo, pagamento is null ? "Pendente" : "Pago");
    }

    private async Task<AgendamentoResponse> ConfirmarAsync(
        Guid barbeariaId,
        CriarAgendamentoRequest request,
        string clienteId,
        OrigemAgendamento origem,
        VinculoProfissional? vinculoUsuario,
        CancellationToken cancellationToken)
    {
        var servico = await servicos.ObterPorIdAsync(barbeariaId, request.ServicoId, cancellationToken);
        if (servico is null || !servico.Ativo || servico.Preco is null)
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["servicoId"] = ["Serviço ativo não encontrado nesta barbearia."]
            });

        var vinculoProfissional = vinculoUsuario is not null &&
                                  vinculoUsuario.ProfissionalId == request.ProfissionalId
            ? vinculoUsuario
            : await profissionais.ObterVinculoAsync(barbeariaId, request.ProfissionalId, cancellationToken);

        if (vinculoProfissional is null || vinculoProfissional.Situacao != StatusVinculo.Ativo)
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["profissionalId"] = ["Profissional sem vínculo ativo com a barbearia."]
            });

        await validador.ValidarAsync(
            barbeariaId, request.ProfissionalId, request.IniciaEmUtc, servico.DuracaoMinutos, cancellationToken);

        var agendamento = Agendamento.Criar(
            barbeariaId,
            clienteId,
            request.ProfissionalId,
            request.ServicoId,
            request.IniciaEmUtc,
            servico.Nome,
            servico.DuracaoMinutos,
            servico.Preco.Value,
            request.Observacoes,
            origem);

        var chaveLock = await validador.ObterChaveLockAsync(
            barbeariaId, request.ProfissionalId, request.IniciaEmUtc, cancellationToken);
        var resultado = await repositorio.ConfirmarAsync(agendamento, chaveLock, cancellationToken);
        if (resultado == ResultadoConfirmacao.Indisponivel)
            throw new ExcecaoIndisponibilidadeAplicacao(
                CodigosErro.AgendaLockTimeout, "A agenda está ocupada. Tente novamente em instantes.");

        if (resultado != ResultadoConfirmacao.Sucesso)
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.AgendaHorarioIndisponivel, "O horário não está mais disponível.");

        return ParaResponse(agendamento);
    }

    private async Task<TimeZoneInfo> ObterFusoAsync(Guid barbeariaId, CancellationToken cancellationToken)
    {
        var barbearia = await barbearias.ObterPorIdAsync(barbeariaId, cancellationToken);
        if (barbearia is null)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Barbearia não encontrada.");
        return TimeZoneInfo.FindSystemTimeZoneById(barbearia.FusoHorarioId);
    }

    private async Task<TimeZoneInfo> ObterFusoPadraoAsync(string usuarioId, CancellationToken cancellationToken)
    {
        // Os agendamentos do cliente são apresentados no fuso da primeira
        // barbearia encontrada; a listagem detalhada por barbearia usa o fuso dela.
        var agendamentos = await repositorio.ListarDoClienteAsync(
            usuarioId, DateTime.MinValue, DateTime.MaxValue, cancellationToken);
        if (agendamentos.Count == 0)
            return TimeZoneInfo.Utc;

        var barbearia = await barbearias.ObterPorIdAsync(agendamentos[0].BarbeariaId, cancellationToken);
        return barbearia is null
            ? TimeZoneInfo.Utc
            : TimeZoneInfo.FindSystemTimeZoneById(barbearia.FusoHorarioId);
    }

    private static DateTime ParaUtc(DateOnly data, TimeOnly hora, TimeZoneInfo fuso) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(data.ToDateTime(hora), DateTimeKind.Unspecified), fuso);

    private static AgendamentoResponse ParaResponse(
        Agendamento agendamento, PagamentoResumo? pagamento = null, string? situacaoPagamento = null) =>
        new(
            agendamento.Id,
            agendamento.BarbeariaId,
            agendamento.ProfissionalId,
            agendamento.ServicoId,
            agendamento.IniciaEmUtc,
            agendamento.TerminaEmUtc,
            agendamento.NomeServicoSnapshot,
            agendamento.DuracaoSnapshot,
            agendamento.PrecoSnapshot,
            agendamento.Situacao.ToString(),
            agendamento.Origem.ToString(),
            agendamento.CanceladoEmUtc,
            agendamento.CanceladoPorUsuarioId,
            agendamento.MotivoCancelamento,
            agendamento.InicioRealUtc,
            agendamento.FimRealUtc,
            situacaoPagamento ?? (pagamento is null ? "Pendente" : "Pago"),
            pagamento);

    private static ExcecaoAutorizacaoAplicacao AcessoNegado() =>
        new(CodigosErro.TenantNegado, "O usuário não possui permissão para esta operação.");

    private static ExcecaoAutorizacaoAplicacao AcessoNegado(string mensagem) =>
        new(CodigosErro.TenantNegado, mensagem);
}
