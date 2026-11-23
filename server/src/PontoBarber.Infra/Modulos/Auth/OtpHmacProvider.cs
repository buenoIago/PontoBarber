using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using PontoBarber.Aplicacao.Modulos.Auth;

namespace PontoBarber.Infra.Modulos.Auth;

/// <summary>
/// Chave do HMAC do OTP. Pode vir da configuração; se ausente, é gerada uma vez
/// por processo (códigos emitidos antes de um restart deixam de ser válidos).
/// </summary>
public sealed class OtpHmacProvider(IConfiguration configuracao) : IOtpHmacProvider
{
    private static readonly byte[] _chaveProcesso = RandomNumberGenerator.GetBytes(32);

    public byte[] Chave
    {
        get
        {
            var configurada = configuracao["Seguranca:OtpHmacChave"];
            return string.IsNullOrWhiteSpace(configurada)
                ? _chaveProcesso
                : Convert.FromBase64String(configurada);
        }
    }
}
