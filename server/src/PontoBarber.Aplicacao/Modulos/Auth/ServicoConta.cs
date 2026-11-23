using System.Security.Cryptography;
using System.Text;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Aplicacao.Modulos.Auth;
using PontoBarber.Dominio.Modulos.Auth;

namespace PontoBarber.Aplicacao.Auth;

/// <summary>
/// Cadastro, OTP do cliente e login por senha dos profissionais. O tipo do
/// usuário é sempre definido pelo caso de uso, nunca pelo corpo da requisição.
/// </summary>
public sealed class ServicoConta(
    IIdentidade identidade,
    ICodigoOtpRepositorio codigosOtp,
    IEnviadorEmail enviadorEmail,
    IOtpHmacProvider hmacProvider) : IDisposable
{
    public const string TipoCliente = "Customer";
    public const string TipoBarbeiro = "Professional";

    private const string _assuntoOtp = "Seu código de acesso PontoBarber";
    private readonly HMACSHA256 _hmac = new(hmacProvider.Chave);

    public Task<string> CadastrarClienteAsync(
        RegistroClienteRequest request, CancellationToken cancellationToken) =>
        CadastrarClienteInternoAsync(request, cancellationToken);

    public Task<string> CadastrarBarbeiroAsync(
        RegistroProfissionalRequest request, CancellationToken cancellationToken)
    {
        var erros = new Dictionary<string, string[]>();
        var nome = ValidarObrigatorio(request.NomeCompleto, "nomeCompleto", erros);
        var email = ValidarObrigatorio(request.Email, "email", erros);
        var telefone = ValidarObrigatorio(request.Telefone, "telefone", erros);
        var senha = ValidarObrigatorio(request.Senha, "senha", erros);
        var confirmar = ValidarObrigatorio(request.ConfirmarSenha, "confirmarSenha", erros);

        ValidarEmail(email, erros);
        if (senha is not null && senha != confirmar)
            AdicionarErro(erros, "confirmarSenha", "A confirmação de senha diverge da senha.");
        if (erros.Count > 0) throw new ExcecaoValidacaoAplicacao(erros);

        return identidade.CriarUsuarioAsync(
            new DadosRegistro(nome!.Trim(), email!.Trim(), telefone!.Trim(), senha!),
            TipoBarbeiro,
            cancellationToken);
    }

    public async Task<ResultadoAutenticacao> AutenticarAsync(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var email = ValidarTexto(request.Email, "email");
        var senha = ValidarTexto(request.Senha, "senha");
        return await identidade.AutenticarAsync(email.Trim(), senha, cancellationToken);
    }

    /// <summary>
    /// Solicita o OTP. A resposta é sempre a mesma, independentemente de o
    /// e-mail existir, para evitar enumeração de contas.
    /// </summary>
    public async Task SolicitarOtpAsync(
        SolicitarOtpRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@')) return;

        var email = request.Email.Trim();
        var conta = await identidade.ObterContaPorEmailAsync(email, cancellationToken);
        if (conta is null || conta.Tipo != TipoCliente) return;

        var agora = DateTime.UtcNow;
        var existente = await codigosOtp.ObterAsync(email, cancellationToken);
        if (existente is not null && !existente.Expirou(agora))
        {
            if (!existente.RespeitaIntervalo(agora) || !existente.PodeReenviar(agora)) return;
        }

        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000)
            .ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = _hmac.ComputeHash(Encoding.UTF8.GetBytes(codigo + Convert.ToBase64String(salt)));

        var novo = CodigoOtp.Criar(
            email, Convert.ToBase64String(salt), Convert.ToHexString(hash), agora);
        await codigosOtp.SalvarAsync(novo, cancellationToken);

        await enviadorEmail.EnviarAsync(
            email,
            _assuntoOtp,
            $"Seu código de acesso é {codigo}. Ele expira em {CodigoOtp.ValidadeMinutos} minutos.",
            cancellationToken);
    }

    public async Task VerificarOtpAsync(
        VerificarOtpRequest request, CancellationToken cancellationToken)
    {
        var email = ValidarTexto(request.Email, "email");
        var codigo = ValidarTexto(request.Codigo, "codigo").Trim();
        if (codigo.Length != CodigoOtp.Digitos || !codigo.All(char.IsDigit))
            throw ErroGenerico();

        var conta = await identidade.ObterContaPorEmailAsync(email.Trim(), cancellationToken);
        if (conta is null || conta.Tipo != TipoCliente) throw ErroGenerico();

        var registro = await codigosOtp.ObterAsync(email.Trim(), cancellationToken);
        var agora = DateTime.UtcNow;
        if (registro is null || !registro.Elegivel(agora)) throw ErroGenerico();

        var esperado = _hmac.ComputeHash(
            Encoding.UTF8.GetBytes(codigo + registro.Salt));
        if (!CryptographicOperations.FixedTimeEquals(esperado, Convert.FromHexString(registro.Hash)))
        {
            await codigosOtp.RegistrarTentativaInvalidaAsync(registro.Id, cancellationToken);
            throw ErroGenerico();
        }

        if (!await codigosOtp.ConsumirAsync(registro.Id, agora, cancellationToken))
            throw ErroGenerico();

        await identidade.AutenticarSemSenhaAsync(conta.Id, cancellationToken);
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
        var conta = await ObterContaAsync(usuarioId, cancellationToken);
        if (conta.Tipo == TipoCliente)
            throw new ExcecaoAutorizacaoAplicacao(
                CodigosErro.AutenticacaoNegada, "Clientes não possuem senha.");

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

        if (erros.Count > 0) throw new ExcecaoValidacaoAplicacao(erros);

        var resultado = await identidade.AlterarSenhaAsync(
            usuarioId, senhaAtual!, novaSenha!, cancellationToken);

        switch (resultado)
        {
            case ResultadoAlteracaoSenha.SenhaAtualIncorreta:
                throw new ExcecaoValidacaoAplicacao(new Dictionary<string, string[]>
                {
                    ["senhaAtual"] = ["A senha atual está incorreta."]
                });
            case ResultadoAlteracaoSenha.UsuarioNaoEncontrado:
                throw new ExcecaoNaoEncontradoAplicacao(
                    CodigosErro.TenantNaoEncontrado, "Conta não encontrada.");
            case ResultadoAlteracaoSenha.PerfilSemSenha:
                throw new ExcecaoAutorizacaoAplicacao(
                    CodigosErro.AutenticacaoNegada, "Clientes não possuem senha.");
        }
    }

    private async Task<string> CadastrarClienteInternoAsync(
        RegistroClienteRequest request, CancellationToken cancellationToken)
    {
        var erros = new Dictionary<string, string[]>();
        var nome = ValidarObrigatorio(request.NomeCompleto, "nomeCompleto", erros);
        var email = ValidarObrigatorio(request.Email, "email", erros);
        var telefone = ValidarObrigatorio(request.Telefone, "telefone", erros);
        ValidarEmail(email, erros);
        if (erros.Count > 0) throw new ExcecaoValidacaoAplicacao(erros);

        return await identidade.CriarClienteAsync(
            new DadosRegistroCliente(nome!.Trim(), email!.Trim(), telefone!.Trim()),
            cancellationToken);
    }

    private static void ValidarEmail(string? email, Dictionary<string, string[]> erros)
    {
        if (email is not null && !email.Contains('@'))
            AdicionarErro(erros, "email", "Informe um e-mail válido.");
    }

    private static ExcecaoAutenticacaoAplicacao ErroGenerico() =>
        new(CodigosErro.AutenticacaoCredenciaisInvalidas, "E-mail ou código inválidos.");

    private static string? ValidarObrigatorio(
        string? valor, string campo, Dictionary<string, string[]> erros)
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

    public void Dispose() => _hmac.Dispose();
}
