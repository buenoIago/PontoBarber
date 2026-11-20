using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia.DTOs;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

namespace PontoBarber.Aplicacao.Modulos.Barbearia;

/// <summary>
/// Casos de uso da barbearia: criação, consulta e edição de dados cadastrais.
/// As regras de validação e atualização pertencem ao domínio; a aplicação
/// apenas converte os erros para o contrato de ProblemDetails (TDD §11.1).
/// </summary>
public sealed class ServicoBarbearia(
    IIdentidade identidade,
    IBarbeariaRepositorio repositorio,
    IProfissionalRepositorio profissionais)
{
    private const int _tentativasCodigoConvite = 10;

    public async Task<BarbeariaResponse> CriarAsync(
        CriarBarbeariaRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(usuarioId, out _))
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["usuario"] = ["Usuário autenticado inválido."]
            });

        var conta = await identidade.ObterContaAsync(usuarioId, cancellationToken);
        if (conta is null || conta.Tipo != ServicoConta.TipoBarbeiro)
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["usuario"] = ["Somente profissionais podem criar uma barbearia."]
            });

        var codigoConvite = await GerarCodigoConviteUnicoAsync(cancellationToken);

        var proprietario = PerfilProfissional.Criar(usuarioId, conta.NomeCompleto, conta.Telefone);
        var barbearia = BarbeariaEntidade.Criar(
            request.Nome?.Trim() ?? string.Empty,
            request.TelefoneComercial?.Trim() ?? string.Empty,
            request.EmailComercial?.Trim() ?? string.Empty,
            request.Cidade?.Trim() ?? string.Empty,
            request.Endereco?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(request.Instagram) ? null : request.Instagram.Trim(),
            codigoConvite,
            request.FusoHorarioId?.Trim() ?? string.Empty,
            proprietario.Id);

        Validar(barbearia);

        var vinculo = VinculoProfissional.Criar(barbearia.Id, proprietario.Id);
        await repositorio.CriarAsync(barbearia, proprietario, vinculo, cancellationToken);

        return ParaResponse(barbearia);
    }

    public async Task<BarbeariaResponse> ObterAsync(
        Guid barbeariaId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var (_, vinculo) = await ObterVinculoAtivoAsync(barbeariaId, usuarioId, cancellationToken);

        var barbearia = await repositorio.ObterPorIdAsync(barbeariaId, cancellationToken);
        if (barbearia is null)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Barbearia não encontrada.");

        var resposta = ParaResponse(barbearia);
        // O código de convite é visível somente ao proprietário (OpenSpec).
        return vinculo.Cargo == CargoProfissional.Proprietario
            ? resposta
            : resposta with { CodigoConvite = null };
    }

    public async Task<BarbeariaResponse> AtualizarAsync(
        Guid barbeariaId,
        AtualizarBarbeariaRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var (_, vinculo) = await ObterVinculoAtivoAsync(barbeariaId, usuarioId, cancellationToken);
        if (vinculo.Cargo != CargoProfissional.Proprietario)
            throw new ExcecaoAutorizacaoAplicacao(
                CodigosErro.TenantNegado, "Somente o proprietário pode editar os dados da barbearia.");

        var barbearia = await repositorio.ObterPorIdAsync(barbeariaId, cancellationToken);
        if (barbearia is null)
            throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Barbearia não encontrada.");

        var atualizada = BarbeariaEntidade.Criar(
            request.Nome?.Trim() ?? string.Empty,
            request.TelefoneComercial?.Trim() ?? string.Empty,
            request.EmailComercial?.Trim() ?? string.Empty,
            request.Cidade?.Trim() ?? string.Empty,
            request.Endereco?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(request.Instagram) ? null : request.Instagram.Trim(),
            barbearia.CodigoConvite,
            barbearia.FusoHorarioId,
            barbearia.IdProfissionalProprietario);

        // Valida o candidato antes de tocar na instância persistida, para que
        // uma requisição inválida não deixe efeitos colaterais no domínio.
        Validar(atualizada);
        barbearia.Atualizar(atualizada);

        await repositorio.AtualizarAsync(barbearia, cancellationToken);

        return ParaResponse(barbearia);
    }

    private async Task<(PerfilProfissional Perfil, VinculoProfissional Vinculo)> ObterVinculoAtivoAsync(
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

        return (perfil, vinculo);
    }

    private static void Validar(BarbeariaEntidade barbearia)
    {
        var erros = barbearia.Validar();
        if (erros.Count == 0)
            return;

        throw new ExcecaoValidacaoAplicacao(
            erros.ToDictionary(erro => erro.Campo, erro => new[] { erro.Mensagem }));
    }

    private static ExcecaoAutorizacaoAplicacao AcessoNegado() =>
        new(CodigosErro.TenantNegado, "O usuário não possui vínculo ativo com a barbearia solicitada.");

    private async Task<string> GerarCodigoConviteUnicoAsync(CancellationToken cancellationToken)
    {
        for (var tentativa = 0; tentativa < _tentativasCodigoConvite; tentativa++)
        {
            var codigo = BarbeariaEntidade.GerarCodigoConvite();
            if (!await repositorio.CodigoConviteExisteAsync(codigo, cancellationToken))
                return codigo;
        }

        throw new ExcecaoConflitoAplicacao(
            CodigosErro.TenantConviteConflito,
            "Não foi possível gerar um código de convite único.");
    }

    private static BarbeariaResponse ParaResponse(BarbeariaEntidade barbearia) =>
        new(
            barbearia.Id,
            barbearia.Nome,
            barbearia.TelefoneComercial,
            barbearia.EmailComercial,
            barbearia.Cidade,
            barbearia.Endereco,
            barbearia.Instagram,
            barbearia.CodigoConvite,
            barbearia.FusoHorarioId);
}
