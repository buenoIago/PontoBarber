namespace PontoBarber.Aplicacao.Modulos.Auth;

/// <summary>
/// Porta de envio de e-mail transacional. A implementação de produção depende
/// da decisão ABT-018; no desenvolvimento usa-se Mailpit por SMTP local.
/// </summary>
public interface IEnviadorEmail
{
    Task EnviarAsync(string destinatario, string assunto, string corpo, CancellationToken cancellationToken);
}
