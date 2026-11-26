using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PontoBarber.Api.Modulos.Auth;
using PontoBarber.Infra.Modulos.Auth;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Helpers compartilhados de autenticação de cliente via OTP. Os testes usam
/// o enviador em memória (Email:Modo=Memoria), nunca SMTP real.
/// </summary>
public static partial class AuthTestHelper
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public static async Task<string> RegistrarClienteAsync(
        HttpClient cliente, string nome = "Cliente Teste", string? email = null)
    {
        email ??= $"cliente-{Guid.NewGuid():N}@exemplo.com";
        var resposta = await PostAsync(cliente, "/api/v1/auth/cadastro/cliente", new
        {
            nomeCompleto = nome,
            email,
            telefone = "11999999999"
        });
        Assert.AreEqual(HttpStatusCode.Created, resposta.StatusCode);
        return email;
    }

    public static async Task<string> SolicitarCodigoAsync(HttpClient cliente, string email)
    {
        EnviadorEmailMemoria.Limpar();
        var solicitacao = await PostAsync(cliente, "/api/v1/auth/otp/solicitar", new { email });
        Assert.AreEqual(HttpStatusCode.Accepted, solicitacao.StatusCode);
        return ObterCodigo(email);
    }

    public static async Task LoginClienteAsync(HttpClient cliente, string email)
    {
        var codigo = await SolicitarCodigoAsync(cliente, email);
        var verificacao = await PostAsync(cliente, "/api/v1/auth/otp/verificar", new { email, codigo });
        Assert.AreEqual(HttpStatusCode.NoContent, verificacao.StatusCode);
    }

    public static async Task RegistrarEEntrarClienteAsync(
        HttpClient cliente, string nome = "Cliente Teste", string? email = null)
    {
        var efetivo = await RegistrarClienteAsync(cliente, nome, email);
        await LoginClienteAsync(cliente, efetivo);
    }

    public static string ObterCodigo(string email)
    {
        var corpo = EnviadorEmailMemoria.UltimoCorpo(email);
        var codigo = CodigoOtpRegex().Match(corpo ?? string.Empty).Value;
        Assert.AreEqual(6, codigo.Length, "Código OTP não foi capturado pelo enviador em memória.");
        return codigo;
    }

    public static async Task<HttpResponseMessage> PostAsync(
        HttpClient cliente, string rota, object corpo)
    {
        var respostaGet = await cliente.GetAsync("/health/live");
        var valor = respostaGet.Headers.GetValues("Set-Cookie")
            .First(item => item.Contains(ConfiguracaoAuth.CookieRequestToken, StringComparison.Ordinal));
        var trecho = valor.Split(';')[0];
        var token = Uri.UnescapeDataString(trecho[(trecho.IndexOf('=') + 1)..]);

        using var requisicao = new HttpRequestMessage(HttpMethod.Post, rota)
        {
            Content = new StringContent(JsonSerializer.Serialize(corpo, _json), Encoding.UTF8, "application/json")
        };
        requisicao.Headers.Add(ConfiguracaoAuth.HeaderCsrf, token);
        return await cliente.SendAsync(requisicao);
    }

    [GeneratedRegex(@"\b\d{6}\b")]
    private static partial Regex CodigoOtpRegex();
}
