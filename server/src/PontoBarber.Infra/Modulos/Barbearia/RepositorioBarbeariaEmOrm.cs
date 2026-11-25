using Npgsql;
using Microsoft.EntityFrameworkCore;
using PontoBarber.Aplicacao.Compartilhado;
using PontoBarber.Dominio.Modulos.Barbearia;
using PontoBarber.Dominio.Modulos.Profissionais;
using PontoBarber.Infra.Compartilhado.Orm;
using BarbeariaEntidade = PontoBarber.Dominio.Modulos.Barbearia.Barbearia;

namespace PontoBarber.Infra.Modulos.Barbearia;

/// <summary>
/// Persistência da barbearia: criação transacional, consulta e atualização
/// de dados cadastrais (ADR-019, Task 4.2).
/// </summary>
public sealed class RepositorioBarbeariaEmOrm(ContextoPontoBarber contexto) : IBarbeariaRepositorio
{
    public Task<bool> CodigoConviteExisteAsync(string codigo, CancellationToken cancellationToken) =>
        contexto.Set<BarbeariaEntidade>()
            .AnyAsync(barbearia => barbearia.CodigoConvite == codigo, cancellationToken);

    public async Task CriarAsync(
        BarbeariaEntidade barbearia,
        PerfilProfissional? perfilNovo,
        VinculoProfissional vinculo,
        CancellationToken cancellationToken)
    {
        contexto.Set<BarbeariaEntidade>().Add(barbearia);
        if (perfilNovo is not null)
            contexto.Set<PerfilProfissional>().Add(perfilNovo);
        contexto.Set<VinculoProfissional>().Add(vinculo);

        await SalvarAsync(cancellationToken);
    }

    public Task<BarbeariaEntidade?> ObterPorIdAsync(Guid barbeariaId, CancellationToken cancellationToken) =>
        contexto.Set<BarbeariaEntidade>()
            .AsNoTracking()
            .SingleOrDefaultAsync(barbearia => barbearia.Id == barbeariaId, cancellationToken);

    public Task<BarbeariaEntidade?> ObterPorCodigoConviteAsync(string codigo, CancellationToken cancellationToken) =>
        contexto.Set<BarbeariaEntidade>()
            .AsNoTracking()
            .SingleOrDefaultAsync(barbearia => barbearia.CodigoConvite == codigo, cancellationToken);

    public async Task AtualizarAsync(BarbeariaEntidade barbearia, CancellationToken cancellationToken)
    {
        contexto.Set<BarbeariaEntidade>().Update(barbearia);
        await SalvarAsync(cancellationToken);
    }

    private async Task SalvarAsync(CancellationToken cancellationToken)
    {
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
        excecao.InnerException is PostgresException postgres &&
        postgres.SqlState == "23505" &&
        postgres.MessageText.Contains("UX_Barbearias_CodigoConvite", StringComparison.Ordinal);
}
