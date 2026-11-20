using PontoBarber.Aplicacao.Modulos.Profissionais;
using PontoBarber.Dominio.Modulos.Barbearia;

namespace PontoBarber.Testes.Arquitetura;

/// <summary>
/// Task 4.5: contratos que sustentam o vínculo profissional do proprietário.
/// </summary>
[TestClass]
public class TestesVinculoProprietario
{
    [TestMethod]
    public void Barbearia_IdentificaProprietarioSemSetterPublico()
    {
        var propriedade = typeof(Barbearia).GetProperty(nameof(Barbearia.IdProfissionalProprietario));

        Assert.IsNotNull(propriedade);
        Assert.IsNull(propriedade!.GetSetMethod());
    }

    [TestMethod]
    public void ServicoProfissional_ExpoeAutorizacaoDeAgendaSemImplementarAgendamentos()
    {
        var metodo = typeof(ServicoProfissional).GetMethod("ObterVinculoParaAgendaAsync");

        Assert.IsNotNull(metodo);
        Assert.AreEqual(typeof(Task<PontoBarber.Aplicacao.Modulos.Profissionais.DTOs.VinculoResponse>), metodo!.ReturnType);
    }
}
