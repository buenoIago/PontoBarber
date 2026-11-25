using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Financeiro;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Restauração real em instância isolada. Usa a conexão local configurada em
/// PONTOBARBER_TEST_CONNECTION; nunca toca bancos compartilhados ou de produção.
/// O "backup" é um clone via CREATE DATABASE ... TEMPLATE, recurso nativo do
/// PostgreSQL que preserva os dados no ponto em que foi tirado.
/// </summary>
[TestClass]
public sealed class TestesRestauracao
{
    [TestMethod]
    [TestCategory("Restauracao")]
    public async Task Restauracao_BackupVerificado_RestauraDadosEIntegridade()
    {
        var externa = Environment.GetEnvironmentVariable("PONTOBARBER_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(externa))
        {
            Assert.Inconclusive(
                "PONTOBARBER_TEST_CONNECTION não configurada: nenhum backup utilizável " +
                "em ambiente isolado foi identificado; restauração não executada.");
            return;
        }

        var sufixo = Guid.NewGuid().ToString("N");
        var database = $"PontoBarberRestore_{sufixo}";
        var backup = $"PontoBarberRestoreBackup_{sufixo}";
        var administrador = new NpgsqlConnectionStringBuilder(externa) { Database = "postgres" }.ConnectionString;
        var conexaoBanco = new NpgsqlConnectionStringBuilder(externa) { Database = database }.ConnectionString;

        await ExecutarAsync(administrador, $"CREATE DATABASE \"{database}\"");

        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Modo"] = "Memoria",
                    ["ConnectionStrings:DefaultConnection"] = conexaoBanco,
                    ["Jobs:IntervaloMinutos"] = "60"
                })));

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            await contexto.Database.EnsureCreatedAsync();
            Semear(contexto);
        }

        // Backup de linha de base: clone transacional do banco isolado.
        NpgsqlConnection.ClearAllPools();
        await ExecutarAsync(administrador, $"CREATE DATABASE \"{backup}\" TEMPLATE \"{database}\"");

        // Alterações posteriores ao backup: marcador do RPO.
        var marcacao = DateTime.UtcNow;
        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            var usuario = await contexto.Set<Usuario>().FirstAsync();
            var agendamento = await contexto.Set<Agendamento>().IgnoreQueryFilters().FirstAsync();
            contexto.Set<Pagamento>().Add(Pagamento.Criar(
                agendamento.BarbeariaId, agendamento.Id, 999m, FormaPagamento.Pix,
                DateTime.UtcNow, usuario.Id));
            SemeaduraIntegridade.GarantirTudo(contexto);
            await contexto.SaveChangesAsync();
        }

        // Restauração sobre o banco isolado a partir do clone da linha de base.
        var cronometro = Stopwatch.StartNew();
        NpgsqlConnection.ClearAllPools();
        await ExecutarAsync(administrador, $"DROP DATABASE \"{database}\"");
        await ExecutarAsync(administrador, $"CREATE DATABASE \"{database}\" TEMPLATE \"{backup}\"");
        cronometro.Stop();
        NpgsqlConnection.ClearAllPools();

        using var verificacao = fabrica.Services.CreateScope();
        var ctx = verificacao.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
        var usuarios = await ctx.Set<Usuario>().CountAsync();
        var barbearias = await ctx.Set<Barbearia>().IgnoreQueryFilters().CountAsync();
        var agendamentos = await ctx.Set<Agendamento>().IgnoreQueryFilters().CountAsync();
        var pagamentos = await ctx.Set<Pagamento>().IgnoreQueryFilters().CountAsync();

        Console.WriteLine($"[RESTORE] database={database}, backup={backup}");
        Console.WriteLine($"[RESTORE] rto={cronometro.Elapsed.TotalSeconds:F2}s, " +
            $"rpo={DateTime.UtcNow.Subtract(marcacao).TotalSeconds:F2}s (marcador pós-backup)");
        Console.WriteLine($"[RESTORE] integridade: usuarios={usuarios}, barbearias={barbearias}, " +
            $"agendamentos={agendamentos}, pagamentos={pagamentos}");

        // Lima somente os bancos isolados criados por este teste.
        await ExecutarAsync(administrador, $"DROP DATABASE IF EXISTS \"{backup}\"");
        await ExecutarAsync(administrador, $"DROP DATABASE IF EXISTS \"{database}\"");

        Assert.AreEqual(1, usuarios);
        Assert.AreEqual(1, barbearias);
        Assert.AreEqual(1, agendamentos);
        Assert.AreEqual(0, pagamentos, "O pagamento inserido após o backup não deve existir após a restauração.");
        Assert.IsTrue(cronometro.Elapsed < TimeSpan.FromHours(2), "RTO acima do limite de 2 h.");
    }

    private static async Task ExecutarAsync(string conexao, string sql)
    {
        await using var ligacao = new NpgsqlConnection(conexao);
        await ligacao.OpenAsync();
        await using var comando = ligacao.CreateCommand();
        comando.CommandText = sql;
        comando.CommandTimeout = 300;
        await comando.ExecuteNonQueryAsync();
    }

    private static void Semear(ContextoPontoBarber contexto)
    {
        var usuario = new Usuario
        {
            Id = Guid.NewGuid().ToString(),
            UserName = $"restore-{Guid.NewGuid():N}@exemplo.com",
            Email = $"restore-{Guid.NewGuid():N}@exemplo.com",
            FullName = "Usuario Restauracao",
            UserType = "Customer"
        };
        contexto.Set<Usuario>().Add(usuario);

        var barbearia = Barbearia.Criar(
            "Barbearia Restauração", "(11) 99999-9999", "contato@exemplo.com",
            "São Paulo", "Rua Central, 100", null, "RESTORE1", "America/Sao_Paulo", Guid.NewGuid());
        contexto.Set<Barbearia>().Add(barbearia);

        var agendamento = Agendamento.Criar(
            barbearia.Id,
            usuario.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(1),
            "Corte restauração",
            40,
            80m,
            null,
            OrigemAgendamento.Publico);
        contexto.Set<Agendamento>().Add(agendamento);
        SemeaduraIntegridade.GarantirTudo(contexto);
        contexto.SaveChanges();
    }
}
