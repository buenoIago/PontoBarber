using PontoBarber.Dominio.Modulos.Profissionais;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

#pragma warning disable CA1707 // Nomenclatura de testes Metodo_Cenario_Resultado (TDD §7.1)

namespace PontoBarber.Testes.Unidade;

/// <summary>
/// Padronização de entidades: identificador, validação e atualização controlada.
/// </summary>
[TestClass]
public sealed class EntidadeBaseTestes
{
    [TestMethod]
    public void Entidades_ComIdGerado_NaoSaoGuidVazio()
    {
        var barbearia = NovaBarbearia();
        var perfil = PerfilProfissional.Criar("usuario-1", "Nome", "11999999999");
        var vinculo = VinculoProfissional.Criar(barbearia.Id, perfil.Id);

        Assert.AreNotEqual(Guid.Empty, barbearia.Id);
        Assert.AreNotEqual(Guid.Empty, perfil.Id);
        Assert.AreNotEqual(Guid.Empty, vinculo.Id);
    }

    [TestMethod]
    public void Validar_ComDadosValidos_NaoRetornaErros()
    {
        var erros = NovaBarbearia().Validar();

        Assert.AreEqual(0, erros.Count);
    }

    [TestMethod]
    public void Validar_ComCamposInvalidos_RetornaErrosPorCampo()
    {
        var barbearia = BarbeariaEntidade.Criar(
            " ", "123", "invalido", " ", " ", null, "ABC23456", "Marte/Fobos", Guid.NewGuid());

        var campos = barbearia.Validar().Select(erro => erro.Campo).ToList();

        CollectionAssert.Contains(campos, "nome");
        CollectionAssert.Contains(campos, "telefoneComercial");
        CollectionAssert.Contains(campos, "emailComercial");
        CollectionAssert.Contains(campos, "cidade");
        CollectionAssert.Contains(campos, "endereco");
        CollectionAssert.Contains(campos, "fusoHorarioId");
    }

    [TestMethod]
    public void Atualizar_ComNovaInstancia_CopiaSomenteCamposPermitidos()
    {
        var original = NovaBarbearia();
        var id = original.Id;
        var codigo = original.CodigoConvite;
        var proprietario = original.IdProfissionalProprietario;
        var fuso = original.FusoHorarioId;
        var criado = original.CriadoEmUtc;
        var ativo = original.Ativo;

        var alterada = BarbeariaEntidade.Criar(
            "Nova Barbearia", "(21) 88888-7777", "novo@exemplo.com", "Rio", "Rua Nova, 10",
            null, codigo, fuso, proprietario);
        original.Atualizar(alterada);

        Assert.AreEqual("Nova Barbearia", original.Nome);
        Assert.AreEqual("Rua Nova, 10", original.Endereco);
        Assert.IsNull(original.Instagram);
        Assert.AreEqual(id, original.Id);
        Assert.AreEqual(codigo, original.CodigoConvite);
        Assert.AreEqual(proprietario, original.IdProfissionalProprietario);
        Assert.AreEqual(fuso, original.FusoHorarioId);
        Assert.AreEqual(criado, original.CriadoEmUtc);
        Assert.AreEqual(ativo, original.Ativo);
    }

    [TestMethod]
    public void Atualizar_ComInstanciaInvalida_PermiteValidarDepois()
    {
        var original = NovaBarbearia();
        var invalida = BarbeariaEntidade.Criar(
            "", "(11) 99999-9999", "contato@exemplo.com", "São Paulo", "Rua Um, 1",
            null, original.CodigoConvite, original.FusoHorarioId, original.IdProfissionalProprietario);

        original.Atualizar(invalida);

        Assert.IsTrue(original.Validar().Any(erro => erro.Campo == "nome"));
    }

    [TestMethod]
    public void PerfilProfissional_Atualizar_AlteraSomenteDadosCadastrais()
    {
        var perfil = PerfilProfissional.Criar("usuario-1", "Nome Original", "11999999999");
        var id = perfil.Id;

        perfil.Atualizar(PerfilProfissional.Criar("outro-usuario", "Nome Novo", "21988887777"));

        Assert.AreEqual("Nome Novo", perfil.NomeCompleto);
        Assert.AreEqual("21988887777", perfil.Telefone);
        Assert.AreEqual(id, perfil.Id);
        Assert.AreEqual("usuario-1", perfil.UsuarioId);
        Assert.IsTrue(perfil.Ativo);
    }

    [TestMethod]
    public void PerfilProfissional_Validar_ComDadosInvalidos_RetornaErros()
    {
        var perfil = PerfilProfissional.Criar("", " ", "123");

        var campos = perfil.Validar().Select(erro => erro.Campo).ToList();

        CollectionAssert.Contains(campos, "usuarioId");
        CollectionAssert.Contains(campos, "nomeCompleto");
        CollectionAssert.Contains(campos, "telefone");
    }

    [TestMethod]
    public void VinculoProfissional_Validar_ComIdsValidos_NaoRetornaErros()
    {
        var vinculo = VinculoProfissional.CriarBarbeiro(Guid.CreateVersion7(), Guid.CreateVersion7());

        Assert.AreEqual(0, vinculo.Validar().Count);
    }

    [TestMethod]
    public void VinculoProfissional_Validar_ComIdsVazios_RetornaErros()
    {
        var vinculo = VinculoProfissional.CriarBarbeiro(Guid.Empty, Guid.Empty);

        var campos = vinculo.Validar().Select(erro => erro.Campo).ToList();

        CollectionAssert.Contains(campos, "barbeariaId");
        CollectionAssert.Contains(campos, "profissionalId");
    }

    [TestMethod]
    public void VinculoProfissional_Atualizar_RejeitaAlteracaoEAceitaValoresIguais()
    {
        var vinculo = VinculoProfissional.CriarBarbeiro(Guid.CreateVersion7(), Guid.CreateVersion7());

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            vinculo.Atualizar(VinculoProfissional.CriarBarbeiro(Guid.CreateVersion7(), Guid.CreateVersion7())));

        vinculo.Atualizar(vinculo);
    }

    private static BarbeariaEntidade NovaBarbearia() => BarbeariaEntidade.Criar(
        "Barbearia Central", "(11) 99999-9999", "contato@exemplo.com",
        "São Paulo", "Rua Central, 100", "@barbeariacentral",
        "ABC23456", "America/Sao_Paulo", Guid.NewGuid());
}

#pragma warning restore CA1707
