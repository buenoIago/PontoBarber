using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PontoBarber.Dominio.Modulos.Auth;
using PontoBarber.Infra.Compartilhado.Orm;

namespace PontoBarber.Infra.Modulos.Auth;

public sealed class RepositorioCodigoOtpEmOrm(ContextoPontoBarber contexto) : ICodigoOtpRepositorio
{
    public Task<CodigoOtp?> ObterAsync(string email, CancellationToken cancellationToken) =>
        contexto.Set<CodigoOtp>()
            .SingleOrDefaultAsync(
                codigo => codigo.Email == CodigoOtp.Normalizar(email),
                cancellationToken);

    public async Task SalvarAsync(CodigoOtp codigo, CancellationToken cancellationToken)
    {
        var existente = await ObterAsync(codigo.Email, cancellationToken);
        if (existente is null)
            contexto.Set<CodigoOtp>().Add(codigo);
        else if (existente.Expirou(codigo.CriadoEmUtc))
            existente.Renovar(codigo.Salt, codigo.Hash, codigo.CriadoEmUtc);
        else
            existente.Reenviar(codigo.Salt, codigo.Hash, codigo.CriadoEmUtc);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RegistrarTentativaInvalidaAsync(
        Guid codigoId, CancellationToken cancellationToken)
    {
        var linhas = await contexto.Set<CodigoOtp>()
            .Where(codigo => codigo.Id == codigoId && codigo.ConsumidoEmUtc == null)
            .ExecuteUpdateAsync(
                item => item.SetProperty(codigo => codigo.TentativasInvalidas, codigo => codigo.TentativasInvalidas + 1),
                cancellationToken);
        return linhas > 0;
    }

    public async Task<bool> ConsumirAsync(
        Guid codigoId, DateTime agoraUtc, CancellationToken cancellationToken)
    {
        await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);
        var conexao = (SqlConnection)contexto.Database.GetDbConnection();
        var transacaoDb = (SqlTransaction)transacao.GetDbTransaction();
        var chave = $"pb:auth:otp:{codigoId}";

        using var comandoLock = conexao.CreateCommand();
        comandoLock.Transaction = transacaoDb;
        comandoLock.CommandType = CommandType.StoredProcedure;
        comandoLock.CommandText = "sp_getapplock";
        comandoLock.Parameters.AddWithValue("@Resource", chave);
        comandoLock.Parameters.AddWithValue("@LockMode", "Exclusive");
        comandoLock.Parameters.AddWithValue("@LockOwner", "Transaction");
        comandoLock.Parameters.AddWithValue("@LockTimeout", 5000);
        var retorno = new SqlParameter("@resultado", SqlDbType.Int)
        {
            Direction = ParameterDirection.ReturnValue
        };
        comandoLock.Parameters.Add(retorno);
        await comandoLock.ExecuteNonQueryAsync(cancellationToken);

        if ((int)retorno.Value! < 0)
        {
            await transacao.RollbackAsync(cancellationToken);
            return false;
        }

        var limite = DateTime.UtcNow.AddMinutes(-CodigoOtp.ValidadeMinutos);
        var linhas = await contexto.Set<CodigoOtp>()
            .Where(codigo => codigo.Id == codigoId &&
                             codigo.ConsumidoEmUtc == null &&
                             codigo.ExpiraEmUtc > agoraUtc &&
                             codigo.CriadoEmUtc > limite &&
                             codigo.TentativasInvalidas < CodigoOtp.MaximoTentativas)
            .ExecuteUpdateAsync(
                item => item.SetProperty(codigo => codigo.ConsumidoEmUtc, agoraUtc),
                cancellationToken);

        await transacao.CommitAsync(cancellationToken);
        return linhas > 0;
    }
}
