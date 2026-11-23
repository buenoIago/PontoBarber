using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using PontoBarber.Aplicacao.Modulos.Auth;

namespace PontoBarber.Infra.Modulos.Auth;

/// <summary>
/// Envio por SMTP. No desenvolvimento aponta para o Mailpit local
/// (localhost:1025). Nenhum provedor de produção é escolhido nesta etapa.
/// </summary>
public sealed class EnviadorEmailSmtp(IConfiguration configuracao) : IEnviadorEmail
{
    public Task EnviarAsync(
        string destinatario, string assunto, string corpo, CancellationToken cancellationToken)
    {
        var host = configuracao["Email:Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException(
                "Envio de e-mail não configurado (Email:Smtp:Host). Defina o provedor conforme ABT-018.");

        var porta = int.TryParse(configuracao["Email:Smtp:Port"], out var valor) ? valor : 1025;
        var remetente = configuracao["Email:From"] ?? "nao-responder@pontobarber.local";

        var mensagem = new MimeMessage();
        mensagem.From.Add(MailboxAddress.Parse(remetente));
        mensagem.To.Add(MailboxAddress.Parse(destinatario));
        mensagem.Subject = assunto;
        mensagem.Body = new TextPart("plain") { Text = corpo };

        using var cliente = new SmtpClient();
        cliente.Connect(host, porta, SecureSocketOptions.None, cancellationToken);
        cliente.Send(new SendRequest(mensagem), cancellationToken);
        cliente.Disconnect(true, cancellationToken);
        return Task.CompletedTask;
    }
}
