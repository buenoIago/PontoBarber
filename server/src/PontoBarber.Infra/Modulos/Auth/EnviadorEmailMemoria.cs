using System.Collections.Concurrent;
using PontoBarber.Aplicacao.Modulos.Auth;

namespace PontoBarber.Infra.Modulos.Auth;

/// <summary>
/// Implementação em memória usada apenas por testes automatizados
/// (<c>Email:Modo=Memoria</c>). Mantém o último corpo enviado por destinatário;
/// não é registrada em produção e não substitui o provedor definitivo (ABT-018).
/// </summary>
public sealed class EnviadorEmailMemoria : IEnviadorEmail
{
    private static readonly ConcurrentDictionary<string, string> _mensagens = new(StringComparer.OrdinalIgnoreCase);

    public Task EnviarAsync(
        string destinatario, string assunto, string corpo, CancellationToken cancellationToken)
    {
        _mensagens[destinatario] = corpo;
        return Task.CompletedTask;
    }

    public static string? UltimoCorpo(string destinatario) =>
        _mensagens.TryGetValue(destinatario, out var corpo) ? corpo : null;

    public static void Limpar() => _mensagens.Clear();
}
