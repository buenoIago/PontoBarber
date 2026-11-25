using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace PontoBarber.Infra.Compartilhado.Orm;

/// <summary>
/// Substitui o sp_getapplock do SQL Server por advisory locks transacionais do
/// PostgreSQL. A chave textual é convertida de forma determinística em bigint;
/// o lock é liberado automaticamente no commit/rollback da transação.
/// </summary>
internal static class LockTransacionalPostgres
{
    private const string _sqlTimeout = "SET LOCAL lock_timeout = '5s'";
    private const string _sqlLock = "SELECT pg_advisory_xact_lock(@chave)";
    private const string _sqlTentarLock = "SELECT pg_try_advisory_xact_lock(@chave)";

    public static long Chave(string recurso)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(recurso));
        return BitConverter.ToInt64(hash, 0);
    }

    /// <summary>Aguarda o lock por até 5 s; false indica timeout (55P03).</summary>
    public static async Task<bool> AdquirirAsync(
        DbConnection conexao,
        DbTransaction transacao,
        string recurso,
        CancellationToken cancellationToken)
    {
        try
        {
            await ExecutarAsync(conexao, transacao, _sqlTimeout, null, cancellationToken);
            await ExecutarAsync(conexao, transacao, _sqlLock, Chave(recurso), cancellationToken);
            return true;
        }
        catch (PostgresException excecao) when (excecao.SqlState == "55P03")
        {
            return false;
        }
    }

    /// <summary>Tentativa sem espera, usada quando o chamador trata a indisponibilidade.</summary>
    public static async Task<bool> TentarAdquirirAsync(
        DbConnection conexao,
        DbTransaction transacao,
        string recurso,
        CancellationToken cancellationToken)
    {
        await using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = _sqlTentarLock;
        var parametro = comando.CreateParameter();
        parametro.ParameterName = "@chave";
        parametro.Value = Chave(recurso);
        comando.Parameters.Add(parametro);
        return (bool)(await comando.ExecuteScalarAsync(cancellationToken))!;
    }

    private static async Task ExecutarAsync(
        DbConnection conexao,
        DbTransaction transacao,
        string sql,
        long? chave,
        CancellationToken cancellationToken)
    {
        await using var comando = conexao.CreateCommand();
        comando.Transaction = transacao;
        comando.CommandText = sql;
        if (chave.HasValue)
        {
            var parametro = comando.CreateParameter();
            parametro.ParameterName = "@chave";
            parametro.Value = chave.Value;
            comando.Parameters.Add(parametro);
        }
        await comando.ExecuteNonQueryAsync(cancellationToken);
    }
}
