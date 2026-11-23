namespace PontoBarber.Dominio.Modulos.Auth;

/// <summary>Persistência e consumo atômico do código OTP.</summary>
public interface ICodigoOtpRepositorio
{
    Task<CodigoOtp?> ObterAsync(string email, CancellationToken cancellationToken);

    Task SalvarAsync(CodigoOtp codigo, CancellationToken cancellationToken);

    Task<bool> RegistrarTentativaInvalidaAsync(Guid codigoId, CancellationToken cancellationToken);

    /// <summary>Consome o código somente se ainda elegível, de forma atômica.</summary>
    Task<bool> ConsumirAsync(Guid codigoId, DateTime agoraUtc, CancellationToken cancellationToken);
}
