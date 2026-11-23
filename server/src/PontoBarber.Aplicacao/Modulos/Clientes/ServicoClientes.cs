using PontoBarber.Aplicacao.Auth;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Clientes.DTOs;
using PontoBarber.Dominio.Modulos.Clientes;
using PontoBarber.Dominio.Modulos.Profissionais;

namespace PontoBarber.Aplicacao.Modulos.Clientes;

/// <summary>
/// Relação de clientes derivada dos agendamentos e autoatendimento do perfil.
/// A autorização é sempre aplicada aqui, nunca apenas na ocultação de botões.
/// </summary>
public sealed class ServicoClientes(
    IClienteRepositorio repositorio,
    IProfissionalRepositorio profissionais,
    IIdentidade identidade)
{
    private const int _tamanhoPadrao = 20;
    private const int _tamanhoMaximo = 100;

    public async Task<ListaClientesResponse> ListarAsync(
        Guid barbeariaId,
        string? busca,
        int? pagina,
        int? tamanho,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirProprietarioAsync(barbeariaId, usuarioId, cancellationToken);

        var paginaEfetiva = Math.Max(1, pagina ?? 1);
        var tamanhoEfetivo = Math.Clamp(tamanho ?? _tamanhoPadrao, 1, _tamanhoMaximo);
        var termo = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim();
        if (termo?.Length > 120)
            throw ErroValidacao("busca", "Máximo de 120 caracteres.");

        var itens = await repositorio.ListarAsync(
            barbeariaId, termo, paginaEfetiva, tamanhoEfetivo, cancellationToken);
        var total = await repositorio.ContarAsync(barbeariaId, termo, cancellationToken);

        return new ListaClientesResponse(
            itens.Select(ParaResponse).ToList(), total, paginaEfetiva, tamanhoEfetivo);
    }

    public async Task<ClienteResponse> ObterAsync(
        Guid barbeariaId,
        string clienteId,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirProprietarioAsync(barbeariaId, usuarioId, cancellationToken);

        var cliente = await repositorio.ObterAsync(barbeariaId, clienteId, cancellationToken)
            ?? throw new ExcecaoNaoEncontradoAplicacao(
                CodigosErro.TenantNaoEncontrado, "Cliente não relacionado a esta barbearia.");

        return ParaResponse(cliente);
    }

    public async Task<ClientePerfilResponse> ObterPerfilAsync(
        string usuarioId, CancellationToken cancellationToken)
    {
        var conta = await ExigirClienteAsync(usuarioId, cancellationToken);
        return new ClientePerfilResponse(
            conta.Id, conta.NomeCompleto, conta.Email, conta.Telefone);
    }

    public async Task<ClientePerfilResponse> AtualizarPerfilAsync(
        AtualizarPerfilClienteRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        await ExigirClienteAsync(usuarioId, cancellationToken);

        DadosPerfilCliente dados;
        try
        {
            dados = DadosPerfilCliente.Criar(request.NomeCompleto, request.Email, request.Telefone);
        }
        catch (InvalidOperationException excecao)
        {
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                ["perfil"] = [excecao.Message]
            });
        }

        var resultado = await identidade.AtualizarPerfilAsync(
            usuarioId, dados.NomeCompleto, dados.Email, dados.Telefone, cancellationToken);

        switch (resultado)
        {
            case ResultadoAtualizacaoPerfil.Sucesso:
                break;
            case ResultadoAtualizacaoPerfil.UsuarioNaoEncontrado:
                throw new ExcecaoNaoEncontradoAplicacao(
                    CodigosErro.TenantNaoEncontrado, "Conta não encontrada.");
            case ResultadoAtualizacaoPerfil.EmailDuplicado:
                throw new ExcecaoConflitoAplicacao(
                    CodigosErro.ValidacaoFalhou, "Este e-mail já está em uso.");
            default:
                throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
                {
                    ["perfil"] = ["Não foi possível atualizar os dados."]
                });
        }

        return await ObterPerfilAsync(usuarioId, cancellationToken);
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

    private async Task<DadosConta> ExigirClienteAsync(
        string usuarioId, CancellationToken cancellationToken)
    {
        var conta = await identidade.ObterContaAsync(usuarioId, cancellationToken);
        if (conta is null || conta.Tipo != ServicoConta.TipoCliente)
            throw AcessoNegado();
        return conta;
    }

    private static ClienteResponse ParaResponse(ClienteResumo cliente) =>
        new(cliente.UsuarioId, cliente.NomeCompleto, cliente.Email, cliente.Telefone);

    private static ExcecaoAutorizacaoAplicacao AcessoNegado() =>
        new(CodigosErro.TenantNegado, "O usuário não possui permissão para esta operação.");

    private static ExcecaoValidacaoAplicacao ErroValidacao(string campo, string mensagem) =>
        new(new Dictionary<string, string[]> { [campo] = [mensagem] });
}
