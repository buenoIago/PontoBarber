using System.Reflection;
using PontoBarber.Aplicacao.Modulos.Jobs;
using PontoBarber.Dominio.Compartilhado;

namespace PontoBarber.Testes.Arquitetura;

/// <summary>
/// ADR-027: nenhuma biblioteca de agendamento pode ser referenciada por
/// Dominio ou Aplicacao. O adaptador provisório existe somente em Infra.
/// </summary>
[TestClass]
public class TestesDependenciasScheduler
{
    private static readonly string[] _bibliotecasProibidas =
        ["Quartz", "Hangfire", "Scheduler", "Microsoft.Extensions.Hosting"];

    [TestMethod]
    public void Dominio_NaoReferenciaBibliotecaDeAgendamento()
    {
        var referencias = typeof(EntidadeBase<>).Assembly
            .GetReferencedAssemblies()
            .Select(referencia => referencia.Name ?? string.Empty)
            .ToList();

        var proibidas = referencias
            .Where(referencia => _bibliotecasProibidas.Any(proibida =>
                referencia.Contains(proibida, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.AreEqual(0, proibidas.Count, string.Join(", ", proibidas));
    }

    [TestMethod]
    public void Aplicacao_NaoReferenciaBibliotecaDeAgendamento()
    {
        var referencias = typeof(ServicoExecucaoJobs).Assembly
            .GetReferencedAssemblies()
            .Select(referencia => referencia.Name ?? string.Empty)
            .ToList();

        var proibidas = referencias
            .Where(referencia => _bibliotecasProibidas.Any(proibida =>
                referencia.Contains(proibida, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Assert.AreEqual(0, proibidas.Count, string.Join(", ", proibidas));
    }

    [TestMethod]
    public void ContratoDeExecucao_PertenceAAplicacao()
    {
        Assert.AreEqual("PontoBarber.Aplicacao", typeof(IProcessoPeriodico).Assembly.GetName().Name);
    }
}
