using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Agendamentos;
using PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Profissionais;

namespace PontoBarber.Aplicacao.Modulos.Profissionais;

/// <summary>
/// Bloqueios pontuais (Task 6.3). O proprietário gerencia os bloqueios dos
/// profissionais da própria barbearia; o barbeiro, somente os seus.
/// </summary>
public sealed class GestaoBloqueios(
    IBloqueioAgendaRepositorio repositorio,
    IProfissionalRepositorio profissionais,
    IAgendamentoRepositorio agendamentos)
{
    public async Task<IReadOnlyList<BloqueioResponse>> ListarAsync(
        Guid barbeariaId,
        Guid profissionalId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirPermissaoAsync(barbeariaId, profissionalId, usuarioId, cancellationToken);

        var bloqueios = await repositorio.ListarAsync(barbeariaId, profissionalId, cancellationToken);
        return bloqueios.Select(ParaResponse).ToList();
    }

    public async Task<BloqueioResponse> CriarAsync(
        Guid barbeariaId,
        Guid profissionalId,
        CriarBloqueioRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirPermissaoAsync(barbeariaId, profissionalId, usuarioId, cancellationToken);

        var erros = BloqueioAgenda.ValidarDados(
            barbeariaId, profissionalId, request.IniciaEmUtc, request.TerminaEmUtc);
        if (erros.Count > 0)
            throw new ExcecaoValidacaoAplicacao(
                erros.ToDictionary(erro => erro.Campo, erro => new[] { erro.Mensagem }));

        if (await agendamentos.ExisteConflitoAsync(
                barbeariaId,
                profissionalId,
                request.IniciaEmUtc,
                request.TerminaEmUtc,
                cancellationToken))
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.AgendaBloqueada, "O bloqueio conflita com um agendamento existente.");

        var bloqueio = BloqueioAgenda.Criar(
            barbeariaId, profissionalId, request.IniciaEmUtc, request.TerminaEmUtc, request.Motivo);
        await repositorio.AdicionarAsync(bloqueio, cancellationToken);

        return ParaResponse(bloqueio);
    }

    public async Task RemoverAsync(
        Guid barbeariaId,
        Guid profissionalId,
        Guid bloqueioId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirPermissaoAsync(barbeariaId, profissionalId, usuarioId, cancellationToken);

        var bloqueio = await repositorio.ObterPorIdAsync(barbeariaId, bloqueioId, cancellationToken);
        if (bloqueio is null || bloqueio.ProfissionalId != profissionalId)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Bloqueio não encontrado nesta barbearia.");

        await repositorio.RemoverAsync(bloqueio, cancellationToken);
    }

    private async Task ExigirPermissaoAsync(
        Guid barbeariaId,
        Guid profissionalId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(usuarioId, out _))
            throw AcessoNegado();

        var perfil = await profissionais.ObterPorUsuarioAsync(usuarioId, cancellationToken);
        if (perfil is null)
            throw AcessoNegado();

        var vinculo = await profissionais.ObterVinculoAsync(barbeariaId, perfil.Id, cancellationToken);
        if (vinculo is null || vinculo.Situacao != StatusVinculo.Ativo)
            throw AcessoNegado();

        var vinculoAlvo = await profissionais.ObterVinculoAsync(barbeariaId, profissionalId, cancellationToken);
        if (vinculoAlvo is null || vinculoAlvo.Situacao == StatusVinculo.Removido)
            throw AcessoNegado();

        if (vinculo.Cargo != CargoProfissional.Proprietario && perfil.Id != profissionalId)
            throw AcessoNegado("Um profissional só pode gerenciar os próprios bloqueios.");
    }

    private static BloqueioResponse ParaResponse(BloqueioAgenda bloqueio) =>
        new(
            bloqueio.Id,
            bloqueio.ProfissionalId,
            bloqueio.IniciaEmUtc,
            bloqueio.TerminaEmUtc,
            bloqueio.Motivo);

    private static ExcecaoAutorizacaoAplicacao AcessoNegado() =>
        new(CodigosErro.TenantNegado, "O usuário não possui vínculo ativo com a barbearia.");

    private static ExcecaoAutorizacaoAplicacao AcessoNegado(string mensagem) =>
        new(CodigosErro.TenantNegado, mensagem);
}
