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
}
