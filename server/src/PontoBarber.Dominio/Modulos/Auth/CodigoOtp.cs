using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Dominio.Modulos.Auth;

/// <summary>
/// Código OTP do cliente. O código nunca é persistido em texto puro: guarda-se
/// apenas o salt e o HMAC-SHA256. Um único registro por e-mail garante que um
/// reenvio invalide o código anterior.
/// </summary>
public sealed class CodigoOtp : EntidadeBase<CodigoOtp>
{
    public const int Digitos = 6;
    public const int ValidadeMinutos = 5;
    public const int MaximoTentativas = 5;
    public const int MaximoReenvios = 3;
    public const int SegundosEntreReenvios = 60;

    private CodigoOtp()
    {
    }

    public string Email { get; private set; } = string.Empty;
    public string Salt { get; private set; } = string.Empty;
    public string Hash { get; private set; } = string.Empty;
    public DateTime CriadoEmUtc { get; private set; }
    public DateTime ExpiraEmUtc { get; private set; }
    public DateTime? ConsumidoEmUtc { get; private set; }
    public int TentativasInvalidas { get; private set; }
    public int Reenvios { get; private set; }

    public static CodigoOtp Criar(string email, string salt, string hash, DateTime agoraUtc) =>
        new()
        {
            Email = Normalizar(email),
            Salt = salt,
            Hash = hash,
            CriadoEmUtc = agoraUtc,
            ExpiraEmUtc = agoraUtc.AddMinutes(ValidadeMinutos)
        };

    public static string Normalizar(string email) =>
        (email ?? string.Empty).Trim().ToUpperInvariant();

    /// <summary>Permite no máximo <see cref="MaximoReenvios"/> por janela de validade.</summary>
    public bool PodeReenviar(DateTime agoraUtc) =>
        !Expirou(agoraUtc) && Reenvios < MaximoReenvios;

    public bool RespeitaIntervalo(DateTime agoraUtc) =>
        agoraUtc >= CriadoEmUtc.AddSeconds(SegundosEntreReenvios);

    public void Reenviar(string salt, string hash, DateTime agoraUtc)
    {
        Salt = salt;
        Hash = hash;
        CriadoEmUtc = agoraUtc;
        ExpiraEmUtc = agoraUtc.AddMinutes(ValidadeMinutos);
        ConsumidoEmUtc = null;
        TentativasInvalidas = 0;
        Reenvios++;
    }

    /// <summary>Inicia um novo ciclo após a expiração, zerando o contador de reenvios.</summary>
    public void Renovar(string salt, string hash, DateTime agoraUtc)
    {
        Reenviar(salt, hash, agoraUtc);
        Reenvios = 0;
    }

    public void RegistrarTentativaInvalida() => TentativasInvalidas++;

    public bool Expirou(DateTime agoraUtc) => ExpiraEmUtc <= agoraUtc;

    public bool Elegivel(DateTime agoraUtc) =>
        ConsumidoEmUtc is null && !Expirou(agoraUtc) && TentativasInvalidas < MaximoTentativas;

    public void Consumir(DateTime agoraUtc) => ConsumidoEmUtc = agoraUtc;

    public override IReadOnlyList<ErroValidacao> Validar()
    {
        var erros = new List<ErroValidacao>();
        if (string.IsNullOrWhiteSpace(Email)) erros.Add(new ErroValidacao("email", "Campo obrigatório."));
        if (string.IsNullOrWhiteSpace(Salt) || string.IsNullOrWhiteSpace(Hash))
            erros.Add(new ErroValidacao("codigo", "Hash do código obrigatório."));
        if (CriadoEmUtc.Kind != DateTimeKind.Utc || ExpiraEmUtc.Kind != DateTimeKind.Utc)
            erros.Add(new ErroValidacao("periodo", "Informe as datas em UTC."));
        if (ExpiraEmUtc <= CriadoEmUtc)
            erros.Add(new ErroValidacao("expiraEmUtc", "A expiração deve ser posterior à criação."));
        return erros;
    }

    public override void Atualizar(CodigoOtp entidadeAtualizada) =>
        throw new InvalidOperationException("O código OTP não pode ser alterado por atualização genérica.");
}
