using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Servicos.DTOs;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Servicos;

namespace PontoBarber.Aplicacao.Modulos.Servicos;

/// <summary>
/// Criação e edição do catálogo (Task 5.2). O tenant e o cargo vêm da rota e
/// do vínculo ativo do usuário, nunca do corpo da requisição.
/// </summary>
public sealed class GestaoServicos(
    IServicoRepositorio repositorio,
    IProfissionalRepositorio profissionais,
    IIdentidade identidade)
{
    public async Task<ServicoResponse> CriarAsync(
        Guid barbeariaId,
        CriarServicoRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirProprietarioAsync(barbeariaId, usuarioId, cancellationToken);

        var nome = request.Nome?.Trim() ?? string.Empty;
        var duracao = request.DuracaoMinutos ?? 0;
        Validar(barbeariaId, nome, duracao, request.Preco);

        var servico = Servico.Criar(barbeariaId, nome, duracao, request.Preco!.Value);
        await repositorio.AdicionarAsync([servico], cancellationToken);

        return ParaResponse(servico);
    }

    public async Task<ServicoResponse> AtualizarAsync(
        Guid barbeariaId,
        Guid servicoId,
        AtualizarServicoRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var vinculo = await ObterVinculoAtivoAsync(barbeariaId, usuarioId, cancellationToken);
        var servico = await repositorio.ObterPorIdAsync(barbeariaId, servicoId, cancellationToken);
        if (servico is null)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Serviço não encontrado nesta barbearia.");

        var nome = request.Nome is null ? servico.Nome : request.Nome.Trim();
        var duracao = request.DuracaoMinutos ?? servico.DuracaoMinutos;
        var preco = request.Preco ?? servico.Preco;

        if (vinculo.Cargo == CargoProfissional.Proprietario)
        {
            Validar(barbeariaId, nome, duracao, preco);
            servico.AtualizarDados(nome, duracao, preco!.Value, request.Ativo ?? servico.Ativo);
        }
        else
        {
            if (nome != servico.Nome)
                throw AcessoNegado("Barbeiros não podem alterar o nome do serviço.");
            if (request.Ativo.HasValue && request.Ativo.Value != servico.Ativo)
                throw AcessoNegado("Barbeiros não podem alterar a disponibilidade do serviço.");

            Validar(barbeariaId, nome, duracao, preco);
            servico.AtualizarValores(duracao, preco!.Value);
        }

        await repositorio.AtualizarAsync(servico, cancellationToken);
        return ParaResponse(servico);
    }

    /// <summary>
    /// Consulta do catálogo conforme o perfil: clientes recebem somente os
    /// serviços ativos; profissionais com vínculo ativo veem o catálogo da
    /// própria barbearia, inclusive inativos (Task 5.3).
    /// </summary>
    public async Task<IReadOnlyList<ServicoResponse>> ListarAsync(
        Guid barbeariaId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(usuarioId, out _))
            throw AcessoNegado();

        var conta = await identidade.ObterContaAsync(usuarioId, cancellationToken);
        if (conta is null)
            throw AcessoNegado();

        IReadOnlyList<Servico> servicos;
        if (conta.Tipo == ServicoConta.TipoCliente)
            servicos = await repositorio.ListarAtivosPorBarbeariaAsync(barbeariaId, cancellationToken);
        else
        {
            await ObterVinculoAtivoAsync(barbeariaId, usuarioId, cancellationToken);
            servicos = await repositorio.ListarPorBarbeariaAsync(barbeariaId, cancellationToken);
        }

        return servicos.Select(ParaResponse).ToList();
    }

    /// <summary>Remove o serviço por desativação lógica; apenas proprietário.</summary>
    public async Task RemoverAsync(
        Guid barbeariaId,
        Guid servicoId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirProprietarioAsync(barbeariaId, usuarioId, cancellationToken);

        var servico = await repositorio.ObterPorIdAsync(barbeariaId, servicoId, cancellationToken);
        if (servico is null)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Serviço não encontrado nesta barbearia.");

        servico.Remover();
        await repositorio.AtualizarAsync(servico, cancellationToken);
    }

    private async Task ExigirProprietarioAsync(
        Guid barbeariaId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var vinculo = await ObterVinculoAtivoAsync(barbeariaId, usuarioId, cancellationToken);
        if (vinculo.Cargo != CargoProfissional.Proprietario)
            throw AcessoNegado("Somente o proprietário pode criar serviços.");
    }

    private async Task<VinculoProfissional> ObterVinculoAtivoAsync(
        Guid barbeariaId,
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

        return vinculo;
    }

    private static void Validar(Guid barbeariaId, string nome, int duracao, decimal? preco)
    {
        var erros = Servico.ValidarDados(barbeariaId, nome, duracao, preco);
        if (erros.Count == 0)
            return;

        throw new ExcecaoValidacaoAplicacao(
            erros.ToDictionary(erro => erro.Campo, erro => new[] { erro.Mensagem }));
    }

    private static ServicoResponse ParaResponse(Servico servico) =>
        new(
            servico.Id,
            servico.BarbeariaId,
            servico.Nome,
            servico.DuracaoMinutos,
            servico.Preco,
            servico.Ativo,
            servico.EhModelo);

    private static ExcecaoAutorizacaoAplicacao AcessoNegado(string mensagem) =>
        new(CodigosErro.TenantNegado, mensagem);

    private static ExcecaoAutorizacaoAplicacao AcessoNegado() =>
        AcessoNegado("O usuário não possui permissão para gerenciar este serviço.");
}
