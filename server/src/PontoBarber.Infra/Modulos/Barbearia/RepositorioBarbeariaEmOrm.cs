using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

namespace PontoBarber.Infra.Modulos.Barbearia;

/// <summary>
/// Persistência da criação de barbearia com o proprietário e seu vínculo na
/// mesma transação (ADR-019).
/// </summary>
public sealed class RepositorioBarbeariaEmOrm(ContextoPontoBarber contexto) : IBarbeariaRepositorio
{
    public Task<bool> CodigoConviteExisteAsync(string codigo, CancellationToken cancellationToken) =>
        contexto.Set<BarbeariaEntidade>()
            .AnyAsync(barbearia => barbearia.CodigoConvite == codigo, cancellationToken);

    public async Task CriarAsync(
        BarbeariaEntidade barbearia,
        PerfilProfissional proprietario,
        VinculoProfissional vinculo,
        CancellationToken cancellationToken)
    {
        contexto.Set<BarbeariaEntidade>().Add(barbearia);
        contexto.Set<PerfilProfissional>().Add(proprietario);
        contexto.Set<VinculoProfissional>().Add(vinculo);

        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException excecao) when (EhConflitoDeConvite(excecao))
        {
            throw new ExcecaoConflitoAplicacao(
                CodigosErro.TenantConviteConflito,
                "Não foi possível gerar um código de convite único.");
        }
    }

    private static bool EhConflitoDeConvite(DbUpdateException excecao) =>
        excecao.InnerException is SqlException sql &&
        (sql.Number == 2601 || sql.Number == 2627) &&
        sql.Message.Contains("UX_Barbershops_CodigoConvite", StringComparison.Ordinal);
}
