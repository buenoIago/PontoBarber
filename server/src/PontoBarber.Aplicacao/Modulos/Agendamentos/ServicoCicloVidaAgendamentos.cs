using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Agendamentos.DTOs;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Profissionais;

namespace PontoBarber.Aplicacao.Modulos.Agendamentos;

/// <summary>
/// Cancelamento, remarcação, sugestões e histórico. A autorização é aplicada
/// aqui; a integridade e a ordem das transições ficam no repositório.
/// </summary>
public sealed class ServicoCicloVidaAgendamentos(
    IAgendamentoRepositorio repositorio,
    IIdentidade identidade,
    IProfissionalRepositorio profissionais,
    ValidadorHorarioAgendamento validador)
{
    public async Task<CancelamentoResponse> CancelarAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        CancelarAgendamentoRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var agendamento = await ObterAgendamentoAsync(barbeariaId, agendamentoId, cancellationToken);
        var permissao = await ObterPermissaoAsync(barbeariaId, usuarioId, cancellationToken);
        GarantirAcesso(agendamento, permissao);

        if (!permissao.EhCliente && string.IsNullOrWhiteSpace(request.Motivo))
            throw ErroValidacao("motivo", "Informe o motivo do cancelamento.");

        var agora = DateTime.UtcNow;
        var chaveLock = await validador.ObterChaveLockAsync(
            barbeariaId, agendamento.ProfissionalId, agendamento.IniciaEmUtc, cancellationToken);

        var resultado = await repositorio.CancelarAsync(
            barbeariaId, agendamentoId, chaveLock, permissao.UsuarioId, request.Motivo, agora, cancellationToken);
        MapearResultado(resultado);

        var historico = await repositorio.ListarHistoricoAsync(barbeariaId, agendamentoId, cancellationToken);
        return new CancelamentoResponse(
            agendamentoId,
            agendamento.Situacao.ToString(),
            agendamento.CanceladoEmUtc,
            agendamento.CanceladoPorUsuarioId,
            agendamento.MotivoCancelamento,
            historico[^1].Id);
    }

    public async Task<AgendamentoResponse> RemarcarAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        RemarcarAgendamentoRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var agendamento = await ObterAgendamentoAsync(barbeariaId, agendamentoId, cancellationToken);
        var permissao = await ObterPermissaoAsync(barbeariaId, usuarioId, cancellationToken);
        GarantirAcesso(agendamento, permissao);

        var agora = DateTime.UtcNow;
        GarantirNaoIniciado(agendamento, agora);
        await validador.ValidarAsync(
            barbeariaId, agendamento.ProfissionalId, request.IniciaEmUtc,
            agendamento.DuracaoSnapshot, cancellationToken);

        var chaves = await ObterChavesAsync(
            barbeariaId, agendamento.ProfissionalId,
            [agendamento.IniciaEmUtc, request.IniciaEmUtc], cancellationToken);

        var resultado = await repositorio.RemarcarAsync(
            barbeariaId, agendamentoId, request.IniciaEmUtc, chaves, agora, cancellationToken);
        MapearResultado(resultado);

        return ParaResponse(agendamento);
    }

    public async Task<SugestaoResponse> SugerirAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        SugerirHorarioRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var agendamento = await ObterAgendamentoAsync(barbeariaId, agendamentoId, cancellationToken);
        var permissao = await ObterPermissaoAsync(barbeariaId, usuarioId, cancellationToken);
        GarantirAcesso(agendamento, permissao);

        // TDD §10.2: a sugestão é uma ação do barbeiro da própria agenda.
        if (permissao.EhCliente || permissao.EhProprietario)
            throw AcessoNegado("Somente o barbeiro da agenda pode sugerir outro horário.");

        if (string.IsNullOrWhiteSpace(request.Motivo))
            throw ErroValidacao("motivo", "Campo obrigatório.");

        var agora = DateTime.UtcNow;
        GarantirNaoIniciado(agendamento, agora);
        await validador.ValidarAsync(
            barbeariaId, agendamento.ProfissionalId, request.IniciaEmUtc,
            agendamento.DuracaoSnapshot, cancellationToken);

        var chaveLock = await validador.ObterChaveLockAsync(
            barbeariaId, agendamento.ProfissionalId, request.IniciaEmUtc, cancellationToken);

        var resultado = await repositorio.SugerirAsync(
            barbeariaId, agendamentoId, request.IniciaEmUtc, request.Motivo, permissao.UsuarioId,
            chaveLock, agora, cancellationToken);
        MapearResultado(resultado);

        var sugestoes = await repositorio.ListarSugestoesAsync(barbeariaId, agendamentoId, cancellationToken);
        return ParaResponse(sugestoes[0]);
    }

    public async Task<SugestaoResponse> AceitarAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        Guid sugestaoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await GarantirTitularAsync(barbeariaId, agendamentoId, usuarioId, cancellationToken);
        var sugestao = await ObterSugestaoAsync(barbeariaId, agendamentoId, sugestaoId, cancellationToken);
        var agora = DateTime.UtcNow;

        if (sugestao.EstaExpirada(agora))
            throw new ExcecaoConflitoAplicacao(CodigosErro.SugestaoExpirada, "A sugestão expirou.");
        if (sugestao.InicioSugeridoUtc <= agora)
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.SugestaoExpirada, "O horário sugerido já passou.");
        if (sugestao.Situacao != SituacaoSugestao.Pendente)
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.AgendamentoEstadoInvalido, "A sugestão já foi respondida.");

        await validador.ValidarAsync(
            barbeariaId, sugestao.ProfissionalId, sugestao.InicioSugeridoUtc,
            (int)(sugestao.FimSugeridoUtc - sugestao.InicioSugeridoUtc).TotalMinutes, cancellationToken);

        var chaves = await ObterChavesAsync(
            barbeariaId, sugestao.ProfissionalId,
            [sugestao.InicioSugeridoUtc], cancellationToken);

        var resultado = await repositorio.ResponderSugestaoAsync(
            barbeariaId, agendamentoId, sugestaoId, true, chaves, agora, cancellationToken);
        MapearResultado(resultado);

        return ParaResponse((await ObterSugestaoAsync(
            barbeariaId, agendamentoId, sugestaoId, cancellationToken)));
    }

    public async Task<SugestaoResponse> RejeitarAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        Guid sugestaoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await GarantirTitularAsync(barbeariaId, agendamentoId, usuarioId, cancellationToken);
        var sugestao = await ObterSugestaoAsync(barbeariaId, agendamentoId, sugestaoId, cancellationToken);
        var agora = DateTime.UtcNow;

        if (sugestao.EstaExpirada(agora))
            throw new ExcecaoConflitoAplicacao(CodigosErro.SugestaoExpirada, "A sugestão expirou.");
        if (sugestao.Situacao != SituacaoSugestao.Pendente)
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.AgendamentoEstadoInvalido, "A sugestão já foi respondida.");

        var resultado = await repositorio.ResponderSugestaoAsync(
            barbeariaId, agendamentoId, sugestaoId, false, [], agora, cancellationToken);
        MapearResultado(resultado);

        return ParaResponse((await ObterSugestaoAsync(
            barbeariaId, agendamentoId, sugestaoId, cancellationToken)));
    }

    public async Task<IReadOnlyList<HistoricoResponse>> ListarHistoricoAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await GarantirAcessoAsync(barbeariaId, agendamentoId, usuarioId, cancellationToken);
        var eventos = await repositorio.ListarHistoricoAsync(barbeariaId, agendamentoId, cancellationToken);
        return eventos.Select(ParaResponse).ToList();
    }

    public async Task<IReadOnlyList<SugestaoResponse>> ListarSugestoesAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await GarantirAcessoAsync(barbeariaId, agendamentoId, usuarioId, cancellationToken);
        var sugestoes = await repositorio.ListarSugestoesAsync(barbeariaId, agendamentoId, cancellationToken);
        return sugestoes.Select(ParaResponse).ToList();
    }

    /// <summary>
    /// Operação idempotente de expiração. A execução periódica depende da
    /// decisão de scheduler (ADR-027); a correção do estado e a liberação da
    /// reserva não dependem do job, pois a validação de aceite também
    /// verifica ExpiraEmUtc em tempo real.
    /// </summary>
    public Task<IReadOnlyList<SugestaoExpiradaInfo>> ExpirarSugestoesAsync(
        CancellationToken cancellationToken) =>
        repositorio.ExpirarSugestoesAsync(DateTime.UtcNow, cancellationToken);

    private async Task GarantirAcessoAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var agendamento = await ObterAgendamentoAsync(barbeariaId, agendamentoId, cancellationToken);
        var permissao = await ObterPermissaoAsync(barbeariaId, usuarioId, cancellationToken);
        GarantirAcesso(agendamento, permissao);
    }

    private async Task GarantirTitularAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var agendamento = await ObterAgendamentoAsync(barbeariaId, agendamentoId, cancellationToken);
        var conta = await identidade.ObterContaAsync(usuarioId, cancellationToken);
        if (conta is null || conta.Tipo != ServicoConta.TipoCliente || agendamento.ClienteId != conta.Id)
            throw AcessoNegado("Somente o titular pode responder a uma sugestão.");
    }

    private async Task<Agendamento> ObterAgendamentoAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        CancellationToken cancellationToken) =>
        await repositorio.ObterPorIdAsync(barbeariaId, agendamentoId, cancellationToken)
            ?? throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Agendamento não encontrado.");

    private async Task<SugestaoHorario> ObterSugestaoAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        Guid sugestaoId,
        CancellationToken cancellationToken) =>
        await repositorio.ObterSugestaoAsync(barbeariaId, agendamentoId, sugestaoId, cancellationToken)
            ?? throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Sugestão não encontrada.");

    private async Task<Permissao> ObterPermissaoAsync(
        Guid barbeariaId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var conta = await identidade.ObterContaAsync(usuarioId, cancellationToken);
        if (conta is null)
            throw AcessoNegado();

        if (conta.Tipo == ServicoConta.TipoCliente)
            return new Permissao(conta.Id, true, false, null);

        var perfil = await profissionais.ObterPorUsuarioAsync(usuarioId, cancellationToken);
        if (perfil is null)
            throw AcessoNegado();

        var vinculo = await profissionais.ObterVinculoAsync(barbeariaId, perfil.Id, cancellationToken);
        if (vinculo is null || vinculo.Situacao != StatusVinculo.Ativo)
            throw AcessoNegado();

        return new Permissao(conta.Id, false, vinculo.Cargo == CargoProfissional.Proprietario, perfil.Id);
    }

    private static void GarantirAcesso(Agendamento agendamento, Permissao permissao)
    {
        if (permissao.EhCliente)
        {
            if (agendamento.ClienteId != permissao.UsuarioId)
                throw AcessoNegado();
            return;
        }

        if (!permissao.EhProprietario && agendamento.ProfissionalId != permissao.ProfissionalId)
            throw AcessoNegado("O agendamento pertence a outro profissional.");
    }

    private static void GarantirNaoIniciado(Agendamento agendamento, DateTime agora)
    {
        if (agora >= agendamento.IniciaEmUtc)
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.AgendamentoJaIniciado, "Não é possível alterar um agendamento que já iniciou.");
    }

    private async Task<IReadOnlyList<string>> ObterChavesAsync(
        Guid barbeariaId,
        Guid profissionalId,
        IEnumerable<DateTime> instantes,
        CancellationToken cancellationToken)
    {
        var chaves = new List<string>();
        foreach (var instante in instantes)
            chaves.Add(await validador.ObterChaveLockAsync(barbeariaId, profissionalId, instante, cancellationToken));
        return chaves.Distinct(StringComparer.Ordinal).ToList();
    }

    private static void MapearResultado(ResultadoCicloVida resultado)
    {
        switch (resultado)
        {
            case ResultadoCicloVida.Sucesso:
                return;
            case ResultadoCicloVida.NaoEncontrado:
                throw new ExcecaoNaoEncontradoAplicacao(
                    CodigosErro.TenantNaoEncontrado, "Agendamento não encontrado.");
            case ResultadoCicloVida.EstadoInvalido:
                throw new ExcecaoConflitoAplicacao(
                    CodigosErro.AgendamentoEstadoInvalido, "O agendamento não permite esta operação no estado atual.");
            case ResultadoCicloVida.JaIniciado:
                throw new ExcecaoConflitoAplicacao(
                    CodigosErro.AgendamentoJaIniciado, "O agendamento já iniciou.");
            case ResultadoCicloVida.Indisponivel:
                throw new ExcecaoIndisponibilidadeAplicacao(
                    CodigosErro.AgendaLockTimeout, "A agenda está ocupada. Tente novamente em instantes.");
            default:
                throw new ExcecaoConflitoAplicacao(
                    CodigosErro.AgendaHorarioIndisponivel, "O horário não está mais disponível.");
        }
    }

    private static ExcecaoAutorizacaoAplicacao AcessoNegado() =>
        new(CodigosErro.TenantNegado, "O usuário não possui permissão para esta operação.");

    private static ExcecaoAutorizacaoAplicacao AcessoNegado(string mensagem) =>
        new(CodigosErro.TenantNegado, mensagem);

    private static ExcecaoValidacaoAplicacao ErroValidacao(string campo, string mensagem) =>
        new(new Dictionary<string, string[]> { [campo] = [mensagem] });

    private static AgendamentoResponse ParaResponse(Agendamento agendamento) =>
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
            agendamento.FimRealUtc);

    private static SugestaoResponse ParaResponse(SugestaoHorario sugestao) =>
        new(
            sugestao.Id,
            sugestao.AgendamentoId,
            sugestao.ProfissionalId,
            sugestao.InicioSugeridoUtc,
            sugestao.FimSugeridoUtc,
            sugestao.Motivo,
            sugestao.Situacao.ToString(),
            sugestao.ExpiraEmUtc,
            sugestao.RespondidoEmUtc,
            sugestao.AutorUsuarioId);

    private static HistoricoResponse ParaResponse(HistoricoAgendamento evento) =>
        new(
            evento.Id,
            evento.AgendamentoId,
            evento.TipoEvento.ToString(),
            evento.AtorUsuarioId,
            evento.OcorridoEmUtc,
            evento.InicioAnteriorUtc,
            evento.NovoInicioUtc,
            evento.Detalhes);

    private sealed record Permissao(string UsuarioId, bool EhCliente, bool EhProprietario, Guid? ProfissionalId);
}
