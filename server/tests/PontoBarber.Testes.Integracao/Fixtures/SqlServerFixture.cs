using Npgsql;
using Testcontainers.PostgreSql;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Inicializa o PostgreSQL em container apenas quando um teste realmente
/// solicita uma connection string, e o descarta no encerramento do assembly.
/// Cada teste utiliza um banco com nome único, criado e removido por ele.
/// </summary>
[TestClass]
public static class ConfiguracaoTestesIntegracao
{
    private static readonly Lazy<PostgresFixture> _fixture = new(() => new PostgresFixture());
    private static readonly Lazy<Task> _inicializacao = new(() => _fixture.Value.InicializarAsync());

    public static PostgresFixture Container
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

public sealed class PostgresFixture : IAsyncDisposable
{
    private const string _imagem = "postgres:16.8-alpine";

    private PostgreSqlContainer? _container;
    private string _stringBase = string.Empty;

    public Task InicializarAsync()
    {
        // Ambientes sem daemon Docker podem fornecer um PostgreSQL isolado por
        // variável de ambiente. Nenhum banco compartilhado é aceito.
        var externa = Environment.GetEnvironmentVariable("PONTOBARBER_TEST_CONNECTION");
        if (!string.IsNullOrWhiteSpace(externa))
        {
            _stringBase = externa;
            return Task.CompletedTask;
        }

        return InicializarContainerAsync();
    }

    private async Task InicializarContainerAsync()
    {
        _container = new PostgreSqlBuilder(_imagem)
            .WithDatabase("pontobarber")
            .WithUsername("pontobarber")
            .WithPassword($"pb{Guid.NewGuid():N}"[..24])
            .Build();

        await _container.StartAsync();
    }

    public string ObterStringConexao(string database)
    {
        var origem = _stringBase;
        if (string.IsNullOrWhiteSpace(origem))
        {
            if (_container is null)
                throw new InvalidOperationException("O container de testes não foi inicializado.");
            origem = _container.GetConnectionString();
        }

        // Cada teste usa um banco único; sem pooling, as conexões são fechadas
        // de fato e não esgotam max_connections do PostgreSQL.
        return new NpgsqlConnectionStringBuilder(origem)
        {
            Database = database,
            Pooling = false
        }.ConnectionString;
    }

    /// <summary>
    /// Cria um catálogo exclusivo no container e devolve a connection string.
    /// Usado quando o teste precisa de um banco existente, mas não deve criar
    /// schema no banco padrão nem compartilhar estado com outros testes.
    /// </summary>
    public string CriarBancoIsolado(string prefixo)
    {
        var database = $"{prefixo}_{Guid.NewGuid():N}";

        using (var conexao = new NpgsqlConnection(ObterStringConexao("postgres")))
        {
            conexao.Open();
            using var comando = conexao.CreateCommand();
            comando.CommandText = $"CREATE DATABASE \"{database}\"";
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
