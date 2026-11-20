using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Inicializa o SQL Server em container apenas quando um teste realmente
/// solicita uma connection string, e o descarta no encerramento do assembly.
/// Cada teste utiliza um banco com nome único, criado e removido por ele.
/// </summary>
[TestClass]
public static class ConfiguracaoTestesIntegracao
{
    private static readonly Lazy<SqlServerFixture> _fixture = new(() => new SqlServerFixture());
    private static readonly Lazy<Task> _inicializacao = new(() => _fixture.Value.InicializarAsync());

    public static SqlServerFixture Container
    {
        get
        {
            _inicializacao.Value.GetAwaiter().GetResult();
            return _fixture.Value;
        }
    }

    [AssemblyCleanup]
    public static void Finalizar(TestContext contexto)
    {
        if (!_inicializacao.IsValueCreated)
            return;

        var tarefa = _inicializacao.Value;

        if (tarefa.IsFaulted || tarefa.IsCanceled)
        {
            // Docker indisponível: libera apenas recursos parciais sem reobservar a falha.
            _fixture.Value.DisposeAsync().AsTask().GetAwaiter().GetResult();
            return;
        }

        tarefa.GetAwaiter().GetResult();
        _fixture.Value.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}

public sealed class SqlServerFixture : IAsyncDisposable
{
    private const string _imagem = "mcr.microsoft.com/mssql/server:2022-CU28-ubuntu-22.04";

    private MsSqlContainer? _container;

    public async Task InicializarAsync()
    {
        // Política do SQL Server: >= 8 caracteres, maiúscula, minúscula, dígito e símbolo.
        var senha = $"P@ssw0rd!{Guid.NewGuid():N}"[..24];

        _container = new MsSqlBuilder(_imagem)
            .WithPassword(senha)
            .Build();

        await _container.StartAsync();
    }

    public string ObterStringConexao(string database)
    {
        if (_container is null)
            throw new InvalidOperationException("O container de testes não foi inicializado.");

        var construtor = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = database,
            TrustServerCertificate = true
        };

        return construtor.ConnectionString;
    }

    /// <summary>
    /// Cria um catálogo exclusivo no container e devolve a connection string.
    /// Usado quando o teste precisa de um banco existente, mas não deve criar
    /// schema no <c>master</c> nem compartilhar estado com outros testes.
    /// </summary>
    public string CriarBancoIsolado(string prefixo)
    {
        var database = $"{prefixo}_{Guid.NewGuid():N}";

        using (var conexao = new SqlConnection(ObterStringConexao("master")))
        {
            conexao.Open();
            using var comando = conexao.CreateCommand();
            comando.CommandText = $"CREATE DATABASE [{database}]";
            comando.ExecuteNonQuery();
        }

        return ObterStringConexao(database);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}
