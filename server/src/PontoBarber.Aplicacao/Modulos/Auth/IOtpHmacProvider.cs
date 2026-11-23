namespace PontoBarber.Aplicacao.Modulos.Auth;

/// <summary>Chave estável usada no HMAC do código OTP.</summary>
public interface IOtpHmacProvider
{
    byte[] Chave { get; }
}
