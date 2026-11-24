using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Profissionais.DTOs;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;

namespace PontoBarber.Aplicacao.Modulos.Profissionais;

/// <summary>
/// Ingresso de profissional em barbearia por código de convite (Task 4.3).
/// O perfil e o vínculo pertencem sempre ao usuário autenticado; o cargo é
/// definido pelo domínio como Barbeiro.
/// </summary>
public sealed class ServicoProfissional(
    IIdentidade identidade,
    IBarbeariaRepositorio barbearias,
    IProfissionalRepositorio profissionais)
{
    public async Task<VinculoResponse> EntrarPorConviteAsync(
        EntrarBarbeariaRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoConvite))
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["codigoConvite"] = ["Campo obrigatório."]
            });

        if (!Guid.TryParse(usuarioId, out _))
            throw new ExcecaoAutorizacaoAplicacao(
                CodigosErro.AutenticacaoNegada, "Usuário autenticado inválido.");

        var conta = await identidade.ObterContaAsync(usuarioId, cancellationToken);
        if (conta is null || conta.Tipo != ServicoConta.TipoBarbeiro)
            throw new ExcecaoAutorizacaoAplicacao(
                CodigosErro.AutenticacaoNegada, "Somente profissionais podem entrar em uma barbearia.");

        var barbearia = await barbearias.ObterPorCodigoConviteAsync(
            request.CodigoConvite.Trim(), cancellationToken);
        if (barbearia is null)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Convite inválido ou barbearia não encontrada.");

        var perfil = await profissionais.ObterPorUsuarioAsync(usuarioId, cancellationToken);
        PerfilProfissional? perfilNovo = null;
        if (perfil is null)
        {
            perfil = PerfilProfissional.Criar(usuarioId, conta.NomeCompleto, conta.Telefone);
            perfilNovo = perfil;
        }

        var existente = await profissionais.ObterVinculoAsync(
            barbearia.Id, perfil.Id, cancellationToken);
        if (existente is not null)
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.TenantConviteConflito,
                "O profissional já possui vínculo com esta barbearia.");

        var vinculo = VinculoProfissional.CriarBarbeiro(barbearia.Id, perfil.Id);
        await profissionais.AdicionarVinculoAsync(perfilNovo, vinculo, cancellationToken);

        return new VinculoResponse(
            vinculo.Id,
            vinculo.BarbeariaId,
            vinculo.ProfissionalId,
            vinculo.Cargo.ToString(),
            vinculo.Situacao.ToString());
    }

    /// <summary>
    /// Resolve o vínculo profissional que dá acesso à agenda dentro da barbearia
    /// da rota (TDD §11.2). O proprietário, por sua função administrativa, pode
    /// consultar a agenda de qualquer profissional; os demais profissionais
    /// somente a própria. Este método não implementa agendamentos — fornece
    /// apenas a autorização de recurso que os módulos posteriores usarão.
    /// </summary>
    public async Task<VinculoResponse> ObterVinculoParaAgendaAsync(
        Guid barbeariaId,
        Guid profissionalId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(usuarioId, out _))
            throw AcessoNegado();

        var perfilUsuario = await profissionais.ObterPorUsuarioAsync(usuarioId, cancellationToken);
        if (perfilUsuario is null)
            throw AcessoNegado();

        var vinculoUsuario = await profissionais.ObterVinculoAsync(barbeariaId, perfilUsuario.Id, cancellationToken);
        if (vinculoUsuario is null || vinculoUsuario.Situacao != StatusVinculo.Ativo)
            throw AcessoNegado();

        var vinculoAlvo = await profissionais.ObterVinculoParaAtualizacaoAsync(
            barbeariaId, profissionalId, cancellationToken);
        if (vinculoAlvo is null || vinculoAlvo.Situacao == StatusVinculo.Removido)
            throw AcessoNegado();

        if (perfilUsuario.Id != profissionalId && vinculoUsuario.Cargo != CargoProfissional.Proprietario)
            throw AcessoNegado();

        return new VinculoResponse(
            vinculoAlvo.Id,
            vinculoAlvo.BarbeariaId,
            vinculoAlvo.ProfissionalId,
            vinculoAlvo.Cargo.ToString(),
            vinculoAlvo.Situacao.ToString());
    }

    /// <summary>
    /// Dados de leitura para o agendamento público (Task 7.7): a barbearia da
    /// rota e somente os profissionais com vínculo ativo. Não exige vínculo do
    /// solicitante nem devolve telefone, cargo ou código de convite.
    /// </summary>
    public async Task<DadosAgendamentoPublicoResponse> ObterDadosParaAgendamentoPublicoAsync(
        Guid barbeariaId,
        CancellationToken cancellationToken)
    {
        var barbearia = await barbearias.ObterPorIdAsync(barbeariaId, cancellationToken)
            ?? throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Barbearia não encontrada.");

        var vinculos = await profissionais.ListarProfissionaisAsync(barbeariaId, cancellationToken);
        var ativos = vinculos
            .Where(item => item.Vinculo.Situacao == StatusVinculo.Ativo)
            .Select(item => new ProfissionalDisponivelResponse(
                item.Perfil.Id, item.Perfil.NomeCompleto))
            .OrderBy(item => item.NomeCompleto, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new DadosAgendamentoPublicoResponse(
            barbearia.Id, barbearia.Nome, barbearia.FusoHorarioId, ativos);
    }

    /// <summary>
    /// Lista nome, telefone e situação dos profissionais da barbearia da rota
    /// (RF-PROF-002/003). Exige vínculo ativo do usuário, sem exigir cargo de
    /// proprietário para a leitura.
    /// </summary>
    public async Task<IReadOnlyList<ProfissionalResponse>> ListarProfissionaisAsync(
        Guid barbeariaId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ObterVinculoAtivoAsync(barbeariaId, usuarioId, cancellationToken);

        var vinculos = await profissionais.ListarProfissionaisAsync(barbeariaId, cancellationToken);
        return vinculos
            .Select(item => ParaResponse(item.Perfil, item.Vinculo.Situacao))
            .ToList();
    }

    /// <summary>
    /// Edita apenas nome e telefone do perfil. O tenant e o vínculo do administrador
    /// são derivados da rota e da sessão; o alvo é localizado dentro da mesma
    /// barbearia (Task 4.4 / RF-PROF-005).
    /// </summary>
    public async Task<ProfissionalResponse> AtualizarDadosAsync(
        Guid barbeariaId,
        Guid profissionalId,
        AtualizarProfissionalRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ObterVinculoDoProprietarioAsync(barbeariaId, usuarioId, cancellationToken);
        var (perfil, vinculo) = await ObterAlvoAsync(barbeariaId, profissionalId, cancellationToken);

        var candidato = PerfilProfissional.Criar(
            perfil.UsuarioId,
            request.NomeCompleto?.Trim() ?? string.Empty,
            NormalizarTelefone(request.Telefone));
        Validar(candidato);

        perfil.Atualizar(candidato);
        await profissionais.SalvarAlteracoesAsync(cancellationToken);

        return ParaResponse(perfil, vinculo.Situacao);
    }

    /// <summary>
    /// Ativa ou desativa o vínculo por transição explícita de domínio.
    /// Somente o proprietário da barbearia da rota pode executar (RF-PROF-006).
    /// </summary>
    public async Task<ProfissionalResponse> AlterarStatusAsync(
        Guid barbeariaId,
        Guid profissionalId,
        AlterarStatusProfissionalRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ObterVinculoDoProprietarioAsync(barbeariaId, usuarioId, cancellationToken);

        if (request.Ativo is null)
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["ativo"] = ["Campo obrigatório."]
            });

        var (perfil, vinculo) = await ObterAlvoAsync(barbeariaId, profissionalId, cancellationToken);

        if (request.Ativo.Value)
            vinculo.Ativar();
        else
            vinculo.Desativar();

        await profissionais.SalvarAlteracoesAsync(cancellationToken);

        return ParaResponse(perfil, vinculo.Situacao);
    }

    /// <summary>
    /// Remove o vínculo por desativação lógica (ADR-023). Perfil, conta de
    /// acesso e registros históricos permanecem intactos (RF-PROF-007).
    /// </summary>
    public async Task RemoverVinculoAsync(
        Guid barbeariaId,
        Guid profissionalId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ObterVinculoDoProprietarioAsync(barbeariaId, usuarioId, cancellationToken);
        var (_, vinculo) = await ObterAlvoAsync(barbeariaId, profissionalId, cancellationToken);

        vinculo.Remover();
        await profissionais.SalvarAlteracoesAsync(cancellationToken);
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

    private async Task ObterVinculoDoProprietarioAsync(
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
        if (vinculo is null ||
            vinculo.Situacao != StatusVinculo.Ativo ||
            vinculo.Cargo != CargoProfissional.Proprietario)
            throw AcessoNegado();
    }

    private async Task<(PerfilProfissional Perfil, VinculoProfissional Vinculo)> ObterAlvoAsync(
        Guid barbeariaId,
        Guid profissionalId,
        CancellationToken cancellationToken)
    {
        var perfil = await profissionais.ObterPorIdAsync(profissionalId, cancellationToken);
        if (perfil is null)
            throw AcessoNegado();

        // O alvo é sempre procurado pela barbearia da rota: um vínculo de outro
        // tenant nunca é encontrado, mesmo que o identificador seja conhecido.
        var vinculo = await profissionais.ObterVinculoParaAtualizacaoAsync(
            barbeariaId, profissionalId, cancellationToken);
        if (vinculo is null)
            throw AcessoNegado();

        return (perfil, vinculo);
    }

    private static void Validar(PerfilProfissional perfil)
    {
        var erros = perfil.Validar();
        if (erros.Count == 0)
            return;

        throw new ExcecaoValidacaoAplicacao(
            erros.ToDictionary(erro => erro.Campo, erro => new[] { erro.Mensagem }));
    }

    private static string? NormalizarTelefone(string? telefone) =>
        string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();

    private static ProfissionalResponse ParaResponse(PerfilProfissional perfil, StatusVinculo situacao) =>
        new(perfil.Id, perfil.NomeCompleto, perfil.Telefone, situacao.ToString());

    private static ExcecaoAutorizacaoAplicacao AcessoNegado() =>
        new(CodigosErro.TenantNegado, "O usuário não possui permissão para gerenciar este vínculo.");
}
