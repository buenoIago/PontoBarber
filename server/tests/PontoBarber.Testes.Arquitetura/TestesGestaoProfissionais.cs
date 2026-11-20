using PontoBarber.Api.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Modulos.Profissionais;

namespace PontoBarber.Testes.Arquitetura;

/// <summary>
/// Task 4.4: fronteiras da gestão de profissionais e invariantes de estado.
/// </summary>
[TestClass]
public class TestesGestaoProfissionais
{
    [TestMethod]
    public void VinculoProfissional_EstadoSomentePorOperacoesExplicitas()
    {
        foreach (var nome in new[] { "Ativar", "Desativar", "Remover" })
            Assert.IsNotNull(typeof(VinculoProfissional).GetMethod(nome), $"Método {nome} ausente.");

        foreach (var nome in new[] { nameof(VinculoProfissional.Situacao), nameof(VinculoProfissional.RemovidoEmUtc) })
        {
            var propriedade = typeof(VinculoProfissional).GetProperty(nome)!;
            Assert.IsNull(propriedade.GetSetMethod(), $"{nome} não deve ter setter público.");
        }
    }

    [TestMethod]
    public void GestaoDeProfissionais_RespeitaFronteirasDasCamadas()
    {
        Assert.IsTrue(typeof(IProfissionalRepositorio).Namespace!.StartsWith(
            "PontoBarber.Dominio.Modulos", StringComparison.Ordinal));
        Assert.IsTrue(typeof(RepositorioProfissionalEmOrm).Namespace!.StartsWith(
            "PontoBarber.Infra.Modulos", StringComparison.Ordinal));
        Assert.IsTrue(typeof(ProfissionalController).Namespace!.StartsWith(
            "PontoBarber.Api.Modulos", StringComparison.Ordinal));
    }
}
