using PontoBarber.Dominio.Modulos.Clientes;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

[TestClass]
public sealed class ClientesTestes
{
    [TestMethod]
    public void DadosPerfilCliente_CamposValidos_Aceita()
    {
        var dados = DadosPerfilCliente.Criar("  Cliente Teste  ", " cliente@exemplo.com ", "  ");

        Assert.AreEqual("Cliente Teste", dados.NomeCompleto);
        Assert.AreEqual("cliente@exemplo.com", dados.Email);
        Assert.IsNull(dados.Telefone);
        Assert.AreEqual(0, dados.Validar().Count);
    }

    [TestMethod]
    public void DadosPerfilCliente_NomeCurtoOuEmailInvalido_Rejeita()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            DadosPerfilCliente.Criar("ab", "cliente@exemplo.com", null));
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            DadosPerfilCliente.Criar("Cliente Teste", "sem-arroba", null));
    }

    [TestMethod]
    public void DadosPerfilCliente_TelefoneLongo_Rejeita()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
            DadosPerfilCliente.Criar("Cliente Teste", "cliente@exemplo.com", new string('9', 21)));
    }
}

#pragma warning restore CA1707
