using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PontoBarber.Dominio.Modulos.Agendamentos;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Financeiro;
using PontoBarber.Infra.Compartilhado.Orm;
using PontoBarber.Infra.Identity;

namespace PontoBarber.Testes.Integracao;

/// <summary>
/// Restauração real em instância isolada. Usa a conexão local configurada em
/// PONTOBARBER_TEST_CONNECTION; nunca toca bancos compartilhados ou de produção.
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
        var backup = Path.Combine(
            @"C:\Users\Cliente\AppData\Local\Temp\opencode\validacao", $"pb-restore-{sufixo}.bak");
        Directory.CreateDirectory(Path.GetDirectoryName(backup)!);

        var construtorMaster = new SqlConnectionStringBuilder(externa) { InitialCatalog = "master" };
        var construtorBanco = new SqlConnectionStringBuilder(externa)
        {
            InitialCatalog = database,
            TrustServerCertificate = true
        };

        using (var conexao = new SqlConnection(construtorMaster.ToString()))
        {
            conexao.Open();
            using var comando = conexao.CreateCommand();
            comando.CommandText = $"CREATE DATABASE [{database}]";
            comando.ExecuteNonQuery();
        }

        using var fabrica = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuracao) =>
                configuracao.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:Modo"] = "Memoria",
                    ["ConnectionStrings:DefaultConnection"] = construtorBanco.ToString(),
                    ["Jobs:IntervaloMinutos"] = "60"
                })));

        using (var escopo = fabrica.Services.CreateScope())
        {
            var contexto = escopo.ServiceProvider.GetRequiredService<ContextoPontoBarber>();
            await contexto.Database.EnsureCreatedAsync();
            Semear(contexto);
        }

        // Backup de linha de base.
        await ExecutarAsync(construtorMaster.ToString(),
            $"BACKUP DATABASE [{database}] TO DISK = N'{backup}' WITH INIT");

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
            await contexto.SaveChangesAsync();
        }

        // Restauração sobre o banco isolado (single-user para evitar conexões ativas).
        var cronometro = Stopwatch.StartNew();
        await ExecutarAsync(construtorMaster.ToString(),
            $"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
        await ExecutarAsync(construtorMaster.ToString(),
            $"RESTORE DATABASE [{database}] FROM DISK = N'{backup}' WITH REPLACE");
        await ExecutarAsync(construtorMaster.ToString(),
            $"ALTER DATABASE [{database}] SET MULTI_USER");
        cronometro.Stop();
        SqlConnection.ClearAllPools();

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

        Assert.AreEqual(1, usuarios);
        Assert.AreEqual(1, barbearias);
        Assert.AreEqual(1, agendamentos);
        Assert.AreEqual(0, pagamentos, "O pagamento inserido após o backup não deve existir após a restauração.");
        Assert.IsTrue(cronometro.Elapsed < TimeSpan.FromHours(2), "RTO acima do limite de 2 h.");
    }

    private static async Task ExecutarAsync(string conexao, string sql)
    {
        await using var ligacao = new SqlConnection(conexao);
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
        contexto.SaveChanges();
    }
}
