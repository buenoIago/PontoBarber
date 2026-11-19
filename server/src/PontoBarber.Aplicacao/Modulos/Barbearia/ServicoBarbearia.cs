using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Barbearia.DTOs;
using PontoBarber.Aplicacao.Modulos.Barbearia.Util;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

namespace PontoBarber.Aplicacao.Modulos.Barbearia;

/// <summary>
/// Criação da barbearia com validação dos dados, fuso IANA e código de
/// convite único. O criador é o profissional autenticado e recebe o vínculo
/// de proprietário na mesma transação (SPEC §1/§2, TDD §8 e §11.2).
/// </summary>
public sealed class ServicoBarbearia(
    IIdentidade identidade,
    IBarbeariaRepositorio repositorio)
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

        var erros = new Dictionary<string, string[]>();

        var nome = ValidadorBarbearia.ValidarObrigatorio(request.Nome, "nome", erros);
        var telefone = ValidadorBarbearia.ValidarObrigatorio(request.TelefoneComercial, "telefoneComercial", erros);
        var email = ValidadorBarbearia.ValidarObrigatorio(request.EmailComercial, "emailComercial", erros);
        var cidade = ValidadorBarbearia.ValidarObrigatorio(request.Cidade, "cidade", erros);
        var endereco = ValidadorBarbearia.ValidarObrigatorio(request.Endereco, "endereco", erros);
        var fuso = ValidadorBarbearia.ValidarObrigatorio(request.FusoHorarioId, "fusoHorarioId", erros);
        var instagram = string.IsNullOrWhiteSpace(request.Instagram) ? null : request.Instagram.Trim();

        if (telefone is not null && telefone.Count(char.IsDigit) < 10)
            ValidadorBarbearia.AdicionarErro(erros, "telefoneComercial", "Informe um telefone válido.");

        if (email is not null && !ValidadorBarbearia.EmailValido(email))
            ValidadorBarbearia.AdicionarErro(erros, "emailComercial", "Informe um e-mail válido.");

        if (fuso is not null && !ValidadorBarbearia.FusoIanaValido(fuso))
            ValidadorBarbearia.AdicionarErro(erros, "fusoHorarioId", "Informe um identificador de fuso IANA válido.");

        if (erros.Count > 0)
            throw new ExcecaoValidacaoAplicacao(erros);

        var codigoConvite = await GerarCodigoConviteUnicoAsync(cancellationToken);

        var proprietario = PerfilProfissional.Criar(usuarioId, conta.NomeCompleto, conta.Telefone);
        var barbearia = BarbeariaEntidade.Criar(
            nome!.Trim(),
            telefone!.Trim(),
            email!.Trim(),
            cidade!.Trim(),
            endereco!.Trim(),
            instagram,
            codigoConvite,
            fuso!.Trim(),
            proprietario.Id);
        var vinculo = VinculoProfissional.Criar(barbearia.Id, proprietario.Id);

        await repositorio.CriarAsync(barbearia, proprietario, vinculo, cancellationToken);

        return new BarbeariaResponse(
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
}
