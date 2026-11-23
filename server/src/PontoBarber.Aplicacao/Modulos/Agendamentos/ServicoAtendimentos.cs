using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Agendamentos.DTOs;
using PontoBarber.Aplicacao.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Financeiro;
using PontoBarber.Dominio.Modulos.Idempotencia;
using PontoBarber.Dominio.Modulos.Profissionais;

namespace PontoBarber.Aplicacao.Modulos.Agendamentos;

/// <summary>
/// Atendimento presencial e pagamento. A autorização exige barbeiro da própria
/// agenda; o estado financeiro é persistido separadamente e não altera o
/// estado do atendimento.
/// </summary>
public sealed class ServicoAtendimentos(
    IAgendamentoRepositorio agendamentos,
    IPagamentoRepositorio pagamentos,
    IProfissionalRepositorio profissionais,
    IBarbeariaRepositorio barbearias,
    ValidadorHorarioAgendamento validador,
    IIdentidade identidade)
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public async Task<AgendamentoResponse> IniciarAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var agendamento = await ObterAutorizadoAsync(barbeariaId, agendamentoId, usuarioId, cancellationToken);
        if (agendamento.Situacao != SituacaoAgendamento.Confirmado)
            throw new ExcecaoConflitoAplicacao(
                agendamento.Situacao is SituacaoAgendamento.EmAndamento or SituacaoAgendamento.Concluido
                    ? CodigosErro.AgendamentoJaIniciado
                    : CodigosErro.AgendamentoEstadoInvalido,
                $"O agendamento está {agendamento.Situacao} e não pode ser iniciado.");

        var agora = DateTime.UtcNow;
        var fuso = await ObterFusoAsync(barbeariaId, cancellationToken);
        var hoje = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(agora, fuso));
        var diaAgendamento = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(agendamento.IniciaEmUtc, fuso));
        if (hoje != diaAgendamento)
            throw ErroValidacao("agendamento", "O atendimento só pode começar no dia do agendamento.");

        var chave = await validador.ObterChaveLockAsync(
            barbeariaId, agendamento.ProfissionalId, agendamento.IniciaEmUtc, cancellationToken);
        var resultado = await agendamentos.IniciarAsync(
            barbeariaId, agendamentoId, chave, usuarioId, agora, cancellationToken);
        MapearResultado(resultado, agendamento);

        return ParaResponse(agendamento);
    }

    public async Task<AgendamentoResponse> ConcluirAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var agendamento = await ObterAutorizadoAsync(barbeariaId, agendamentoId, usuarioId, cancellationToken);
        var agora = DateTime.UtcNow;
        var chave = await validador.ObterChaveLockAsync(
            barbeariaId, agendamento.ProfissionalId, agendamento.IniciaEmUtc, cancellationToken);

        var resultado = await agendamentos.ConcluirAsync(
            barbeariaId, agendamentoId, chave, usuarioId, agora, cancellationToken);
        MapearResultado(resultado, agendamento);

        return ParaResponse(agendamento);
    }

    public async Task<AgendamentoResponse> RegistrarAusenciaAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var agendamento = await ObterAutorizadoAsync(barbeariaId, agendamentoId, usuarioId, cancellationToken);
        var agora = DateTime.UtcNow;
        var chave = await validador.ObterChaveLockAsync(
            barbeariaId, agendamento.ProfissionalId, agendamento.IniciaEmUtc, cancellationToken);

        var resultado = await agendamentos.RegistrarAusenciaAsync(
            barbeariaId, agendamentoId, chave, usuarioId, agora, cancellationToken);
        MapearResultado(resultado, agendamento);

        return ParaResponse(agendamento);
    }

    public async Task<PagamentoResponse> RegistrarPagamentoAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        RegistrarPagamentoRequest request,
        string? chaveIdempotencia,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(chaveIdempotencia))
            throw new ExcecaoIdempotenciaAplicacao(
                CodigosErro.IdempotenciaChaveAusente, "Informe a chave de idempotência.");

        var erros = new Dictionary<string, string[]>();
        if (request.Valor < 0 || decimal.Round(request.Valor, 2) != request.Valor)
            erros["valor"] = ["Informe um valor não negativo com até duas casas decimais."];
        if (!Enum.TryParse<FormaPagamento>(request.Metodo, ignoreCase: true, out var metodo) ||
            !Enum.IsDefined(metodo))
            erros["metodo"] = ["Forma de pagamento inválida."];
        if (erros.Count > 0)
            throw new ExcecaoValidacaoAplicacao(erros);

        var agendamento = await ObterAutorizadoAsync(barbeariaId, agendamentoId, usuarioId, cancellationToken);
        if (agendamento.Situacao is SituacaoAgendamento.Cancelado or SituacaoAgendamento.NaoCompareceu)
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.AgendamentoEstadoInvalido, "Este agendamento não admite pagamento.");

        var hash = CalcularHash(agendamentoId, request.Valor, metodo);
        var registro = await pagamentos.ObterIdempotenciaAsync(
            usuarioId, chaveIdempotencia, cancellationToken);

        if (registro is not null && registro.ExpiraEmUtc > DateTime.UtcNow)
        {
            if (!string.Equals(registro.HashRequisicao, hash, StringComparison.Ordinal))
                throw new ExcecaoConflitoAplicacao(
                    CodigosErro.ConcorrenciaConflito, "A chave de idempotência foi reutilizada com outros dados.");

            return JsonSerializer.Deserialize<PagamentoResponse>(registro.JsonResposta ?? "null", _json)!;
        }

        if (registro is not null)
            await pagamentos.RemoverIdempotenciaExpiradaAsync(registro, cancellationToken);

        if (await pagamentos.ObterPorAgendamentoAsync(barbeariaId, agendamentoId, cancellationToken) is not null)
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.PagamentoJaRegistrado, "Este atendimento já possui pagamento registrado.");

        var agora = DateTime.UtcNow;
        var pagamento = Pagamento.Criar(
            barbeariaId, agendamentoId, request.Valor, metodo, agora, usuarioId);
        var resposta = new PagamentoResponse(
            pagamento.Id, agendamentoId, pagamento.Valor, metodo.ToString(), agora, "Pago");

        var novoRegistro = RegistroIdempotencia.Criar(
            usuarioId,
            chaveIdempotencia,
            hash,
            JsonSerializer.Serialize(resposta, _json),
            201,
            agora,
            agora.AddHours(24));

        var resultado = await pagamentos.RegistrarAsync(
            pagamento, novoRegistro, usuarioId, cancellationToken);

        return resultado switch
        {
            ResultadoPagamento.Sucesso => resposta,
            ResultadoPagamento.ChaveEmConflito => throw new ExcecaoConflitoAplicacao(
                CodigosErro.ConcorrenciaConflito, "A chave de idempotência foi reutilizada com outros dados."),
            _ => throw new ExcecaoConflitoAplicacao(
                CodigosErro.PagamentoJaRegistrado, "Este atendimento já possui pagamento registrado.")
        };
    }

    private async Task<Agendamento> ObterAutorizadoAsync(
        Guid barbeariaId,
        Guid agendamentoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var agendamento = await agendamentos.ObterPorIdAsync(barbeariaId, agendamentoId, cancellationToken)
            ?? throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Agendamento não encontrado.");

        var conta = await identidade.ObterContaAsync(usuarioId, cancellationToken);
        if (conta is null || conta.Tipo != ServicoConta.TipoBarbeiro)
            throw AcessoNegado();

        var perfil = await profissionais.ObterPorUsuarioAsync(usuarioId, cancellationToken);
        if (perfil is null)
            throw AcessoNegado();

        var vinculo = await profissionais.ObterVinculoAsync(barbeariaId, perfil.Id, cancellationToken);
        if (vinculo is null || vinculo.Situacao != StatusVinculo.Ativo ||
            perfil.Id != agendamento.ProfissionalId)
            throw AcessoNegado("Somente o barbeiro da própria agenda pode realizar esta operação.");

        return agendamento;
    }

    private async Task<TimeZoneInfo> ObterFusoAsync(Guid barbeariaId, CancellationToken cancellationToken)
    {
        var barbearia = await barbearias.ObterPorIdAsync(barbeariaId, cancellationToken)
            ?? throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Barbearia não encontrada.");
        return TimeZoneInfo.FindSystemTimeZoneById(barbearia.FusoHorarioId);
    }

    private static string CalcularHash(Guid agendamentoId, decimal valor, FormaPagamento metodo)
    {
        var dados = $"{agendamentoId}|{valor:F2}|{metodo}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(dados)));
    }

    private static void MapearResultado(ResultadoCicloVida resultado, Agendamento agendamento)
    {
        switch (resultado)
        {
            case ResultadoCicloVida.Sucesso:
                return;
            case ResultadoCicloVida.NaoEncontrado:
                throw new ExcecaoNaoEncontradoAplicacao(
                    CodigosErro.TenantNaoEncontrado, "Agendamento não encontrado.");
            case ResultadoCicloVida.JaIniciado:
                throw new ExcecaoConflitoAplicacao(
                    CodigosErro.AgendamentoJaIniciado, "O atendimento já foi iniciado.");
            case ResultadoCicloVida.Indisponivel:
                throw new ExcecaoIndisponibilidadeAplicacao(
                    CodigosErro.AgendaLockTimeout, "A agenda está ocupada. Tente novamente em instantes.");
            default:
                throw new ExcecaoConflitoAplicacao(
                    CodigosErro.AgendamentoEstadoInvalido,
                    $"O agendamento está {agendamento.Situacao} e não permite esta operação.");
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
}
