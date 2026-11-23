using Microsoft.AspNetCore.Identity;
using PontoBarber.Aplicacao.Auth;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Infra.Compartilhado.Identity;

/// <summary>
/// Implementação da porta de identidade com ASP.NET Core Identity.
/// O bloqueio progressivo (5 → 5 min, 10 → 30 min) é aplicado aqui porque
/// é detalhe do Identity, não regra de negócio da aplicação.
/// </summary>
public sealed class AdaptadorIdentidade(
    UserManager<Usuario> usuarios,
    SignInManager<Usuario> entradas) : IIdentidade
{
    private const int _limitePrimeiroBloqueio = 5;
    private const int _limiteSegundoBloqueio = 10;

    private static readonly string[] _codigosEmailDuplicado = ["DuplicateEmail", "DuplicateUserName"];
    private static readonly string[] _mensagemErroCadastro = ["Não foi possível concluir o cadastro."];

    public async Task<string> CriarUsuarioAsync(
        DadosRegistro dados,
        string tipoUsuario,
        CancellationToken cancellationToken)
    {
        var usuario = new Usuario
        {
            UserName = dados.Email,
            Email = dados.Email,
            PhoneNumber = dados.Telefone,
            FullName = dados.NomeCompleto,
            UserType = tipoUsuario
        };

        var resultado = await usuarios.CreateAsync(usuario, dados.Senha);

        if (!resultado.Succeeded)
        {
            var codigo = resultado.Errors.Select(erro => erro.Code).ToArray();
            if (codigo.Intersect(_codigosEmailDuplicado).Any())
            {
                throw new Aplicacao.Compartilhado.ExcecaoConflitoAplicacao(
                    Aplicacao.Compartilhado.CodigosErro.ValidacaoFalhou,
                    "Este e-mail já está cadastrado.");
            }

            throw new Aplicacao.Compartilhado.ExcecaoValidacaoAplicacao(
                codigo.ToDictionary(c => c, _ => _mensagemErroCadastro));
        }

        return usuario.Id;
    }

    public async Task<string> CriarClienteAsync(
        DadosRegistroCliente dados,
        CancellationToken cancellationToken)
    {
        // Conta existente (inclusive cliente antigo com senha): não alterar
        // dados nem hash. O cliente já pode solicitar OTP com este e-mail.
        if (await usuarios.FindByEmailAsync(dados.Email) is not null)
            throw new Aplicacao.Compartilhado.ExcecaoConflitoAplicacao(
                Aplicacao.Compartilhado.CodigosErro.ValidacaoFalhou,
                "Este e-mail já está cadastrado.");

        var usuario = new Usuario
        {
            UserName = dados.Email,
            Email = dados.Email,
            PhoneNumber = dados.Telefone,
            FullName = dados.NomeCompleto,
            UserType = "Customer"
        };

        // Sem senha: o Identity mantém PasswordHash nulo.
        var resultado = await usuarios.CreateAsync(usuario);
        if (!resultado.Succeeded)
        {
            var codigo = resultado.Errors.Select(erro => erro.Code).ToArray();
            if (codigo.Intersect(_codigosEmailDuplicado).Any())
                throw new Aplicacao.Compartilhado.ExcecaoConflitoAplicacao(
                    Aplicacao.Compartilhado.CodigosErro.ValidacaoFalhou,
                    "Este e-mail já está cadastrado.");

            throw new Aplicacao.Compartilhado.ExcecaoValidacaoAplicacao(
                codigo.ToDictionary(c => c, _ => _mensagemErroCadastro));
        }

        return usuario.Id;
    }

    public async Task AutenticarSemSenhaAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.FindByIdAsync(usuarioId)
            ?? throw new Aplicacao.Compartilhado.ExcecaoValidacaoAplicacao(
                new Dictionary<string, string[]> { ["usuario"] = ["Conta não encontrada."] });

        await entradas.SignInAsync(usuario, isPersistent: true);
    }

    public async Task<ResultadoAutenticacao> AutenticarAsync(
        string email,
        string senha,
        CancellationToken cancellationToken)
    {
        var usuario = await usuarios.FindByEmailAsync(email);
        // Clientes não possuem senha: resposta genérica, sem revelar o motivo.
        if (usuario is not null && usuario.UserType == "Customer")
            return ResultadoAutenticacao.CredenciaisInvalidas;

        if (usuario is null || await usuarios.IsLockedOutAsync(usuario))
            return usuario is null ? ResultadoAutenticacao.CredenciaisInvalidas : ResultadoAutenticacao.Bloqueado;

        var tentativasAntes = await usuarios.GetAccessFailedCountAsync(usuario);
        // Cookie persistente: a duração por perfil é aplicada no evento OnSigningIn.
        var resultado = await entradas.PasswordSignInAsync(
            usuario, senha, isPersistent: true, lockoutOnFailure: false);

        if (resultado.Succeeded)
            return ResultadoAutenticacao.Sucesso;

        if (resultado.IsLockedOut || await usuarios.IsLockedOutAsync(usuario))
            return ResultadoAutenticacao.Bloqueado;

        // O Identity bloqueia por DefaultLockoutTimeSpan ao atingir MaxFailedAccessAttempts.
        await usuarios.AccessFailedAsync(usuario);

        if (await usuarios.IsLockedOutAsync(usuario))
        {
            // Segundo patamar: 10 tentativas accumulated → 30 minutos.
            if (tentativasAntes + 1 >= _limiteSegundoBloqueio)
            {
                usuario.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(30);
                await usuarios.UpdateAsync(usuario);
            }

            return ResultadoAutenticacao.Bloqueado;
        }

        return ResultadoAutenticacao.CredenciaisInvalidas;
    }

    public async Task<DadosConta?> ObterContaAsync(string usuarioId, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.FindByIdAsync(usuarioId);
        if (usuario is null)
            return null;

        return new DadosConta(
            usuario.Id,
            usuario.FullName,
            usuario.Email ?? string.Empty,
            usuario.PhoneNumber,
            usuario.UserType ?? string.Empty);
    }

    public async Task<ResultadoAtualizacaoPerfil> AtualizarPerfilAsync(
        string usuarioId,
        string nomeCompleto,
        string email,
        string? telefone,
        CancellationToken cancellationToken)
    {
        var usuario = await usuarios.FindByIdAsync(usuarioId);
        if (usuario is null)
            return ResultadoAtualizacaoPerfil.UsuarioNaoEncontrado;

        if (!string.Equals(usuario.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var existente = await usuarios.FindByEmailAsync(email);
            if (existente is not null && existente.Id != usuario.Id)
                return ResultadoAtualizacaoPerfil.EmailDuplicado;

            var resultadoEmail = await usuarios.SetEmailAsync(usuario, email);
            if (!resultadoEmail.Succeeded)
                return resultadoEmail.Errors.Any(erro => _codigosEmailDuplicado.Contains(erro.Code))
                    ? ResultadoAtualizacaoPerfil.EmailDuplicado
                    : ResultadoAtualizacaoPerfil.Falha;
        }

        usuario.FullName = nomeCompleto;
        usuario.PhoneNumber = telefone;
        var resultado = await usuarios.UpdateAsync(usuario);

        if (resultado.Succeeded)
            return ResultadoAtualizacaoPerfil.Sucesso;

        return resultado.Errors.Any(erro => _codigosEmailDuplicado.Contains(erro.Code))
            ? ResultadoAtualizacaoPerfil.EmailDuplicado
            : ResultadoAtualizacaoPerfil.Falha;
    }

    public async Task<DadosConta?> ObterContaPorEmailAsync(
        string email, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.FindByEmailAsync(email);
        if (usuario is null) return null;
        return new DadosConta(
            usuario.Id,
            usuario.FullName,
            usuario.Email ?? string.Empty,
            usuario.PhoneNumber,
            usuario.UserType ?? string.Empty);
    }

    public async Task EncerrarSessaoAsync(CancellationToken cancellationToken) =>
        await entradas.SignOutAsync();

    public async Task<ResultadoAlteracaoSenha> AlterarSenhaAsync(
        string usuarioId,
        string senhaAtual,
        string novaSenha,
        CancellationToken cancellationToken)
    {
        var usuario = await usuarios.FindByIdAsync(usuarioId);
        if (usuario is null)
            return ResultadoAlteracaoSenha.UsuarioNaoEncontrado;
        if (usuario.UserType == "Customer")
            return ResultadoAlteracaoSenha.PerfilSemSenha;

        var resultado = await usuarios.ChangePasswordAsync(usuario, senhaAtual, novaSenha);

        if (resultado.Succeeded)
            return ResultadoAlteracaoSenha.Sucesso;

        return resultado.Errors.Any(erro => erro.Code == "PasswordMismatch")
            ? ResultadoAlteracaoSenha.SenhaAtualIncorreta
            : ResultadoAlteracaoSenha.SenhaNovaInvalida;
    }
}
