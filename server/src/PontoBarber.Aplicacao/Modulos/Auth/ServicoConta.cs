using PontoBarber.Aplicacao.Compartilhado;

namespace PontoBarber.Aplicacao.Auth;

/// <summary>
/// Cadastro de cliente e de barbeiro em fluxos distintos e login compartilhado.
/// O tipo do usuário é definido aqui, nunca recebido do cliente.
/// </summary>
public sealed class ServicoConta(IIdentidade identidade)
{
    public const string TipoCliente = "Customer";
    public const string TipoBarbeiro = "Professional";

    public Task<string> CadastrarClienteAsync(RegistroRequest request, CancellationToken cancellationToken) =>
        CadastrarAsync(request, TipoCliente, cancellationToken);

    public Task<string> CadastrarBarbeiroAsync(RegistroRequest request, CancellationToken cancellationToken) =>
        CadastrarAsync(request, TipoBarbeiro, cancellationToken);

    public async Task<ResultadoAutenticacao> AutenticarAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = ValidarTexto(request.Email, "email");
        var senha = ValidarTexto(request.Senha, "senha");

        return await identidade.AutenticarAsync(email.Trim(), senha, cancellationToken);
    }

    public async Task<DadosConta> ObterContaAsync(string usuarioId, CancellationToken cancellationToken) =>
        await identidade.ObterContaAsync(usuarioId, cancellationToken)
        ?? throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
        {
            ["usuario"] = ["Usuário não encontrado."]
        });

    public Task EncerrarSessaoAsync(CancellationToken cancellationToken) =>
        identidade.EncerrarSessaoAsync(cancellationToken);

    public async Task AlterarSenhaAsync(
        AlterarSenhaRequest request,
        string usuarioId,
        CancellationToken cancellationToken)
    {
        var erros = new Dictionary<string, string[]>();

        var senhaAtual = ValidarObrigatorio(request.SenhaAtual, "senhaAtual", erros);
        var novaSenha = ValidarObrigatorio(request.NovaSenha, "novaSenha", erros);
        var confirmar = ValidarObrigatorio(request.ConfirmarNovaSenha, "confirmarNovaSenha", erros);

        if (novaSenha is not null && novaSenha != confirmar)
            AdicionarErro(erros, "confirmarNovaSenha", "A confirmação da nova senha diverge.");

        if (novaSenha is not null && novaSenha.Length < 8)
            AdicionarErro(erros, "novaSenha", "A senha deve ter ao menos 8 caracteres.");

        if (novaSenha is not null && !novaSenha.Any(char.IsDigit))
            AdicionarErro(erros, "novaSenha", "A senha deve conter ao menos um número.");

        if (novaSenha is not null && !novaSenha.Any(char.IsLetter))
            AdicionarErro(erros, "novaSenha", "A senha deve conter ao menos uma letra.");

        if (erros.Count > 0)
            throw new ExcecaoValidacaoAplicacao(erros);

        var resultado = await identidade.AlterarSenhaAsync(usuarioId, senhaAtual!, novaSenha!, cancellationToken);

        switch (resultado)
        {
            case ResultadoAlteracaoSenha.SenhaAtualIncorreta:
                throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
                {
                    ["senhaAtual"] = ["A senha atual está incorreta."]
                });
            case ResultadoAlteracaoSenha.UsuarioNaoEncontrado:
                throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
                {
                    ["usuario"] = ["Usuário não encontrado."]
                });
        }
    }

    private async Task<string> CadastrarAsync(
        RegistroRequest request,
        string tipoUsuario,
        CancellationToken cancellationToken)
    {
        var erros = new Dictionary<string, string[]>();

        var nome = ValidarObrigatorio(request.NomeCompleto, "nomeCompleto", erros);
        var email = ValidarObrigatorio(request.Email, "email", erros);
        var telefone = ValidarObrigatorio(request.Telefone, "telefone", erros);
        var senha = ValidarObrigatorio(request.Senha, "senha", erros);
        var confirmarSenha = ValidarObrigatorio(request.ConfirmarSenha, "confirmarSenha", erros);

        if (email is not null && !email.Contains('@'))
            AdicionarErro(erros, "email", "Informe um e-mail válido.");

        if (senha is not null && senha != confirmarSenha)
            AdicionarErro(erros, "confirmarSenha", "A confirmação de senha diverge da senha.");

        if (erros.Count > 0)
            throw new ExcecaoValidacaoAplicacao(erros);

        return await identidade.CriarUsuarioAsync(
            new DadosRegistro(nome!.Trim(), email!.Trim(), telefone!.Trim(), senha!),
            tipoUsuario,
            cancellationToken);
    }

    private static string? ValidarObrigatorio(
        string? valor,
        string campo,
        Dictionary<string, string[]> erros)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            AdicionarErro(erros, campo, "Campo obrigatório.");
            return null;
        }

        return valor;
    }

    private static string ValidarTexto(string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
            {
                [campo] = ["Campo obrigatório."]
            });

        return valor;
    }

    private static void AdicionarErro(Dictionary<string, string[]> erros, string campo, string mensagem)
    {
        if (!erros.TryGetValue(campo, out var mensagens))
        {
            mensagens = [];
            erros[campo] = mensagens;
        }

        erros[campo] = [.. mensagens, mensagem];
    }
}
