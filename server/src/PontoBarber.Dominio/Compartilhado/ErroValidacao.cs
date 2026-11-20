namespace PontoBarber.Dominio.Compartilhado;

/// <summary>
/// Erro de validação de uma entidade de domínio, reaproveitado pela
/// camada de aplicação na conversão para erros por campo.
/// </summary>
public sealed record ErroValidacao(string Campo, string Mensagem);
